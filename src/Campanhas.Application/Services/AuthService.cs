using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Exceptions;
using Campanhas.Domain.Interfaces;

namespace Campanhas.Application.Services;

// Login, renovação e saída. O JWT é curto; a sessão continua pelo refresh token, trocado a cada renovação.
public class AuthService : IAuthService
{
    private const string CredenciaisInvalidas = "E-mail ou senha incorretos.";
    private const string SessaoExpirada = "Sua sessão expirou. Entre de novo para continuar.";
    // Duas abas renovando ao mesmo tempo apresentam o mesmo token; dentro desta janela isso não é tratado como cópia.
    private static readonly TimeSpan ToleranciaRenovacao = TimeSpan.FromSeconds(30);
    // Hash de uma senha descartável, gerado uma vez: usado só para igualar o tempo do login sem usuário.
    private static string? _hashFicticio;

    private readonly IUsuarioRepository _usuarios;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISenhaHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenService _geradorRefresh;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _relogio;

    public AuthService(IUsuarioRepository usuarios, IRefreshTokenRepository refreshTokens, ISenhaHasher hasher, IJwtTokenService jwt,
        IRefreshTokenService geradorRefresh, IUnitOfWork uow, TimeProvider relogio)
    {
        _usuarios = usuarios;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _jwt = jwt;
        _geradorRefresh = geradorRefresh;
        _uow = uow;
        _relogio = relogio;
    }

    private DateTime Agora => _relogio.GetUtcNow().UtcDateTime;

    public async Task<SessaoAutenticada> LoginAsync(LoginRequest request, CancellationToken ct = default)
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

        return await AbrirSessaoAsync(usuario, familia: Guid.NewGuid(), ct);
    }

    public async Task<SessaoAutenticada> RenovarAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException(SessaoExpirada);

        var agora = Agora;
        var atual = await _refreshTokens.ObterPorHashAsync(_geradorRefresh.Hash(refreshToken), ct)
            ?? throw new UnauthorizedAccessException(SessaoExpirada);

        if (!atual.Ativo(agora) && !atual.TrocadoHaPouco(agora, ToleranciaRenovacao))
        {
            // Token já trocado sendo usado de novo: alguém pode ter copiado. Encerra todas as sessões daquela família.
            if (atual.SubstituidoEm is not null && atual.RevogadoEm is null)
            {
                await RevogarFamiliaAsync(atual.Familia, agora, ct);
                await _uow.SalvarAsync(ct);
            }
            throw new UnauthorizedAccessException(SessaoExpirada);
        }

        var usuario = await _usuarios.ObterPorIdAsync(atual.UsuarioId, ct)
            ?? throw new UnauthorizedAccessException(SessaoExpirada);

        atual.MarcarSubstituido(agora);
        return await AbrirSessaoAsync(usuario, atual.Familia, ct);
    }

    // Sair não falha: sem cookie, ou com token desconhecido, só não há o que revogar.
    public async Task SairAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var atual = await _refreshTokens.ObterPorHashAsync(_geradorRefresh.Hash(refreshToken), ct);
        if (atual is null)
            return;

        await RevogarFamiliaAsync(atual.Familia, Agora, ct);
        await _uow.SalvarAsync(ct);
    }

    private async Task<SessaoAutenticada> AbrirSessaoAsync(Usuario usuario, Guid familia, CancellationToken ct)
    {
        var agora = Agora;
        var refresh = _geradorRefresh.Gerar();
        var expiraEm = agora + _geradorRefresh.Validade;

        await _refreshTokens.AdicionarAsync(new RefreshToken(usuario.Id, familia, _geradorRefresh.Hash(refresh), agora, expiraEm), ct);
        await _uow.SalvarAsync(ct);

        return new SessaoAutenticada(_jwt.Gerar(usuario), refresh, expiraEm);
    }

    private async Task RevogarFamiliaAsync(Guid familia, DateTime agora, CancellationToken ct)
    {
        foreach (var token in await _refreshTokens.ListarDaFamiliaAsync(familia, ct))
            token.Revogar(agora);
    }
}
