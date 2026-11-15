namespace Campanhas.Application.Common;

// Datas sem fuso (ex.: "2026-12-31T23:59:59" ou "2026-12-31") são do horário de Brasília (UTC-3, sem horário de verão
// desde 2019) e viram UTC; o PostgreSQL (timestamptz) exige Kind = Utc.
// Término informado só com a data ("2026-12-31") vale até o fim daquele dia.
public static class DatasUtc
{
    public static readonly TimeSpan Brasilia = TimeSpan.FromHours(-3);

    public static DateTime Normalizar(DateTime data, bool fimDoDia = false)
    {
        switch (data.Kind)
        {
            case DateTimeKind.Utc:
                return data;
            case DateTimeKind.Local:
                return data.ToUniversalTime();
            default:
                if (fimDoDia && data.TimeOfDay == TimeSpan.Zero)
                    data = data.AddDays(1).AddSeconds(-1);
                return DateTime.SpecifyKind(data - Brasilia, DateTimeKind.Utc);
        }
    }

    // Data (dia do calendário) em Brasília de um instante UTC.
    public static DateOnly DiaEmBrasilia(DateTime utc) => DateOnly.FromDateTime(utc + Brasilia);
}
