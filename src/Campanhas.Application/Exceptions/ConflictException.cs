namespace Campanhas.Application.Exceptions;

// Conflito com dado já existente, como e-mail ou CPF repetido (vira 409).
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
