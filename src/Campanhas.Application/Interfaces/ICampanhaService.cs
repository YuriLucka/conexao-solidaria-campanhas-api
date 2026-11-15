using Campanhas.Application.DTOs;

namespace Campanhas.Application.Interfaces;

public interface ICampanhaService
{
    Task<CampanhaResponse> CriarAsync(CriarCampanhaRequest request, CancellationToken ct = default);
    Task<CampanhaResponse> AtualizarAsync(Guid id, AtualizarCampanhaRequest request, CancellationToken ct = default);
    Task<CampanhaResponse> ObterAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CampanhaResponse>> ListarAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CampanhaAtivaResponse>> ListarAtivasAsync(CancellationToken ct = default);
}
