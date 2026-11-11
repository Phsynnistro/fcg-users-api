using Fcg.Contracts.Events;
using Fcg.Users.Api.Application.Common;
using Fcg.Users.Api.Domain.Users;
using Fcg.Users.Api.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Users.Api.Application.Users;

/// <summary>
/// Caso de uso: cadastrar usuário.
///
/// Ponto-chave (Transactional Outbox): o evento é publicado ANTES do SaveChanges, via IPublishEndpoint
/// com Bus Outbox habilitado. O MassTransit grava a mensagem na tabela OutboxMessage na MESMA transação
/// do INSERT do usuário. Resultado: ou os dois persistem, ou nenhum. Sem outbox, um crash entre
/// "salvar usuário" e "publicar evento" geraria um usuário sem e-mail de boas-vindas (dual-write problem).
/// </summary>
public sealed class RegisterUserHandler(
    UsersDbContext db,
    IPasswordHasher<User> passwordHasher,
    IPublishEndpoint publishEndpoint,
    TimeProvider clock,
    ILogger<RegisterUserHandler> logger)
{
    public async Task<Result<UserResponse>> HandleAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var email = EmailAddress.Normalize(request.Email);

        // Checagem otimista para devolver 409 amigável no caso comum.
        // A garantia real é o índice único (ver catch abaixo) — dois requests simultâneos passariam por aqui.
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return UserErrors.EmailAlreadyInUse;
        }

        var user = User.Create(request.Name, email, UserRole.User, clock);
        user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
        db.Users.Add(user);

        await publishEndpoint.Publish(
            new UserCreatedEvent(user.Id, user.Name, user.Email, user.CreatedAt),
            cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return UserErrors.EmailAlreadyInUse;
        }

        logger.LogInformation("Usuário {UserId} cadastrado; UserCreatedEvent gravado no outbox.", user.Id);
        return user.ToResponse();
    }

    private static Error? Validate(RegisterUserRequest request)
    {
        var builder = new ValidationBuilder()
            .AddIf(string.IsNullOrWhiteSpace(request.Name), "name", "O nome é obrigatório.")
            .AddIf(request.Name?.Trim().Length > User.NameMaxLength, "name", $"O nome deve ter até {User.NameMaxLength} caracteres.")
            .AddIf(!EmailAddress.IsValid(request.Email), "email", "Informe um e-mail válido.");

        foreach (var passwordError in PasswordPolicy.Validate(request.Password))
        {
            builder.Add("password", passwordError);
        }

        return builder.Build();
    }
}
