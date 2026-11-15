using Campanhas.Domain.Entities;

namespace Campanhas.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IReadOnlyList<RefreshToken>> ListarDaFamiliaAsync(Guid familia, CancellationToken ct = default);
    Task AdicionarAsync(RefreshToken token, CancellationToken ct = default);
}
