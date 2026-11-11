namespace Fcg.Users.Api.Domain.Users;

/// <summary>Regra de negócio da FCG: mínimo de 8 caracteres com letras, números e caracteres especiais.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public static IReadOnlyList<string> Validate(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return ["A senha é obrigatória."];
        }

        var errors = new List<string>();

        if (password.Length < MinLength)
        {
            errors.Add($"A senha deve ter no mínimo {MinLength} caracteres.");
        }

        if (!password.Any(char.IsLetter))
        {
            errors.Add("A senha deve conter ao menos uma letra.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("A senha deve conter ao menos um número.");
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("A senha deve conter ao menos um caractere especial.");
        }

        return errors;
    }
}
