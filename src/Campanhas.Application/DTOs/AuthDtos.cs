namespace Campanhas.Application.DTOs;

/// <summary>Credenciais de acesso.</summary>
/// <param name="Email">E-mail cadastrado.</param>
/// <param name="Senha">Senha em texto (trafega só no login).</param>
public record LoginRequest(string Email, string Senha);

/// <summary>Token JWT para o cabeçalho <c>Authorization: Bearer</c>.</summary>
/// <param name="AccessToken">JWT assinado (HS256).</param>
/// <param name="TokenType">Sempre <c>Bearer</c>.</param>
/// <param name="ExpiraEm">Expiração do token (UTC).</param>
/// <param name="Nome">Nome do usuário autenticado.</param>
/// <param name="Perfil"><c>GestorONG</c> ou <c>Doador</c>.</param>
public record TokenResponse(string AccessToken, string TokenType, DateTime ExpiraEm, string Nome, string Perfil);
