using Campanhas.Domain.Entities;

namespace Campanhas.Domain.Interfaces;

public interface IDoacaoRepository
{
    Task AdicionarAsync(Doacao doacao, CancellationToken ct = default);
}
