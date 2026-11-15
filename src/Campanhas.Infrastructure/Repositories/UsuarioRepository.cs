using Campanhas.Domain.Entities;
using Campanhas.Domain.Interfaces;
using Campanhas.Domain.ValueObjects;
using Campanhas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _db;

    public UsuarioRepository(AppDbContext db) => _db = db;

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
        => _db.Usuarios.FirstOrDefaultAsync(u => u.Email == new Email(email), ct);

    public Task<bool> EmailExisteAsync(string email, CancellationToken ct = default)
        => _db.Usuarios.AnyAsync(u => u.Email == new Email(email), ct);

    public Task<bool> CpfExisteAsync(string cpf, CancellationToken ct = default)
        => _db.Usuarios.AnyAsync(u => u.Cpf == new Cpf(cpf), ct);

    public async Task AdicionarAsync(Usuario usuario, CancellationToken ct = default)
        => await _db.Usuarios.AddAsync(usuario, ct);
}
