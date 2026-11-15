using Campanhas.Application.DTOs;
using Campanhas.Domain.Enums;

namespace Campanhas.Application.Interfaces;

// Leituras para as telas (projeções direto do banco, sem passar pelas entidades).
public interface IConsultas
{
    Task<DoacoesDaCampanhaResponse> DoacoesDaCampanhaAsync(Guid campanhaId, int quantidade, CancellationToken ct = default);
    Task<IReadOnlyList<DoacaoResponse>> DoacoesDoDoadorAsync(Guid doadorId, CancellationToken ct = default);
    Task<DoacaoResponse?> DoacaoDoDoadorAsync(Guid doadorId, Guid doacaoId, CancellationToken ct = default);
    Task<IReadOnlyList<DoacaoGestaoResponse>> DoacoesGestaoAsync(Guid? campanhaId, StatusDoacao? status, int limite, CancellationToken ct = default);
    Task<IReadOnlyList<DoadorGestaoResponse>> DoadoresAsync(CancellationToken ct = default);
    Task<ResumoGestaoResponse> ResumoAsync(int dias, DateTime agora, CancellationToken ct = default);
}
