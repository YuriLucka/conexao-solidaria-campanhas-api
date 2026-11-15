using System.Text.RegularExpressions;
using Campanhas.Domain.Exceptions;

namespace Campanhas.Domain.ValueObjects;

public sealed partial class Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("E-mail é obrigatório.");

        value = value.Trim();
        if (value.Length > 200 || !Formato().IsMatch(value))
            throw new DomainException("Formato de e-mail inválido.");

        Value = value.ToLowerInvariant();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Formato();

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is Email other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
