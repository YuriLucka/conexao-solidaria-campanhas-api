namespace Campanhas.Application.Interfaces;

// Publica no broker. Com o outbox, o evento só sai depois do IUnitOfWork.SalvarAsync.
public interface IEventPublisher
{
    Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : class;
}
