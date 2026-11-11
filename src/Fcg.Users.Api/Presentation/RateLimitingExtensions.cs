using System.Threading.RateLimiting;

namespace Fcg.Users.Api.Presentation;

public sealed class AuthenticationRateLimitOptions
{
    public const string SectionName = "RateLimiting:Authentication";

    public int PermitLimit { get; init; } = 20;
    public int WindowSeconds { get; init; } = 60;
}

/// <summary>
/// Limita tentativas de login e de cadastro por IP (janela fixa): mitiga força bruta e credential stuffing
/// (Aula 07 — Segurança). Excedeu → 429 Too Many Requests, em Problem Details.
/// Atenção: atrás de proxy/gateway o IP visto é o do proxy — em produção, configure ForwardedHeaders.
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthenticationPolicy = "authentication";

    public static IServiceCollection AddAuthenticationRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(AuthenticationRateLimitOptions.SectionName).Get<AuthenticationRateLimitOptions>()
            ?? new AuthenticationRateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthenticationPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }
}
