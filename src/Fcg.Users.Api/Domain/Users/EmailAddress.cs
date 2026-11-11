using System.Text.RegularExpressions;

namespace Fcg.Users.Api.Domain.Users;

public static partial class EmailAddress
{
    public const int MaxLength = 254;

    /// <summary>E-mail é a chave natural de login: normalizar evita "Ana@x.com" e "ana@x.com" como contas distintas.</summary>
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        return trimmed.Length <= MaxLength && Pattern().IsMatch(trimmed);
    }

    // Regex gerada em compile-time (source generator): sem custo de interpretação em runtime.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
