using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Users.Api.Infrastructure.Security;

/// <summary>
/// Material criptográfico carregado UMA vez (singleton). Expõe:
///  - SigningCredentials: chave privada, usada para assinar;
///  - ValidationKey: só a parte pública, usada para validar os próprios tokens;
///  - PublicJwk: a chave pública no formato JWK, publicada em /.well-known/jwks.json.
/// </summary>
public sealed class JwtSigningKeys : IDisposable
{
    private readonly RSA _rsa;

    public JwtSigningKeys(IOptions<JwtOptions> options)
    {
        var privateKey = options.Value.PrivateKey;
        if (string.IsNullOrWhiteSpace(privateKey))
        {
            throw new InvalidOperationException("Jwt:PrivateKey não configurada.");
        }

        _rsa = RSA.Create();
        _rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);

        var publicParameters = _rsa.ExportParameters(includePrivateParameters: false);

        // kid derivado da própria chave pública: muda automaticamente quando a chave é rotacionada.
        KeyId = Base64UrlEncoder.Encode(SHA256.HashData(_rsa.ExportSubjectPublicKeyInfo()))[..16];

        SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = KeyId }, SecurityAlgorithms.RsaSha256);
        ValidationKey = new RsaSecurityKey(publicParameters) { KeyId = KeyId };
        PublicJwk = new JsonWebKeyDto(
            Kty: "RSA",
            Use: "sig",
            Alg: SecurityAlgorithms.RsaSha256,
            Kid: KeyId,
            N: Base64UrlEncoder.Encode(publicParameters.Modulus!),
            E: Base64UrlEncoder.Encode(publicParameters.Exponent!));
    }

    public string KeyId { get; }
    public SigningCredentials SigningCredentials { get; }
    public SecurityKey ValidationKey { get; }
    public JsonWebKeyDto PublicJwk { get; }

    public void Dispose() => _rsa.Dispose();
}

/// <summary>JWK (RFC 7517) contendo SOMENTE parâmetros públicos (n, e).</summary>
public sealed record JsonWebKeyDto(
    [property: JsonPropertyName("kty")] string Kty,
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("alg")] string Alg,
    [property: JsonPropertyName("kid")] string Kid,
    [property: JsonPropertyName("n")] string N,
    [property: JsonPropertyName("e")] string E);
