using System.Text.RegularExpressions;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Exceptions;
using Campanhas.Domain.ValueObjects;

namespace Campanhas.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public Cpf Cpf { get; private set; } = null!;
    public string SenhaHash { get; private set; } = string.Empty;
    public Perfil Perfil { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // EF Core
    protected Usuario() { }

    public Usuario(string nome, string email, string cpf, string senhaHash, Perfil perfil, DateTime agora)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome completo é obrigatório.");
        if (nome.Trim().Length > 150)
            throw new DomainException("O nome completo deve ter no máximo 150 caracteres.");
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("Hash de senha não pode ser vazio.");

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Email = new Email(email);
        Cpf = new Cpf(cpf);
        SenhaHash = senhaHash;
        Perfil = perfil;
        CriadoEm = agora;
    }

    // Regra da FCG: mínimo 8 caracteres, com letra, número e caractere especial.
    public static void ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
            throw new DomainException("A senha deve ter no mínimo 8 caracteres.");
        if (!Regex.IsMatch(senha, "[A-Za-z]"))
            throw new DomainException("A senha deve conter ao menos uma letra.");
        if (!Regex.IsMatch(senha, @"\d"))
            throw new DomainException("A senha deve conter ao menos um número.");
        if (!Regex.IsMatch(senha, "[^A-Za-z0-9]"))
            throw new DomainException("A senha deve conter ao menos um caractere especial (por exemplo: @, # ou !).");
    }
}
