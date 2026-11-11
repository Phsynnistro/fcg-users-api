using Fcg.Users.Api.Domain.Common;
using Fcg.Users.Api.Domain.Users;

namespace Fcg.Users.UnitTests;

public sealed class UserTests
{
    private readonly FixedTimeProvider _clock = new();

    [Fact]
    public void Create_NormalizaEmailENome()
    {
        var user = User.Create("  Ana Souza ", "  Ana.Souza@Example.COM ", UserRole.User, _clock);

        Assert.Equal("Ana Souza", user.Name);
        Assert.Equal("ana.souza@example.com", user.Email);
        Assert.Equal(UserRole.User, user.Role);
        Assert.Equal(FixedTimeProvider.Default.UtcDateTime, user.CreatedAt);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Theory]
    [InlineData("sem-arroba.com")]
    [InlineData("a@b")]
    [InlineData("")]
    public void Create_EmailInvalido_LancaDomainException(string email) =>
        Assert.Throws<DomainException>(() => User.Create("Ana", email, UserRole.User, _clock));

    [Fact]
    public void Create_NomeVazio_LancaDomainException() =>
        Assert.Throws<DomainException>(() => User.Create("   ", "ana@example.com", UserRole.User, _clock));

    [Fact]
    public void Create_GeraIdsOrdenaveisPorTempo()
    {
        var first = User.Create("A", "a@example.com", UserRole.User, _clock);
        Thread.Sleep(2);
        var second = User.Create("B", "b@example.com", UserRole.User, _clock);

        // Guid v7: os primeiros bytes carregam o timestamp → ordenação lexicográfica crescente.
        Assert.True(string.CompareOrdinal(first.Id.ToString(), second.Id.ToString()) < 0);
    }
}
