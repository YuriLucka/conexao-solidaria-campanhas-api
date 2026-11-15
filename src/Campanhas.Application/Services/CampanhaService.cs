using Campanhas.Application.Common;
using Campanhas.Application.DTOs;
using Campanhas.Application.Exceptions;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Interfaces;

namespace Campanhas.Application.Services;

public class CampanhaService : ICampanhaService
{
    private readonly ICampanhaRepository _campanhas;
    private readonly IUnitOfWork _uow;
    private readonly ICacheCampanhasAtivas _cache;
    private readonly TimeProvider _relogio;

    public CampanhaService(ICampanhaRepository campanhas, IUnitOfWork uow, ICacheCampanhasAtivas cache, TimeProvider relogio)
    {
        _campanhas = campanhas;
        _uow = uow;
        _cache = cache;
        _relogio = relogio;
    }

    private DateTime Agora => _relogio.GetUtcNow().UtcDateTime;

    public async Task<CampanhaResponse> CriarAsync(CriarCampanhaRequest request, CancellationToken ct = default)
    {
        var campanha = new Campanha(
            request.Titulo, request.Descricao,
            DatasUtc.Normalizar(request.DataInicio), DatasUtc.Normalizar(request.DataFim, fimDoDia: true),
            request.MetaFinanceira, Agora, request.Categoria, request.Capa, request.CriancasAtendidas);

        await _campanhas.AdicionarAsync(campanha, ct);
        await _uow.SalvarAsync(ct);
        await _cache.InvalidarAsync(ct);

        return CampanhaResponse.De(campanha, Agora);
    }

    public async Task<CampanhaResponse> AtualizarAsync(Guid id, AtualizarCampanhaRequest request, CancellationToken ct = default)
    {
        var campanha = await _campanhas.ObterPorIdAsync(id, ct)
            ?? throw new NotFoundException("Campanha não encontrada.");

        campanha.Atualizar(
            request.Titulo, request.Descricao,
            DatasUtc.Normalizar(request.DataInicio), DatasUtc.Normalizar(request.DataFim, fimDoDia: true),
            request.MetaFinanceira, request.Status, Agora, request.Categoria, request.Capa, request.CriancasAtendidas);

        await _uow.SalvarAsync(ct);
        await _cache.InvalidarAsync(ct);

        return CampanhaResponse.De(campanha, Agora);
    }

    public async Task<CampanhaResponse> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var campanha = await _campanhas.ObterPorIdAsync(id, ct)
            ?? throw new NotFoundException("Campanha não encontrada.");
        return CampanhaResponse.De(campanha, Agora);
    }

    public async Task<IReadOnlyList<CampanhaResponse>> ListarAsync(CancellationToken ct = default)
    {
        var agora = Agora;
        return (await _campanhas.ListarAsync(ct)).Select(c => CampanhaResponse.De(c, agora)).ToList();
    }

    // Painel público: cache-aside no Redis. O worker apaga a chave ao processar uma doação.
    public async Task<IReadOnlyList<CampanhaAtivaResponse>> ListarAtivasAsync(CancellationToken ct = default)
    {
        var agora = Agora;
        var emCache = await _cache.ObterAsync(ct);
        if (emCache is not null)
            // O prazo é recalculado a cada leitura: não fica preso ao momento em que o cache foi gravado.
            return emCache.Select(c => c with { PrazoEncerrado = c.DataFim < agora }).ToList();

        var ativas = (await _campanhas.ListarAtivasAsync(ct)).Select(c => CampanhaAtivaResponse.De(c, agora)).ToList();
        await _cache.DefinirAsync(ativas, ct);
        return ativas;
    }
}
