using Fcg.Users.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Users.Api.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento fora da entidade: o domínio não conhece EF Core (nada de data annotations).</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Name).HasMaxLength(User.NameMaxLength).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        // A unicidade de e-mail é garantida AQUI, pelo banco — não pelo "AnyAsync" do handler.
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
