using Campanhas.Application.Services;

namespace Campanhas.Tests.Application;

public class DatasUtcTests
{
    [Fact]
    public void Data_com_fuso_UTC_fica_igual()
    {
        var utc = new DateTime(2026, 12, 31, 15, 0, 0, DateTimeKind.Utc);
        Assert.Equal(utc, DatasUtc.Normalizar(utc));
    }

    [Fact]
    public void Data_sem_fuso_e_horario_de_Brasilia()
    {
        var resultado = DatasUtc.Normalizar(new DateTime(2026, 12, 31, 20, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 12, 31, 23, 0, 0, DateTimeKind.Utc), resultado);
        Assert.Equal(DateTimeKind.Utc, resultado.Kind);
    }

    [Fact]
    public void Termino_so_com_a_data_vale_ate_o_fim_do_dia_em_Brasilia()
        => Assert.Equal(new DateTime(2027, 1, 1, 2, 59, 59, DateTimeKind.Utc),
            DatasUtc.Normalizar(new DateTime(2026, 12, 31), fimDoDia: true));

    [Fact]
    public void Inicio_so_com_a_data_comeca_na_meia_noite_de_Brasilia()
        => Assert.Equal(new DateTime(2026, 12, 31, 3, 0, 0, DateTimeKind.Utc),
            DatasUtc.Normalizar(new DateTime(2026, 12, 31)));

    [Fact]
    public void Dia_em_Brasilia_de_um_instante_UTC()
        => Assert.Equal(new DateOnly(2026, 11, 14), DatasUtc.DiaEmBrasilia(new DateTime(2026, 11, 15, 1, 0, 0, DateTimeKind.Utc)));
}
