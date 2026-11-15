using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Exceptions;
using Campanhas.Domain.Interfaces;

namespace Campanhas.Application.Services;

public class AuthService : IAuthService
{
    private const string CredenciaisInvalidas = "E-mail ou senha incorretos.";
    // Hash de uma senha descartável, gerado uma vez: usado só para igualar o tempo do login sem usuário.
    private static string? _hashFicticio;

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

        // E-mail com formato inválido não é erro de validação aqui: é só uma credencial que não existe.
        Usuario? usuario = null;
        try { usuario = await _usuarios.ObterPorEmailAsync(request.Email.Trim().ToLowerInvariant(), ct); }
        catch (DomainException) { }

        // O BCrypt roda mesmo sem usuário: o tempo de resposta não revela quais e-mails têm conta.
        var senhaConfere = _hasher.Verificar(request.Senha, usuario?.SenhaHash ?? (_hashFicticio ??= _hasher.Hash(Guid.NewGuid().ToString())));
        if (usuario is null || !senhaConfere)
            throw new UnauthorizedAccessException(CredenciaisInvalidas);

        return _jwt.Gerar(usuario);
    }
}
