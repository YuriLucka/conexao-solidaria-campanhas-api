namespace Campanhas.Application.Interfaces;

// Gera o refresh token entregue ao navegador e o hash que fica no banco.
public interface IRefreshTokenService
{
    TimeSpan Validade { get; }
    string Gerar();
    string Hash(string token);
}
