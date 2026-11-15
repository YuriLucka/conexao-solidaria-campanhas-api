namespace Campanhas.Domain.Exceptions;

// Dado inválido (vira 400).
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
