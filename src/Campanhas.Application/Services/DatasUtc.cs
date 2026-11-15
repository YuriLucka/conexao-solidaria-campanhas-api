namespace Campanhas.Application.Services;

// Datas sem fuso (ex.: "2026-12-31T23:59:59") são tratadas como UTC; o PostgreSQL (timestamptz) exige Kind = Utc.
internal static class DatasUtc
{
    public static DateTime Normalizar(DateTime data) => data.Kind switch
    {
        DateTimeKind.Utc => data,
        DateTimeKind.Local => data.ToUniversalTime(),
        _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
    };
}
