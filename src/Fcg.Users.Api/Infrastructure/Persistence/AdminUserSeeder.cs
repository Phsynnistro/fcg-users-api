using Fcg.Users.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fcg.Users.Api.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminName { get; init; } = "Administrador FCG";
    public string? AdminEmail { get; init; }
    public string? AdminPassword { get; init; }
}

/// <summary>
/// Cria o administrador inicial (ninguém consegue virar Admin pela API pública — por design).
/// Credenciais vêm de Secret. Idempotente: se já existir, não faz nada.
/// </summary>
public sealed class AdminUserSeeder(
    UsersDbContext db,
    IPasswordHasher<User> passwordHasher,
    IOptions<SeedOptions> options,
    TimeProvider clock,
    ILogger<AdminUserSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value;

        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogWarning("Seed do administrador ignorado: Seed:AdminEmail/Seed:AdminPassword não configurados.");
            return;
        }

        var email = EmailAddress.Normalize(seed.AdminEmail);
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        var admin = User.Create(seed.AdminName, email, UserRole.Admin, clock);
        admin.SetPasswordHash(passwordHasher.HashPassword(admin, seed.AdminPassword));
        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Administrador inicial {UserId} criado.", admin.Id);
    }
}
