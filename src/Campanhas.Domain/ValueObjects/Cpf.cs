using Campanhas.Domain.Exceptions;

namespace Campanhas.Domain.ValueObjects;

// Aceita com ou sem máscara; guarda só os 11 dígitos.
public sealed class Cpf
{
    public string Value { get; }

    public Cpf(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("O CPF é obrigatório.");

        var trimmed = value.Trim();
        if (trimmed.Any(c => !char.IsDigit(c) && c != '.' && c != '-'))
            throw new DomainException("CPF inválido.");

        var digitos = new string(trimmed.Where(char.IsDigit).ToArray());
        if (!EhValido(digitos))
            throw new DomainException("CPF inválido.");

        Value = digitos;
    }

    public static bool EhValido(string digitos)
    {
        if (digitos.Length != 11 || digitos.All(c => c == digitos[0]))
            return false;

        return digitos[9] - '0' == DigitoVerificador(digitos, 9)
            && digitos[10] - '0' == DigitoVerificador(digitos, 10);
    }

    private static int DigitoVerificador(string digitos, int tamanho)
    {
        var soma = 0;
        for (var i = 0; i < tamanho; i++)
            soma += (digitos[i] - '0') * (tamanho + 1 - i);

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is Cpf other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
