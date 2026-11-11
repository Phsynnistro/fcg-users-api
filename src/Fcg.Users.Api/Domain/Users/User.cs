using Fcg.Users.Api.Domain.Common;

namespace Fcg.Users.Api.Domain.Users;

public enum UserRole
{
    User = 1,
    Admin = 2
}

/// <summary>
/// Aggregate root de identidade. Setters privados: o estado só muda por métodos que
/// garantem as invariantes. O construtor sem parâmetros existe apenas para o EF Core.
/// </summary>
public sealed class User
{
    public const int NameMaxLength = 120;

    private User()
    {
    }

    private User(Guid id, string name, string email, UserRole role, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        Role = role;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static User Create(string name, string email, UserRole role, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            throw new DomainException($"O nome é obrigatório e deve ter até {NameMaxLength} caracteres.");
        }

        if (!EmailAddress.IsValid(email))
        {
            throw new DomainException("E-mail em formato inválido.");
        }

        // Guid v7 (.NET 9+): ordenável por tempo → índice B-tree sem fragmentação, ao contrário do v4.
        return new User(Guid.CreateVersion7(), name.Trim(), EmailAddress.Normalize(email), role, clock.GetUtcNow().UtcDateTime);
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Hash de senha inválido.");
        }

        PasswordHash = passwordHash;
    }
}
