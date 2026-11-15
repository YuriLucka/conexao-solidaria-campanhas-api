using Campanhas.Application.DTOs;

namespace Campanhas.Application.Interfaces;

public interface IDoadorService
{
    Task<DoadorResponse> CadastrarAsync(CadastrarDoadorRequest request, CancellationToken ct = default);
}
