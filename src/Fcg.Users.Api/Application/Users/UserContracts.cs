using Fcg.Users.Api.Domain.Users;

namespace Fcg.Users.Api.Application.Users;

// DTOs de entrada/saída. Nunca exponha a entidade (vazaria PasswordHash).
public sealed record RegisterUserRequest(string Name, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserResponse(Guid Id, string Name, string Email, UserRole Role, DateTime CreatedAt);

public sealed record AccessTokenResponse(string AccessToken, string TokenType, int ExpiresIn, DateTime ExpiresAt);

public static class UserMappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.CreatedAt);
}
