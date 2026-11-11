using System.Text.Json.Serialization;
using Fcg.Users.Api.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Fcg.Users.Api.Presentation.Endpoints;

/// <summary>
/// Discovery mínimo (subconjunto do OpenID Connect Discovery) + JWKS.
/// Os demais serviços apontam Jwt:MetadataAddress para cá — ex.: http://users-api/.well-known/openid-configuration —
/// e obtêm a chave pública automaticamente. É a comunicação HTTP serviço-a-serviço pelo nome do Service do Kubernetes.
/// </summary>
public static class WellKnownEndpoints
{
    public static IEndpointRouteBuilder MapWellKnownEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/.well-known").WithTags("Discovery").AllowAnonymous();

        group.MapGet("/openid-configuration", (HttpContext http, IOptions<JwtOptions> jwt) =>
            {
                // jwks_uri montado a partir do Host da requisição: funciona igual no compose (users-api:8080)
                // e no Kubernetes (users-api:80) sem configuração extra.
                var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
                return Results.Ok(new DiscoveryDocument(jwt.Value.Issuer, $"{baseUrl}/.well-known/jwks.json", ["RS256"]));
            })
            .WithSummary("Documento de discovery do emissor de tokens");

        group.MapGet("/jwks.json", (JwtSigningKeys keys) => Results.Ok(new JsonWebKeySet([keys.PublicJwk])))
            .WithSummary("Chaves públicas (JWKS) para validação dos tokens");

        return app;
    }

    private sealed record DiscoveryDocument(
        [property: JsonPropertyName("issuer")] string Issuer,
        [property: JsonPropertyName("jwks_uri")] string JwksUri,
        [property: JsonPropertyName("id_token_signing_alg_values_supported")] string[] SigningAlgorithms);

    private sealed record JsonWebKeySet([property: JsonPropertyName("keys")] IReadOnlyList<JsonWebKeyDto> Keys);
}
