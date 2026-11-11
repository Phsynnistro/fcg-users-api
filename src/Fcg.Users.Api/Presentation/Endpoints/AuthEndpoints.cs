using Fcg.Users.Api.Application.Users;

namespace Fcg.Users.Api.Presentation.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingExtensions.AuthenticationPolicy)
            .WithName("Login")
            .WithSummary("Autentica o usuário e emite um access token JWT (RS256)")
            .Produces<AccessTokenResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, LoginHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }
}
