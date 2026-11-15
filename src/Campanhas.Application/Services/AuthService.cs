using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Interfaces;

namespace Campanhas.Application.Services;

public class AuthService : IAuthService
{
    private const string CredenciaisInvalidas = "E-mail ou senha incorretos.";

    private readonly IUsuarioRepository _usuarios;
    private readonly ISenhaHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthService(IUsuarioRepository usuarios, ISenhaHasher hasher, IJwtTokenService jwt)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
            throw new UnauthorizedAccessException(CredenciaisInvalidas);

        var usuario = await _usuarios.ObterPorEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException(CredenciaisInvalidas);

        if (!_hasher.Verificar(request.Senha, usuario.SenhaHash))
            throw new UnauthorizedAccessException(CredenciaisInvalidas);

        return _jwt.Gerar(usuario);
    }
}
