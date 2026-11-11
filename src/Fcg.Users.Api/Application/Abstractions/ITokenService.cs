using Fcg.Users.Api.Application.Users;
using Fcg.Users.Api.Domain.Users;

namespace Fcg.Users.Api.Application.Abstractions;

/// <summary>Porta: a aplicação precisa "emitir um token", mas não sabe que é JWT/RS256.</summary>
public interface ITokenService
{
    AccessTokenResponse IssueAccessToken(User user);
}
