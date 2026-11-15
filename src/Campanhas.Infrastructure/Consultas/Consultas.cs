using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Campanhas.Application.Services;
using Campanhas.Domain.Enums;
using Campanhas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Consultas;

public class Consultas : IConsultas
{
    private readonly AppDbContext _db;

    public Consultas(AppDbContext db) => _db = db;

    public async Task<DoacoesDaCampanhaResponse> DoacoesDaCampanhaAsync(Guid campanhaId, int quantidade, CancellationToken ct = default)
    {
        var processadas = _db.Doacoes.AsNoTracking()
            .Where(d => d.CampanhaId == campanhaId && d.Status == StatusDoacao.Processada);

        var total = await processadas.CountAsync(ct);
        var doadores = await processadas.Select(d => d.DoadorId).Distinct().CountAsync(ct);
        var recentes = await processadas
            .OrderByDescending(d => d.ProcessadaEm)
            .Take(quantidade)
            .Join(_db.Usuarios, d => d.DoadorId, u => u.Id, (d, u) => new { u.Nome, d.Valor, d.ProcessadaEm })
            .ToListAsync(ct);

        return new DoacoesDaCampanhaResponse(total, doadores,
            recentes.Select(r => new DoacaoRecenteResponse(NomeAbreviado(r.Nome), r.Valor, r.ProcessadaEm!.Value)).ToList());
    }

    public async Task<IReadOnlyList<DoacaoResponse>> DoacoesDoDoadorAsync(Guid doadorId, CancellationToken ct = default)
        => await DoacoesComTitulo().Where(d => d.Doacao.DoadorId == doadorId)
            .OrderByDescending(d => d.Doacao.CriadaEm)
            .Select(d => new DoacaoResponse(d.Doacao.Id, d.Doacao.CampanhaId, d.Titulo, d.Doacao.Valor, d.Doacao.Status,
                d.Doacao.MotivoRejeicao, d.Doacao.CriadaEm, d.Doacao.ProcessadaEm))
            .ToListAsync(ct);

    public async Task<DoacaoResponse?> DoacaoDoDoadorAsync(Guid doadorId, Guid doacaoId, CancellationToken ct = default)
        => await DoacoesComTitulo().Where(d => d.Doacao.DoadorId == doadorId && d.Doacao.Id == doacaoId)
            .Select(d => new DoacaoResponse(d.Doacao.Id, d.Doacao.CampanhaId, d.Titulo, d.Doacao.Valor, d.Doacao.Status,
                d.Doacao.MotivoRejeicao, d.Doacao.CriadaEm, d.Doacao.ProcessadaEm))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<DoacaoGestaoResponse>> DoacoesGestaoAsync(Guid? campanhaId, StatusDoacao? status, int limite, CancellationToken ct = default)
    {
        var consulta = from d in _db.Doacoes.AsNoTracking()
                       join c in _db.Campanhas on d.CampanhaId equals c.Id
                       join u in _db.Usuarios on d.DoadorId equals u.Id
                       select new { d, c.Titulo, u.Nome };

        if (campanhaId is not null)
            consulta = consulta.Where(x => x.d.CampanhaId == campanhaId);
        if (status is not null)
            consulta = consulta.Where(x => x.d.Status == status);

        return await consulta.OrderByDescending(x => x.d.CriadaEm).Take(limite)
            .Select(x => new DoacaoGestaoResponse(x.d.Id, x.d.CampanhaId, x.Titulo, x.d.DoadorId, x.Nome, x.d.Valor,
                x.d.Status, x.d.MotivoRejeicao, x.d.CriadaEm, x.d.ProcessadaEm))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DoadorGestaoResponse>> DoadoresAsync(CancellationToken ct = default)
    {
        // Email e Cpf são value objects com conversão: saem do banco inteiros e viram texto em memória.
        var doadores = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Perfil == Perfil.Doador)
            .Select(u => new
            {
                u.Id, u.Nome, u.Email, u.Cpf, u.CriadoEm,
                Quantidade = _db.Doacoes.Count(d => d.DoadorId == u.Id && d.Status == StatusDoacao.Processada),
                Total = _db.Doacoes.Where(d => d.DoadorId == u.Id && d.Status == StatusDoacao.Processada).Sum(d => (decimal?)d.Valor) ?? 0m,
                Ultima = _db.Doacoes.Where(d => d.DoadorId == u.Id && d.Status == StatusDoacao.Processada).Max(d => d.ProcessadaEm)
            })
            .ToListAsync(ct);

        return doadores
            .OrderByDescending(x => x.Total).ThenBy(x => x.Nome)
            .Select(x => new DoadorGestaoResponse(x.Id, x.Nome, x.Email.Value, MascararCpf(x.Cpf.Value), x.CriadoEm, x.Quantidade, x.Total, x.Ultima))
            .ToList();
    }

