using Fcg.Users.Api.Domain.Users;

namespace Fcg.Users.UnitTests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("Senha@123")]
    [InlineData("a1!aaaaa")]
    [InlineData("C0mpl3x#Passw0rd")]
    public void Validate_SenhaForte_NaoRetornaErros(string password) =>
        Assert.Empty(PasswordPolicy.Validate(password));

    [Theory]
    [InlineData("", "obrigatória")]
    [InlineData("Ab1!", "mínimo")]
    [InlineData("12345678!", "letra")]
    [InlineData("Abcdefgh!", "número")]
    [InlineData("Abcdefg12", "especial")]
    public void Validate_SenhaFraca_RetornaErroEspecifico(string password, string expectedFragment)
    {
        var errors = PasswordPolicy.Validate(password);

        Assert.Contains(errors, e => e.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase));
    }
}
