namespace Campanhas.Application.Exceptions;

// Recurso inexistente (vira 404).
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Conflito com dado já existente, como e-mail ou CPF repetido (vira 409).
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
