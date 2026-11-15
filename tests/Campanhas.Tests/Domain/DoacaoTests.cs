using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Exceptions;

namespace Campanhas.Tests.Domain;

public class DoacaoTests
{
    private static readonly DateTime Agora = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Doador = Guid.NewGuid();

    private static Campanha CampanhaAtiva() => new("T", "D", Agora.AddDays(-1), Agora.AddDays(10), 1000m, Agora);

    [Fact]
    public void Doacao_nasce_Pendente()
    {
        var campanha = CampanhaAtiva();
        var doacao = new Doacao(campanha, Doador, 50m, Agora);

        Assert.Equal(StatusDoacao.Pendente, doacao.Status);
        Assert.Equal(campanha.Id, doacao.CampanhaId);
        Assert.Equal(50m, doacao.Valor);
        Assert.Null(doacao.ProcessadaEm);
    }

    [Fact]
    public void Doacao_nao_altera_o_valor_arrecadado_da_campanha()
    {
        var campanha = CampanhaAtiva();
        _ = new Doacao(campanha, Doador, 50m, Agora);

        Assert.Equal(0m, campanha.ValorArrecadado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.555)]
    public void Valor_invalido_lanca_DomainException(decimal valor)
        => Assert.Throws<DomainException>(() => new Doacao(CampanhaAtiva(), Doador, valor, Agora));

    [Theory]
    [InlineData(StatusCampanha.Cancelada)]
    [InlineData(StatusCampanha.Concluida)]
    public void Campanha_cancelada_ou_concluida_lanca_RegraNegocioException(StatusCampanha status)
    {
        var campanha = CampanhaAtiva();
        campanha.Atualizar("T", "D", campanha.DataInicio, campanha.DataFim, 1000m, status, Agora);

        Assert.Throws<RegraNegocioException>(() => new Doacao(campanha, Doador, 50m, Agora));
    }

    [Fact]
    public void Campanha_com_prazo_vencido_lanca_RegraNegocioException()
        => Assert.Throws<RegraNegocioException>(() => new Doacao(CampanhaAtiva(), Doador, 50m, Agora.AddDays(11)));
}
