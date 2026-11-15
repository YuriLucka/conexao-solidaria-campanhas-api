using Campanhas.Application.DTOs;

namespace Campanhas.Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public interface IDoadorService
{
    Task<DoadorResponse> CadastrarAsync(CadastrarDoadorRequest request, CancellationToken ct = default);
}

public interface ICampanhaService
{
    Task<CampanhaResponse> CriarAsync(CriarCampanhaRequest request, CancellationToken ct = default);
    Task<CampanhaResponse> AtualizarAsync(Guid id, AtualizarCampanhaRequest request, CancellationToken ct = default);
    Task<CampanhaResponse> ObterAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CampanhaResponse>> ListarAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CampanhaAtivaResponse>> ListarAtivasAsync(CancellationToken ct = default);
}

public interface IDoacaoService
{
    Task<DoacaoResponse> CriarAsync(Guid doadorId, CriarDoacaoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DoacaoResponse>> ListarDoDoadorAsync(Guid doadorId, CancellationToken ct = default);
}
