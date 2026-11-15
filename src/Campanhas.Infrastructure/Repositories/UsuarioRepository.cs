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

    // O value object é criado fora da consulta: formato inválido lança DomainException aqui, não dentro do EF.
    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
    {
        var valor = new Email(email);
        return _db.Usuarios.FirstOrDefaultAsync(u => u.Email == valor, ct);
    }

    public Task<bool> EmailExisteAsync(string email, CancellationToken ct = default)
    {
        var valor = new Email(email);
        return _db.Usuarios.AnyAsync(u => u.Email == valor, ct);
    }

    public Task<bool> CpfExisteAsync(string cpf, CancellationToken ct = default)
    {
        var valor = new Cpf(cpf);
        return _db.Usuarios.AnyAsync(u => u.Cpf == valor, ct);
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken ct = default)
        => await _db.Usuarios.AddAsync(usuario, ct);
}