    // Dias do calendário de Brasília. O período atual são os últimos `dias` dias contando hoje; o anterior, os `dias` antes dele.
    // Poucos registros no MVP: busca as doações processadas dos dois períodos e agrega em memória.
    public async Task<ResumoGestaoResponse> ResumoAsync(int dias, DateTime agora, CancellationToken ct = default)
    {
        var hoje = DatasUtc.DiaEmBrasilia(agora);
        var primeiroDia = hoje.AddDays(-(dias - 1));
        var inicio = InicioDoDiaUtc(primeiroDia);
        var inicioAnterior = InicioDoDiaUtc(primeiroDia.AddDays(-dias));

        var processadas = await _db.Doacoes.AsNoTracking()
            .Where(d => d.Status == StatusDoacao.Processada && d.ProcessadaEm >= inicioAnterior && d.ProcessadaEm <= agora)
            .Select(d => new { d.Valor, d.DoadorId, ProcessadaEm = d.ProcessadaEm!.Value })
            .ToListAsync(ct);
        var pendentes = await _db.Doacoes.CountAsync(d => d.Status == StatusDoacao.Pendente, ct);

        var atual = processadas.Where(d => d.ProcessadaEm >= inicio).ToList();
        var anterior = processadas.Where(d => d.ProcessadaEm < inicio).ToList();
        var deHoje = atual.Where(d => DatasUtc.DiaEmBrasilia(d.ProcessadaEm) == hoje).ToList();

        static decimal Ticket(int n, decimal total) => n == 0 ? 0m : decimal.Round(total / n, 2);

        var porDia = Enumerable.Range(0, dias)
            .Select(i => primeiroDia.AddDays(i))
            .Select(dia =>
            {
                var doDia = atual.Where(d => DatasUtc.DiaEmBrasilia(d.ProcessadaEm) == dia).ToList();
                return new ArrecadacaoDiaResponse(dia, doDia.Sum(d => d.Valor), doDia.Count);
            })
            .ToList();

        return new ResumoGestaoResponse(
            dias,
            atual.Sum(d => d.Valor), anterior.Sum(d => d.Valor),
            atual.Count, anterior.Count,
            atual.Select(d => d.DoadorId).Distinct().Count(), anterior.Select(d => d.DoadorId).Distinct().Count(),
            Ticket(atual.Count, atual.Sum(d => d.Valor)), Ticket(anterior.Count, anterior.Sum(d => d.Valor)),
            deHoje.Sum(d => d.Valor), deHoje.Count,
            pendentes,
            porDia);
    }

    private static DateTime InicioDoDiaUtc(DateOnly diaEmBrasilia)
        => DateTime.SpecifyKind(diaEmBrasilia.ToDateTime(TimeOnly.MinValue) - DatasUtc.Brasilia, DateTimeKind.Utc);

    private IQueryable<DoacaoComTitulo> DoacoesComTitulo()
        => from d in _db.Doacoes.AsNoTracking()
           join c in _db.Campanhas on d.CampanhaId equals c.Id
           select new DoacaoComTitulo { Doacao = d, Titulo = c.Titulo };

    private sealed class DoacaoComTitulo
    {
        public Domain.Entities.Doacao Doacao { get; init; } = null!;
        public string Titulo { get; init; } = string.Empty;
    }

    // "Mariana Albuquerque" -> "Mariana A."
    public static string NomeAbreviado(string nome)
    {
        var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "Doador",
            1 => partes[0],
            _ => $"{partes[0]} {char.ToUpperInvariant(partes[^1][0])}."
        };
    }

    // "52998224725" -> "***.982.247-**"
    public static string MascararCpf(string cpf)
        => cpf.Length == 11 ? $"***.{cpf[3..6]}.{cpf[6..9]}-**" : "***";
}
