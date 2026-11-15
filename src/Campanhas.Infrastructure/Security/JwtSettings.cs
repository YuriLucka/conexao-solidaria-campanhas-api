namespace Campanhas.Infrastructure.Security;

// Seção "JwtSettings". SecretKey vem de Secret (k8s) ou .env (Compose), nunca do repositório.
public class JwtSettings
{
    public const string Secao = "JwtSettings";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "conexao-solidaria";
    public string Audience { get; set; } = "conexao-solidaria";
    // JWT curto; a sessão continua pelo refresh token (cookie HttpOnly), renovado a cada uso.
    public int ExpirationMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
