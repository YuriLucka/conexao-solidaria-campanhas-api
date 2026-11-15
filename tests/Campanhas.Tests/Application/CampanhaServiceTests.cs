using Campanhas.Application.DTOs;
using Campanhas.Application.Exceptions;
using Campanhas.Application.Interfaces;
using Campanhas.Application.Services;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Campanhas.Tests.Application;

public class CampanhaServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICampanhaRepository> _campanhas = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICacheCampanhasAtivas> _cache = new();
    private readonly CampanhaService _service;

    public CampanhaServiceTests()
        => _service = new CampanhaService(_campanhas.Object, _uow.Object, _cache.Object, new FakeTimeProvider(Agora));

    [Fact]
    public async Task Criar_grava_campanha_Ativa_e_invalida_o_cache_do_painel()
    {
        var request = new CriarCampanhaRequest("Natal", "Presentes", Agora.UtcDateTime, Agora.UtcDateTime.AddDays(30), 5000m);

        var resposta = await _service.CriarAsync(request);

        Assert.Equal(StatusCampanha.Ativa, resposta.Status);
        _campanhas.Verify(r => r.AdicionarAsync(It.IsAny<Campanha>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.InvalidarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Data_sem_fuso_e_tratada_como_UTC()
    {
        var semFuso = DateTime.SpecifyKind(Agora.UtcDateTime.AddDays(30), DateTimeKind.Unspecified);
        var resposta = await _service.CriarAsync(new CriarCampanhaRequest("Natal", "Presentes", Agora.UtcDateTime, semFuso, 5000m));

        Assert.Equal(DateTimeKind.Utc, resposta.DataFim.Kind);
    }

    [Fact]
    public async Task Atualizar_campanha_inexistente_lanca_NotFoundException()
        => await Assert.ThrowsAsync<NotFoundException>(() => _service.AtualizarAsync(Guid.NewGuid(),
            new AtualizarCampanhaRequest("T", "D", Agora.UtcDateTime, Agora.UtcDateTime.AddDays(1), 10m, StatusCampanha.Ativa)));

    [Fact]
    public async Task Painel_usa_o_cache_quando_existe()
    {
        IReadOnlyList<CampanhaAtivaResponse> emCache = [];
        _cache.Setup(c => c.ObterAsync(It.IsAny<CancellationToken>())).ReturnsAsync(emCache);

        var resposta = await _service.ListarAtivasAsync();

        Assert.Same(emCache, resposta);
        _campanhas.Verify(r => r.ListarAtivasAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Painel_sem_cache_le_do_banco_e_grava_no_cache()
    {
        var campanha = new Campanha("Natal", "Presentes", Agora.UtcDateTime, Agora.UtcDateTime.AddDays(30), 5000m, Agora.UtcDateTime);
        _cache.Setup(c => c.ObterAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<CampanhaAtivaResponse>?)null);
        _campanhas.Setup(r => r.ListarAtivasAsync(It.IsAny<CancellationToken>())).ReturnsAsync([campanha]);

        var resposta = await _service.ListarAtivasAsync();

        var item = Assert.Single(resposta);
        Assert.Equal("Natal", item.Titulo);
        Assert.Equal(5000m, item.MetaFinanceira);
        Assert.Equal(0m, item.ValorTotalArrecadado);
        _cache.Verify(c => c.DefinirAsync(resposta, It.IsAny<CancellationToken>()), Times.Once);
    }
}
