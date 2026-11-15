using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Interfaces;
using Campanhas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Repositories;

public class CampanhaRepository : ICampanhaRepository
{
    private readonly AppDbContext _db;

    public CampanhaRepository(AppDbContext db) => _db = db;

    public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => _db.Campanhas.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken ct = default)
        => await _db.Campanhas.AsNoTracking().OrderByDescending(c => c.CriadaEm).ToListAsync(ct);

    // Mais perto de terminar primeiro.
    public async Task<IReadOnlyList<Campanha>> ListarAtivasAsync(CancellationToken ct = default)
        => await _db.Campanhas.AsNoTracking()
            .Where(c => c.Status == StatusCampanha.Ativa)
            .OrderBy(c => c.DataFim)
            .ToListAsync(ct);

    public async Task AdicionarAsync(Campanha campanha, CancellationToken ct = default)
        => await _db.Campanhas.AddAsync(campanha, ct);
}
