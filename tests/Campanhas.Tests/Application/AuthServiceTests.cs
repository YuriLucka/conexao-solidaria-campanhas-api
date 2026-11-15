using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Campanhas.Application.Services;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Interfaces;
using Moq;

namespace Campanhas.Tests.Application;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<ISenhaHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly AuthService _service;
    private readonly Usuario _gestora = new("Helena", "helena@ong.demo", "111.444.777-35", "hash", Perfil.GestorONG, DateTime.UtcNow);

    public AuthServiceTests()
    {
        _usuarios.Setup(r => r.ObterPorEmailAsync("helena@ong.demo", It.IsAny<CancellationToken>())).ReturnsAsync(_gestora);
        _service = new AuthService(_usuarios.Object, _hasher.Object, _jwt.Object);
    }

    [Fact]
    public async Task Login_valido_devolve_token()
    {
        var token = new TokenResponse("jwt", "Bearer", DateTime.UtcNow, "Helena", "GestorONG");
        _hasher.Setup(h => h.Verificar("Gestao@2026", "hash")).Returns(true);
        _jwt.Setup(j => j.Gerar(_gestora)).Returns(token);

        var resposta = await _service.LoginAsync(new LoginRequest(" Helena@ONG.demo ", "Gestao@2026"));

        Assert.Same(token, resposta);
    }

    [Fact]
    public async Task Senha_errada_lanca_UnauthorizedAccessException()
    {
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), "hash")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(new LoginRequest("helena@ong.demo", "errada")));
    }

    [Fact]
    public async Task Email_inexistente_lanca_UnauthorizedAccessException()
        => await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(new LoginRequest("ninguem@ong.demo", "x")));
}
