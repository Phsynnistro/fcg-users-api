using Fcg.Users.Api.Application.Abstractions;
using Fcg.Users.Api.Application.Users;
using Fcg.Users.Api.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Users.Api.Infrastructure.Security;

/// <summary>Adapter da porta ITokenService: emite JWT assinado com RS256.</summary>
public sealed class JwtTokenService(
    JwtSigningKeys keys,
    IOptions<JwtOptions> options,
    TimeProvider clock) : ITokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessTokenResponse IssueAccessToken(User user)
    {
        var jwt = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(jwt.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = keys.SigningCredentials,
            Claims = new Dictionary<string, object>
            {
                [FcgClaims.Subject] = user.Id.ToString(),
                [FcgClaims.Email] = user.Email,
                [FcgClaims.Name] = user.Name,
                [FcgClaims.Role] = user.Role.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            }
        };

        return new AccessTokenResponse(
            Handler.CreateToken(descriptor),
            "Bearer",
            (int)(expiresAt - now).TotalSeconds,
            expiresAt);
    }
}
