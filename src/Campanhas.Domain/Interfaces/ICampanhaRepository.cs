using Campanhas.Domain.Entities;

namespace Campanhas.Domain.Interfaces;

public interface ICampanhaRepository
{
    Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Campanha>> ListarAtivasAsync(CancellationToken ct = default);
    Task AdicionarAsync(Campanha campanha, CancellationToken ct = default);
}
