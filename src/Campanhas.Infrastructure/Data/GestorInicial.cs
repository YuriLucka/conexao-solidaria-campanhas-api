using Campanhas.Application.Interfaces;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Campanhas.Infrastructure.Data;

// Conta de gestão criada na primeira subida (GestorInicial__Email, __Senha, __Nome, __Cpf), mesmo sem os dados de
// demonstração: um site recém-publicado precisa de alguém da ONG para cadastrar as campanhas.
public static class GestorInicial
{
    public static async Task<bool> GarantirAsync(AppDbContext db, ISenhaHasher hasher, IConfiguration config, DateTime agora,
        CancellationToken ct = default)
    {
        var email = config["GestorInicial:Email"];
        var senha = config["GestorInicial:Senha"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return false;
        if (await db.Usuarios.AnyAsync(u => u.Perfil == Perfil.GestorONG, ct))
            return false;

        db.Usuarios.Add(new Usuario(config["GestorInicial:Nome"] ?? "Gestão da ONG", email, config["GestorInicial:Cpf"] ?? "111.444.777-35",
            hasher.Hash(senha), Perfil.GestorONG, agora));
        await db.SaveChangesAsync(ct);
        return true;
    }
}
