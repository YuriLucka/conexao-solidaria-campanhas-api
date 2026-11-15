using System.Security.Claims;
using System.Text;
using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Campanhas.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly TimeProvider _relogio;

    public JwtTokenService(IOptions<JwtSettings> settings, TimeProvider relogio)
    {
        _settings = settings.Value;
        _relogio = relogio;
    }

    public TokenResponse Gerar(Usuario usuario)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var expiraEm = agora.AddMinutes(_settings.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expiraEm,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email.Value),
                new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
                new Claim("role", usuario.Perfil.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new TokenResponse(token, "Bearer", expiraEm, usuario.Nome, usuario.Perfil.ToString());
    }
}
