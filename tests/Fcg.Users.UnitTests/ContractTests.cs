using Fcg.Contracts.Events;
using MassTransit;

namespace Fcg.Users.UnitTests;

/// <summary>
/// "Testes de contrato" baratos: o contrato é duplicado entre repositórios, então qualquer mudança
/// acidental de namespace/nome/exchange quebraria a integração SILENCIOSAMENTE em runtime.
/// Este teste transforma essa quebra silenciosa em build vermelho.
/// </summary>
public sealed class ContractTests
{
    [Fact]
    public void UserCreatedEvent_MantemIdentidadeDoContrato()
    {
        var type = typeof(UserCreatedEvent);

        Assert.Equal("Fcg.Contracts.Events", type.Namespace);
        Assert.Equal("fcg.user-created", type.GetCustomAttributes(typeof(EntityNameAttribute), false)
            .Cast<EntityNameAttribute>().Single().EntityName);

        var properties = type.GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Superset(new HashSet<string> { "UserId", "Name", "Email", "CreatedAt" }, properties);
    }
}
