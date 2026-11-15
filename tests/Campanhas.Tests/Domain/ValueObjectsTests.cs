using Campanhas.Domain.Exceptions;
using Campanhas.Domain.ValueObjects;

namespace Campanhas.Tests.Domain;

public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData("111.444.777-35", "11144477735")]
    [InlineData(" 092.275.377-67 ", "09227537767")]
    public void Cpf_valido_guarda_so_os_digitos(string entrada, string esperado)
        => Assert.Equal(esperado, new Cpf(entrada).Value);

    [Theory]
    [InlineData("")]
    [InlineData("529.982.247-24")] // dígito verificador errado
    [InlineData("111.111.111-11")] // todos iguais
    [InlineData("5299822472")]     // 10 dígitos
    [InlineData("529982247255")]   // 12 dígitos
    [InlineData("529a982b247c25")] // letras no meio
    public void Cpf_invalido_lanca_DomainException(string entrada)
        => Assert.Throws<DomainException>(() => new Cpf(entrada));
}

public class EmailTests
{
    [Fact]
    public void Email_e_normalizado_para_minusculo()
        => Assert.Equal("maria@email.com", new Email("  Maria@Email.COM ").Value);

    [Theory]
    [InlineData("")]
    [InlineData("maria")]
    [InlineData("maria@email")]
    [InlineData("maria @email.com")]
    public void Email_invalido_lanca_DomainException(string entrada)
        => Assert.Throws<DomainException>(() => new Email(entrada));
}
