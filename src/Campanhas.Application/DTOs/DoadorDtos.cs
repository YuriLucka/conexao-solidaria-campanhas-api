namespace Campanhas.Application.DTOs;

/// <summary>Dados para criar a conta de um doador.</summary>
/// <param name="NomeCompleto">Nome completo.</param>
/// <param name="Email">E-mail (único).</param>
/// <param name="Cpf">CPF com ou sem máscara (único, dígitos verificadores conferidos).</param>
/// <param name="Senha">Mínimo 8 caracteres, com letra, número e caractere especial.</param>
public record CadastrarDoadorRequest(string NomeCompleto, string Email, string Cpf, string Senha);

/// <summary>Doador criado.</summary>
public record DoadorResponse(Guid Id, string NomeCompleto, string Email);
