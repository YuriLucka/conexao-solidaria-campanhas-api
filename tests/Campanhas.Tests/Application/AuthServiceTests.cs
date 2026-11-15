using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Campanhas.Application.Services;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Campanhas.Tests.Application;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<ISenhaHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly RefreshTokensEmMemoria _refresh = new();
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly AuthService _service;
    private readonly Usuario _gestora = new("Helena", "helena@ong.demo", "111.444.777-35", "hash", Perfil.GestorONG, DateTime.UtcNow);
    private readonly TokenResponse _token = new("jwt", "Bearer", DateTime.UtcNow, "Helena", "GestorONG");

    public AuthServiceTests()
    {
        _usuarios.Setup(r => r.ObterPorEmailAsync("helena@ong.demo", It.IsAny<CancellationToken>())).ReturnsAsync(_gestora);
        _usuarios.Setup(r => r.ObterPorIdAsync(_gestora.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_gestora);
        _hasher.Setup(h => h.Verificar("Gestao@2026", "hash")).Returns(true);
        _jwt.Setup(j => j.Gerar(_gestora)).Returns(_token);
        _service = new AuthService(_usuarios.Object, _refresh, _hasher.Object, _jwt.Object, new GeradorFalso(), _uow.Object, _relogio);
    }

    private Task<SessaoAutenticada> EntrarAsync() => _service.LoginAsync(new LoginRequest("helena@ong.demo", "Gestao@2026"));

    [Fact]
    public async Task Login_valido_devolve_token_e_refresh_token_guardado_so_como_hash()
    {
        var sessao = await _service.LoginAsync(new LoginRequest(" Helena@ONG.demo ", "Gestao@2026"));

        Assert.Same(_token, sessao.Token);
        Assert.Equal(_relogio.GetUtcNow().UtcDateTime.AddDays(7), sessao.RefreshExpiraEm);
        var guardado = Assert.Single(_refresh.Todos);
        Assert.Equal("hash:" + sessao.RefreshToken, guardado.TokenHash);
        Assert.Equal(_gestora.Id, guardado.UsuarioId);
    }

    [Fact]
    public async Task Senha_errada_lanca_UnauthorizedAccessException()
    {
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), "hash")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(new LoginRequest("helena@ong.demo", "errada")));
        Assert.Empty(_refresh.Todos);
    }

    [Fact]
    public async Task Email_inexistente_lanca_UnauthorizedAccessException()
        => await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(new LoginRequest("ninguem@ong.demo", "x")));

    [Fact]
    public async Task Renovar_troca_o_refresh_token_e_mantem_a_familia()
    {
        var login = await EntrarAsync();

        var renovada = await _service.RenovarAsync(login.RefreshToken);

        Assert.NotEqual(login.RefreshToken, renovada.RefreshToken);
        Assert.Same(_token, renovada.Token);
        var antigo = _refresh.PorToken(login.RefreshToken);
        var novo = _refresh.PorToken(renovada.RefreshToken);
        Assert.NotNull(antigo.SubstituidoEm);
        Assert.Equal(antigo.Familia, novo.Familia);
        Assert.True(novo.Ativo(_relogio.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public async Task Token_trocado_usado_de_novo_depois_da_tolerancia_derruba_a_familia()
    {
        var login = await EntrarAsync();
        var renovada = await _service.RenovarAsync(login.RefreshToken);
        _relogio.Advance(TimeSpan.FromMinutes(1));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RenovarAsync(login.RefreshToken));

        Assert.NotNull(_refresh.PorToken(renovada.RefreshToken).RevogadoEm);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RenovarAsync(renovada.RefreshToken));
    }

    [Fact]
    public async Task Duas_abas_renovando_juntas_nao_derrubam_a_sessao()
    {
        var login = await EntrarAsync();
        await _service.RenovarAsync(login.RefreshToken);
        _relogio.Advance(TimeSpan.FromSeconds(5));

        var outraAba = await _service.RenovarAsync(login.RefreshToken);

        Assert.True(_refresh.PorToken(outraAba.RefreshToken).Ativo(_relogio.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public async Task Refresh_token_vencido_lanca_UnauthorizedAccessException()
    {
        var login = await EntrarAsync();
        _relogio.Advance(TimeSpan.FromDays(8));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RenovarAsync(login.RefreshToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("token-que-nao-existe")]
    public async Task Renovar_sem_token_valido_lanca_UnauthorizedAccessException(string? token)
        => await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RenovarAsync(token));

    [Fact]
    public async Task Sair_revoga_a_sessao()
    {
        var login = await EntrarAsync();

        await _service.SairAsync(login.RefreshToken);

        Assert.NotNull(_refresh.PorToken(login.RefreshToken).RevogadoEm);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RenovarAsync(login.RefreshToken));
    }

    [Fact]
    public async Task Sair_sem_token_nao_falha()
        => await _service.SairAsync(null);

    private sealed class GeradorFalso : IRefreshTokenService
    {
        public TimeSpan Validade => TimeSpan.FromDays(7);
        public string Gerar() => Guid.NewGuid().ToString("N");
        public string Hash(string token) => "hash:" + token;
    }

    private sealed class RefreshTokensEmMemoria : IRefreshTokenRepository
    {
        public List<RefreshToken> Todos { get; } = [];

        public RefreshToken PorToken(string token) => Todos.Single(t => t.TokenHash == "hash:" + token);

        public Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct = default)
            => Task.FromResult(Todos.FirstOrDefault(t => t.TokenHash == tokenHash));

        public Task<IReadOnlyList<RefreshToken>> ListarDaFamiliaAsync(Guid familia, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RefreshToken>>(Todos.Where(t => t.Familia == familia).ToList());

        public Task AdicionarAsync(RefreshToken token, CancellationToken ct = default)
        {
            Todos.Add(token);
            return Task.CompletedTask;
        }
    }
}
