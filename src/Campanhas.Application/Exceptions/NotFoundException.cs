namespace Campanhas.Application.Exceptions;

// Recurso inexistente (vira 404).
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
