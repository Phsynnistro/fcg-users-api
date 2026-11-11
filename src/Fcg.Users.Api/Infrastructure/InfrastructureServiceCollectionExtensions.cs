using System.Text.Json.Serialization;
using Fcg.Users.Api.Application.Abstractions;
using Fcg.Users.Api.Infrastructure.Messaging;
using Fcg.Users.Api.Infrastructure.Observability;
using Fcg.Users.Api.Infrastructure.Persistence;
using Fcg.Users.Api.Infrastructure.Security;
using Fcg.Users.Api.Presentation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Users.Api.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddMessaging(services, configuration);
        services.AddObservability(configuration);
        AddSecurity(services, configuration);
        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database não configurada (deve vir de um Secret).");

        // EnableRetryOnFailure: retry automático de falhas transitórias do Postgres (failover, conexão derrubada).
        // É compatível com o outbox porque o MassTransit executa suas transações dentro de
        // Database.CreateExecutionStrategy() — confira EntityFrameworkOutboxContextFactory no fonte do MassTransit.
        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddHealthChecks()
            .AddDbContextCheck<UsersDbContext>("database", tags: [HealthEndpoints.ReadinessTag]);

        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<IDataSeeder, AdminUserSeeder>();
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            // Bus Outbox: Publish() via IPublishEndpoint vira um INSERT em OutboxMessage; um background
            // service entrega ao RabbitMQ depois do commit. Se o broker cair, o cadastro continua funcionando.
            bus.AddEntityFrameworkOutbox<UsersDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                cfg.Host(rabbit.Host, rabbit.Port, rabbit.VirtualHost, host =>
                {
                    host.Username(rabbit.Username);
                    host.Password(rabbit.Password);
                });

                cfg.ConfigureJsonSerializerOptions(json =>
                {
                    json.Converters.Add(new JsonStringEnumConverter());
                    return json;
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }

    private static void AddSecurity(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer é obrigatório.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience é obrigatório.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.PrivateKey), "Jwt:PrivateKey é obrigatório (Secret).")
            .Validate(o => o.AccessTokenLifetimeMinutes is > 0 and <= 1440, "Jwt:AccessTokenLifetimeMinutes deve estar entre 1 e 1440.")
            .ValidateOnStart();

        services.AddSingleton<JwtSigningKeys>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // O emissor valida os próprios tokens com a chave pública local (sem chamada de rede).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, JwtSigningKeys>((bearer, jwtOptions, keys) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = keys.ValidationKey,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = FcgClaims.Name,
                    RoleClaimType = FcgClaims.Role
                };
                bearer.Events = new JwtBearerEvents
                {
                    // Sem isto, um 401 por chave/assinatura inválida é "mudo": o motivo real fica invisível.
                    OnAuthenticationFailed = context =>
                    {
                        context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer")
                            .LogWarning("Token rejeitado: {Reason}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(Policies.Admin));
    }
}
