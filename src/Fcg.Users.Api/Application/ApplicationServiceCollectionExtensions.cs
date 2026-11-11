using Fcg.Users.Api.Application.Users;
using Fcg.Users.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Fcg.Users.Api.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TimeProvider (.NET 8+) em vez de DateTime.UtcNow: o tempo vira dependência testável.
        services.AddSingleton(TimeProvider.System);

        // PBKDF2 do ASP.NET Core Identity: algoritmo, salt e iterações já resolvidos. Não invente criptografia.
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<UserQueries>();

        return services;
    }
}
