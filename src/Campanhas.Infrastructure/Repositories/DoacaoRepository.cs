using Campanhas.Domain.Entities;
using Campanhas.Domain.Interfaces;
using Campanhas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Repositories;

public class DoacaoRepository : IDoacaoRepository
{
    private readonly AppDbContext _db;

    public DoacaoRepository(AppDbContext db) => _db = db;

    public async Task AdicionarAsync(Doacao doacao, CancellationToken ct = default)
        => await _db.Doacoes.AddAsync(doacao, ct);
}
