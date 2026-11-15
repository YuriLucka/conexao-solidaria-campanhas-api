using Campanhas.Domain.Entities;
using Campanhas.Domain.Interfaces;
using Campanhas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> ListarDaFamiliaAsync(Guid familia, CancellationToken ct = default)
        => await _db.RefreshTokens.Where(t => t.Familia == familia).ToListAsync(ct);

    public async Task AdicionarAsync(RefreshToken token, CancellationToken ct = default)
        => await _db.RefreshTokens.AddAsync(token, ct);
}
