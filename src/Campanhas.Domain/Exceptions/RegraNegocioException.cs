namespace Campanhas.Domain.Exceptions;

// Dado válido, mas a operação não é permitida no estado atual (vira 422).
public class RegraNegocioException : Exception
{
    public RegraNegocioException(string message) : base(message) { }
}
