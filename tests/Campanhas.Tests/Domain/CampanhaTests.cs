using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Exceptions;

namespace Campanhas.Tests.Domain;

public class CampanhaTests
{
    private static readonly DateTime Agora = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    private static Campanha Nova(DateTime? fim = null, decimal meta = 1000m, DateTime? agora = null)
        => new("Natal Esperança", "Presentes para as crianças", Agora.AddDays(-1), fim ?? Agora.AddDays(30), meta, agora ?? Agora);

    [Fact]
    public void Campanha_nasce_Ativa_e_sem_valor_arrecadado()
    {
        var campanha = Nova();

        Assert.Equal(StatusCampanha.Ativa, campanha.Status);
        Assert.Equal(0m, campanha.ValorArrecadado);
        Assert.True(campanha.AceitaDoacao(Agora));
    }

    [Fact]
    public void Termino_no_passado_lanca_DomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Nova(fim: Agora.AddMinutes(-1)));
        Assert.Contains("passado", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Meta_menor_ou_igual_a_zero_lanca_DomainException(decimal meta)
    {
        var ex = Assert.Throws<DomainException>(() => Nova(meta: meta));
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void Inicio_depois_do_termino_lanca_DomainException()
        => Assert.Throws<DomainException>(() =>
            new Campanha("T", "D", Agora.AddDays(10), Agora.AddDays(5), 100m, Agora));

    [Fact]
    public void Titulo_vazio_lanca_DomainException()
        => Assert.Throws<DomainException>(() =>
            new Campanha(" ", "D", Agora, Agora.AddDays(5), 100m, Agora));

    [Fact]
    public void Editar_altera_campos_e_status()
    {
        var campanha = Nova();

        campanha.Atualizar("Novo título", "Nova descrição", campanha.DataInicio, Agora.AddDays(60), 2000m, StatusCampanha.Concluida, Agora);

        Assert.Equal("Novo título", campanha.Titulo);
        Assert.Equal(2000m, campanha.MetaFinanceira);
        Assert.Equal(StatusCampanha.Concluida, campanha.Status);
        Assert.False(campanha.AceitaDoacao(Agora));
    }

    [Fact]
    public void Editar_com_meta_zero_lanca_DomainException()
    {
        var campanha = Nova();
        Assert.Throws<DomainException>(() =>
            campanha.Atualizar("T", "D", campanha.DataInicio, campanha.DataFim, 0m, StatusCampanha.Ativa, Agora));
    }

    [Fact]
    public void Editar_mudando_termino_para_o_passado_lanca_DomainException()
    {
        var campanha = Nova();
        Assert.Throws<DomainException>(() =>
            campanha.Atualizar("T", "D", campanha.DataInicio.AddDays(-5), Agora.AddDays(-1), 100m, StatusCampanha.Ativa, Agora));
    }

    [Fact]
    public void Campanha_vencida_pode_ter_descricao_editada_sem_mudar_o_termino()
    {
        var campanha = Nova(fim: Agora.AddDays(1));
        var depois = Agora.AddDays(5);

        campanha.Atualizar("T", "Descrição corrigida", campanha.DataInicio, campanha.DataFim, 100m, StatusCampanha.Ativa, depois);

        Assert.Equal("Descrição corrigida", campanha.Descricao);
    }

    [Fact]
    public void Concluida_pode_voltar_a_Ativa_com_termino_no_futuro()
    {
        var campanha = Nova();
        campanha.Atualizar("T", "D", campanha.DataInicio, campanha.DataFim, 100m, StatusCampanha.Concluida, Agora);

        campanha.Atualizar("T", "D", campanha.DataInicio, campanha.DataFim, 100m, StatusCampanha.Ativa, Agora);

        Assert.Equal(StatusCampanha.Ativa, campanha.Status);
    }

    [Fact]
    public void Reabrir_com_termino_vencido_lanca_DomainException()
    {
        var campanha = Nova(fim: Agora.AddDays(1));
        campanha.Atualizar("T", "D", campanha.DataInicio, campanha.DataFim, 100m, StatusCampanha.Cancelada, Agora);

        Assert.Throws<DomainException>(() =>
            campanha.Atualizar("T", "D", campanha.DataInicio, campanha.DataFim, 100m, StatusCampanha.Ativa, Agora.AddDays(5)));
    }

    [Fact]
    public void Ativa_com_prazo_vencido_nao_aceita_doacao()
    {
        var campanha = Nova(fim: Agora.AddDays(1));
        var depois = Agora.AddDays(2);

        Assert.True(campanha.PrazoEncerrado(depois));
        Assert.False(campanha.AceitaDoacao(depois));
        Assert.Equal(StatusCampanha.Ativa, campanha.Status);
    }
}
