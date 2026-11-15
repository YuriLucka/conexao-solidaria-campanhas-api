namespace Campanhas.Domain.Interfaces;

// Grava numa transação tudo o que foi adicionado/alterado, inclusive o evento no outbox.
public interface IUnitOfWork
{
    Task SalvarAsync(CancellationToken ct = default);
}
