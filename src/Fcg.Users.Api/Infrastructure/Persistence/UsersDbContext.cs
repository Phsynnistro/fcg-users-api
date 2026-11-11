using Fcg.Users.Api.Domain.Users;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Users.Api.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);

        // Tabelas do Transactional Outbox/Inbox do MassTransit. Vivem no MESMO banco do serviço:
        // é isso que permite gravar entidade + mensagem numa única transação local.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
