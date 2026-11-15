namespace Campanhas.Infrastructure.Security;

// Seção "JwtSettings". SecretKey vem de Secret (k8s) ou .env (Compose), nunca do repositório.
public class JwtSettings
{
    public const string Secao = "JwtSettings";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "conexao-solidaria";
    public string Audience { get; set; } = "conexao-solidaria";
    public int ExpirationMinutes { get; set; } = 120;
}
