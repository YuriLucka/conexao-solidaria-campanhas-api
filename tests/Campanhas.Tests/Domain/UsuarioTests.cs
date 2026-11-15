using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Exceptions;

namespace Campanhas.Tests.Domain;

public class UsuarioTests
{
    private static readonly DateTime Agora = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Cria_usuario_com_email_e_cpf_normalizados()
    {
        var usuario = new Usuario(" Maria Silva ", "MARIA@EMAIL.COM", "529.982.247-25", "hash", Perfil.Doador, Agora);

        Assert.Equal("Maria Silva", usuario.Nome);
        Assert.Equal("maria@email.com", usuario.Email.Value);
        Assert.Equal("52998224725", usuario.Cpf.Value);
        Assert.Equal(Perfil.Doador, usuario.Perfil);
    }

    [Fact]
    public void Nome_vazio_lanca_DomainException()
        => Assert.Throws<DomainException>(() => new Usuario(" ", "maria@email.com", "529.982.247-25", "hash", Perfil.Doador, Agora));

    [Theory]
    [InlineData("Curta@1")]      // menos de 8
    [InlineData("SemNumero@")]   // sem número
    [InlineData("12345678@")]    // sem letra
    [InlineData("SemEspecial1")] // sem caractere especial
    public void Senha_fraca_lanca_DomainException(string senha)
        => Assert.Throws<DomainException>(() => Usuario.ValidarSenha(senha));

    [Fact]
    public void Senha_forte_passa()
        => Usuario.ValidarSenha("Doar@2026");
}
