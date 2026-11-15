using Campanhas.Application.DTOs;

namespace Campanhas.Application.Interfaces;

// Cache do painel público. Falha do cache nunca derruba a requisição.
public interface ICacheCampanhasAtivas
{
    Task<IReadOnlyList<CampanhaAtivaResponse>?> ObterAsync(CancellationToken ct = default);
    Task DefinirAsync(IReadOnlyList<CampanhaAtivaResponse> campanhas, CancellationToken ct = default);
    Task InvalidarAsync(CancellationToken ct = default);
}
