using Campanhas.Application.DTOs;
using Campanhas.Domain.Enums;

namespace Campanhas.Application.Interfaces;

// Leituras para as telas (projeções direto do banco, sem passar pelas entidades).
public interface IConsultas
{
    Task<DoacoesDaCampanhaResponse> DoacoesDaCampanhaAsync(Guid campanhaId, int quantidade, CancellationToken ct = default);
    Task<MinhasDoacoesPaginaResponse> DoacoesDoDoadorAsync(Guid doadorId, int pagina, int tamanho, CancellationToken ct = default);
    Task<DoacaoResponse?> DoacaoDoDoadorAsync(Guid doadorId, Guid doacaoId, CancellationToken ct = default);
    Task<DoacoesGestaoPaginaResponse> DoacoesGestaoAsync(Guid? campanhaId, StatusDoacao? status, string? busca, int pagina, int tamanho, CancellationToken ct = default);
    Task<DoadoresGestaoPaginaResponse> DoadoresAsync(string? busca, int pagina, int tamanho, DateTime agora, CancellationToken ct = default);
    Task<ResumoGestaoResponse> ResumoAsync(int dias, DateTime agora, CancellationToken ct = default);
}
