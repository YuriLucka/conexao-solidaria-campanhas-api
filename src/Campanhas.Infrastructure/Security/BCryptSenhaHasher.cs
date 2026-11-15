using Campanhas.Application.Interfaces;

namespace Campanhas.Infrastructure.Security;

public class BCryptSenhaHasher : ISenhaHasher
{
    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public bool Verificar(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
