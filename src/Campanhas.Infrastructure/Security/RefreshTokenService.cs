using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Campanhas.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Campanhas.Infrastructure.Security;

// 32 bytes aleatórios (256 bits) em base64url. Com essa entropia, SHA-256 sem sal basta para guardar o hash.
public class RefreshTokenService : IRefreshTokenService
{
    private readonly JwtSettings _settings;

    public RefreshTokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public TimeSpan Validade => TimeSpan.FromDays(_settings.RefreshTokenDays);

    public string Gerar() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
