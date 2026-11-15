using Campanhas.Application.DTOs;
using Campanhas.Application.Exceptions;
using Campanhas.Application.Interfaces;
using Campanhas.Application.Services;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Exceptions;
using Campanhas.Domain.Interfaces;
using ConexaoSolidaria.Contracts.Events;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Campanhas.Tests.Application;

public class DoacaoServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICampanhaRepository> _campanhas = new();
    private readonly Mock<IDoacaoRepository> _doacoes = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly DoacaoService _service;
    private readonly Campanha _campanha = new("T", "D", Agora.UtcDateTime.AddDays(-1), Agora.UtcDateTime.AddDays(10), 1000m, Agora.UtcDateTime);
    private readonly Guid _doador = Guid.NewGuid();

    public DoacaoServiceTests()
    {
        _campanhas.Setup(r => r.ObterPorIdAsync(_campanha.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_campanha);
        _service = new DoacaoService(_campanhas.Object, _doacoes.Object, _publisher.Object, _uow.Object, new FakeTimeProvider(Agora));
    }

    [Fact]
    public async Task Doacao_grava_Pendente_e_publica_DoacaoRecebidaEvent()
    {
        DoacaoRecebidaEvent? evento = null;
        _publisher.Setup(p => p.PublicarAsync(It.IsAny<DoacaoRecebidaEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DoacaoRecebidaEvent, CancellationToken>((e, _) => evento = e);

        var resposta = await _service.CriarAsync(_doador, new CriarDoacaoRequest(_campanha.Id, 75.50m));

        Assert.Equal(StatusDoacao.Pendente, resposta.Status);
        Assert.NotNull(evento);
        Assert.Equal(resposta.Id, evento!.DoacaoId);
        Assert.Equal(_campanha.Id, evento.IdCampanha);
        Assert.Equal(_doador, evento.DoadorId);
        Assert.Equal(75.50m, evento.ValorDoacao);
        _doacoes.Verify(r => r.AdicionarAsync(It.IsAny<Doacao>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Doacao_nao_soma_o_valor_na_campanha()
    {
        await _service.CriarAsync(_doador, new CriarDoacaoRequest(_campanha.Id, 75.50m));

        Assert.Equal(0m, _campanha.ValorArrecadado);
    }

    [Fact]
    public async Task Campanha_inexistente_lanca_NotFoundException()
        => await Assert.ThrowsAsync<NotFoundException>(() => _service.CriarAsync(_doador, new CriarDoacaoRequest(Guid.NewGuid(), 10m)));

    [Fact]
    public async Task Campanha_cancelada_nao_publica_evento()
    {
        _campanha.Atualizar("T", "D", _campanha.DataInicio, _campanha.DataFim, 1000m, StatusCampanha.Cancelada, Agora.UtcDateTime);

        await Assert.ThrowsAsync<RegraNegocioException>(() => _service.CriarAsync(_doador, new CriarDoacaoRequest(_campanha.Id, 10m)));
        _publisher.Verify(p => p.PublicarAsync(It.IsAny<DoacaoRecebidaEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SalvarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
