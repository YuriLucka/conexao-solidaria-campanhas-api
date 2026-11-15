using Campanhas.Application.DTOs;
using Campanhas.Application.Exceptions;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Interfaces;
using ConexaoSolidaria.Contracts.Events;

namespace Campanhas.Application.Services;

public class DoacaoService : IDoacaoService
{
    private readonly ICampanhaRepository _campanhas;
    private readonly IDoacaoRepository _doacoes;
    private readonly IEventPublisher _publisher;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _relogio;

    public DoacaoService(ICampanhaRepository campanhas, IDoacaoRepository doacoes, IEventPublisher publisher, IUnitOfWork uow, TimeProvider relogio)
    {
        _campanhas = campanhas;
        _doacoes = doacoes;
        _publisher = publisher;
        _uow = uow;
        _relogio = relogio;
    }

    // Não toca no valor arrecadado: grava a doação Pendente e publica o evento.
    // Doação e evento (outbox) vão para o banco na mesma transação; o MassTransit entrega ao RabbitMQ depois.
    public async Task<DoacaoResponse> CriarAsync(Guid doadorId, CriarDoacaoRequest request, CancellationToken ct = default)
    {
        var campanha = await _campanhas.ObterPorIdAsync(request.IdCampanha, ct)
            ?? throw new NotFoundException("Campanha não encontrada.");

        var agora = _relogio.GetUtcNow().UtcDateTime;
        var doacao = new Doacao(campanha, doadorId, request.ValorDoacao, agora);

        await _doacoes.AdicionarAsync(doacao, ct);
        await _publisher.PublicarAsync(new DoacaoRecebidaEvent(doacao.Id, doacao.CampanhaId, doacao.DoadorId, doacao.Valor, agora), ct);
        await _uow.SalvarAsync(ct);

        return DoacaoResponse.De(doacao, campanha.Titulo);
    }
}
