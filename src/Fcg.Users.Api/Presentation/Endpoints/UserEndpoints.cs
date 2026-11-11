using System.Security.Claims;
using Fcg.Users.Api.Application.Common;
using Fcg.Users.Api.Application.Users;
using Fcg.Users.Api.Infrastructure.Security;

namespace Fcg.Users.Api.Presentation.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapPost("", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingExtensions.AuthenticationPolicy)
            .WithName("RegisterUser")
            .WithSummary("Cadastra um usuário e publica UserCreatedEvent")
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapGet("/me", GetMeAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Dados do usuário autenticado")
            .Produces<UserResponse>();

        group.MapGet("/{id:guid}", GetByIdAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("GetUserById")
            .WithSummary("Consulta um usuário (Admin)")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("", ListAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("ListUsers")
            .WithSummary("Lista usuários paginados (Admin)")
            .Produces<PagedResponse<UserResponse>>();

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterUserRequest request, RegisterUserHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/users/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem();
    }

    private static async Task<IResult> GetMeAsync(ClaimsPrincipal user, UserQueries queries, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(user.GetUserId(), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }

    private static async Task<IResult> GetByIdAsync(Guid id, UserQueries queries, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }

    private static async Task<IResult> ListAsync(UserQueries queries, CancellationToken cancellationToken, int page = 1, int pageSize = 20) =>
        Results.Ok(await queries.ListAsync(page, pageSize, cancellationToken));
}
