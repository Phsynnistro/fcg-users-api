using Fcg.Users.Api.Application.Common;

namespace Fcg.Users.Api.Application.Users;

public static class UserErrors
{
    public static readonly Error EmailAlreadyInUse =
        Error.Conflict("users.email_in_use", "Já existe um usuário cadastrado com este e-mail.");

    // Mensagem genérica de propósito: não revela se o e-mail existe (evita enumeração de usuários).
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "E-mail ou senha inválidos.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("users.not_found", $"Usuário {id} não encontrado.");
}
