using Campanhas.Application.DTOs;

namespace Campanhas.Application.Interfaces;

public interface IDoacaoService
{
    Task<DoacaoResponse> CriarAsync(Guid doadorId, CriarDoacaoRequest request, CancellationToken ct = default);
}
