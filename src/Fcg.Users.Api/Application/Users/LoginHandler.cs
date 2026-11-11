using Fcg.Users.Api.Application.Abstractions;
using Fcg.Users.Api.Application.Common;
using Fcg.Users.Api.Domain.Users;
using Fcg.Users.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Users.Api.Application.Users;

public sealed class LoginHandler(
    UsersDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    ILogger<LoginHandler> logger)
{
    // Quando o e-mail não existe, ainda executamos uma verificação de hash para que o tempo de resposta
    // seja parecido com o de uma senha errada (mitiga enumeração de usuários por timing).
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(null!, "fcg-timing-equalizer");

    public async Task<Result<AccessTokenResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var validationError = new ValidationBuilder()
            .AddIf(string.IsNullOrWhiteSpace(request.Email), "email", "O e-mail é obrigatório.")
            .AddIf(string.IsNullOrEmpty(request.Password), "password", "A senha é obrigatória.")
            .Build();

        if (validationError is not null)
        {
            return validationError;
        }

        var email = EmailAddress.Normalize(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(null!, DummyHash, request.Password);
            return UserErrors.InvalidCredentials;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Falha de login para o usuário {UserId}.", user.Id);
            return UserErrors.InvalidCredentials;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Parâmetros do PBKDF2 evoluíram (ex.: mais iterações): atualiza o hash de forma transparente.
            user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
            await db.SaveChangesAsync(cancellationToken);
        }

        return tokenService.IssueAccessToken(user);
    }
}
