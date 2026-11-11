using Fcg.Users.Api.Application;
using Fcg.Users.Api.Infrastructure;
using Fcg.Users.Api.Infrastructure.Persistence;
using Fcg.Users.Api.Presentation;
using Fcg.Users.Api.Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Composition root: cada camada registra as próprias dependências.
builder.Services
    .AddApiDefaults()
    .AddAuthenticationRateLimiting(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapApiDocs("FCG • Users API");
app.MapHealthEndpoints();
app.MapWellKnownEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();

await app.Services.InitializeDatabaseAsync<UsersDbContext>(app.Lifetime.ApplicationStopping);
await app.RunAsync();
