namespace Fcg.Users.Api.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    /// <summary>
    /// Chave privada RSA em PKCS#8 (DER) codificada em Base64 — uma linha só, fácil de injetar via Secret.
    /// Apenas o UsersAPI possui esta chave: só ele ASSINA. Os demais serviços só VALIDAM (chave pública/JWKS).
    /// </summary>
    public string PrivateKey { get; init; } = string.Empty;
}
