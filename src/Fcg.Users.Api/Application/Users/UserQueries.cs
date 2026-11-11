using Fcg.Users.Api.Application.Common;
using Fcg.Users.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Users.Api.Application.Users;

/// <summary>Lado de leitura: AsNoTracking + projeção direta para DTO (não materializa a entidade inteira).</summary>
public sealed class UserQueries(UsersDbContext db)
{
    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Role, u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        return user;
    }

    public async Task<PagedResponse<UserResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var query = db.Users.AsNoTracking().OrderBy(u => u.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Role, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserResponse>(items, page, pageSize, total);
    }
}
