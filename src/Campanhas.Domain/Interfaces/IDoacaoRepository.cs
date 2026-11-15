using Campanhas.Domain.Entities;

namespace Campanhas.Domain.Interfaces;

public interface IDoacaoRepository
{
    Task<IReadOnlyList<Doacao>> ListarPorDoadorAsync(Guid doadorId, CancellationToken ct = default);
    Task AdicionarAsync(Doacao doacao, CancellationToken ct = default);
}
