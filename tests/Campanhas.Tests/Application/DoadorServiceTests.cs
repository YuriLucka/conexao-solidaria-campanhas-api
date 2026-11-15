using Campanhas.Application.DTOs;
using Campanhas.Application.Exceptions;
using Campanhas.Application.Interfaces;
using Campanhas.Application.Services;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Exceptions;
using Campanhas.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Campanhas.Tests.Application;

public class DoadorServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISenhaHasher> _hasher = new();
    private readonly DoadorService _service;

    private static readonly CadastrarDoadorRequest Valido = new("Maria Silva", "maria@email.com", "529.982.247-25", "Doar@2026");

    public DoadorServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-bcrypt");
        _service = new DoadorService(_usuarios.Object, _uow.Object, _hasher.Object, new FakeTimeProvider());
    }

    [Fact]
    public async Task Cadastro_cria_sempre_perfil_Doador_com_senha_em_hash()
    {
        Usuario? salvo = null;
        _usuarios.Setup(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Callback<Usuario, CancellationToken>((u, _) => salvo = u);

        var resposta = await _service.CadastrarAsync(Valido);

        Assert.NotNull(salvo);
        Assert.Equal(Perfil.Doador, salvo!.Perfil);
        Assert.Equal("hash-bcrypt", salvo.SenhaHash);
        Assert.Equal("52998224725", salvo.Cpf.Value);
        Assert.Equal(salvo.Id, resposta.Id);
        _uow.Verify(u => u.SalvarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Email_repetido_lanca_ConflictException()
    {
        _usuarios.Setup(r => r.EmailExisteAsync("maria@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CadastrarAsync(Valido));
        _uow.Verify(u => u.SalvarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cpf_repetido_lanca_ConflictException()
    {
        _usuarios.Setup(r => r.CpfExisteAsync("52998224725", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CadastrarAsync(Valido));
    }

    [Fact]
    public async Task Cpf_invalido_lanca_DomainException()
        => await Assert.ThrowsAsync<DomainException>(() => _service.CadastrarAsync(Valido with { Cpf = "123.456.789-00" }));
}
