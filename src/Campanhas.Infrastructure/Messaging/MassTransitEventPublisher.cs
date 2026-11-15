using Campanhas.Application.Interfaces;
using MassTransit;

namespace Campanhas.Infrastructure.Messaging;

// Com o bus outbox ligado, Publish só grava na tabela outbox_message do DbContext;
// a entrega ao RabbitMQ acontece depois do SaveChanges, por um serviço em segundo plano do MassTransit.
public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publish;

    public MassTransitEventPublisher(IPublishEndpoint publish) => _publish = publish;

    public Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : class
        => _publish.Publish(evento, ct);
}
