using Campanhas.Application.Common;
using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Enums;
using Campanhas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Consultas;

public class ConsultasPostgres : IConsultas
{
    private readonly AppDbContext _db;

    public ConsultasPostgres(AppDbContext db) => _db = db;

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

    public async Task<MinhasDoacoesPaginaResponse> DoacoesDoDoadorAsync(Guid doadorId, int pagina, int tamanho, CancellationToken ct = default)
    {
        var dele = DoacoesComTitulo().Where(d => d.Doacao.DoadorId == doadorId);
        var total = await dele.CountAsync(ct);
        var itens = await dele.OrderByDescending(d => d.Doacao.CriadaEm)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(d => new DoacaoResponse(d.Doacao.Id, d.Doacao.CampanhaId, d.Titulo, d.Doacao.Valor, d.Doacao.Status,
                d.Doacao.MotivoRejeicao, d.Doacao.CriadaEm, d.Doacao.ProcessadaEm))
            .ToListAsync(ct);

        var confirmadas = _db.Doacoes.AsNoTracking().Where(d => d.DoadorId == doadorId && d.Status == StatusDoacao.Processada);
        var somas = await confirmadas.GroupBy(d => d.CampanhaId)
            .Select(g => new { IdCampanha = g.Key, Valor = g.Sum(x => x.Valor), Quantidade = g.Count() })
            .ToListAsync(ct);
        var destaque = somas.OrderByDescending(x => x.Valor).Take(6).ToList();
        var ids = destaque.Select(x => x.IdCampanha).ToList();
        var campanhas = await _db.Campanhas.AsNoTracking().Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Titulo, c.Capa, c.Categoria })
            .ToDictionaryAsync(c => c.Id, ct);
        var porCampanha = destaque
            .Where(x => campanhas.ContainsKey(x.IdCampanha))
            .Select(x => new ApoioCampanhaResponse(x.IdCampanha, campanhas[x.IdCampanha].Titulo, campanhas[x.IdCampanha].Capa,
                campanhas[x.IdCampanha].Categoria, x.Valor))
            .ToList();

        var resumo = new ResumoDoadorResponse(
            somas.Sum(x => x.Valor),
            somas.Sum(x => x.Quantidade),
            somas.Count,
            await _db.Doacoes.CountAsync(d => d.DoadorId == doadorId && d.Status == StatusDoacao.Pendente, ct),
            porCampanha);

        return new MinhasDoacoesPaginaResponse(itens, total, pagina, tamanho, resumo);
    }

    public async Task<DoacaoResponse?> DoacaoDoDoadorAsync(Guid doadorId, Guid doacaoId, CancellationToken ct = default)
        => await DoacoesComTitulo().Where(d => d.Doacao.DoadorId == doadorId && d.Doacao.Id == doacaoId)
            .Select(d => new DoacaoResponse(d.Doacao.Id, d.Doacao.CampanhaId, d.Titulo, d.Doacao.Valor, d.Doacao.Status,
                d.Doacao.MotivoRejeicao, d.Doacao.CriadaEm, d.Doacao.ProcessadaEm))
            .FirstOrDefaultAsync(ct);

    public async Task<DoacoesGestaoPaginaResponse> DoacoesGestaoAsync(Guid? campanhaId, StatusDoacao? status, string? busca, int pagina, int tamanho,
        CancellationToken ct = default)
    {
        var daCampanha = _db.Doacoes.AsNoTracking();
        if (campanhaId is not null)
            daCampanha = daCampanha.Where(d => d.CampanhaId == campanhaId);
        var filtradas = status is null ? daCampanha : daCampanha.Where(d => d.Status == status);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var padrao = Padrao(busca);
            filtradas = filtradas.Where(d => _db.Usuarios.Any(u => u.Id == d.DoadorId && EF.Functions.ILike(u.Nome, padrao)));
        }

        var total = await filtradas.CountAsync(ct);
        var itens = await (from d in filtradas
                           join c in _db.Campanhas on d.CampanhaId equals c.Id
                           join u in _db.Usuarios on d.DoadorId equals u.Id
                           orderby d.CriadaEm descending
                           select new DoacaoGestaoResponse(d.Id, d.CampanhaId, c.Titulo, d.DoadorId, u.Nome, d.Valor,
                               d.Status, d.MotivoRejeicao, d.CriadaEm, d.ProcessadaEm))
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .ToListAsync(ct);

        // Totais sobre o conjunto inteiro da campanha (ou de todas), calculados no banco e sem o filtro de situação.
        var porStatus = await daCampanha.GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count(), Soma = g.Sum(d => d.Valor) })
            .ToListAsync(ct);
        var doadores = await daCampanha.Where(d => d.Status == StatusDoacao.Processada).Select(d => d.DoadorId).Distinct().CountAsync(ct);
        var processadas = porStatus.FirstOrDefault(x => x.Status == StatusDoacao.Processada);
        var confirmadas = processadas?.Quantidade ?? 0;
        var totalConfirmado = processadas?.Soma ?? 0m;
        var totais = new TotaisDoacoesResponse(
            totalConfirmado, confirmadas,
            porStatus.FirstOrDefault(x => x.Status == StatusDoacao.Pendente)?.Quantidade ?? 0,
            porStatus.FirstOrDefault(x => x.Status == StatusDoacao.Rejeitada)?.Quantidade ?? 0,
            doadores,
            confirmadas == 0 ? 0m : decimal.Round(totalConfirmado / confirmadas, 2));

        return new DoacoesGestaoPaginaResponse(itens, total, pagina, tamanho, totais);
    }

    public async Task<DoadoresGestaoPaginaResponse> DoadoresAsync(string? busca, int pagina, int tamanho, DateTime agora, CancellationToken ct = default)
    {
        var doadores = _db.Usuarios.AsNoTracking().Where(u => u.Perfil == Perfil.Doador);
        var confirmadas = _db.Doacoes.AsNoTracking().Where(d => d.Status == StatusDoacao.Processada);
        var limite = agora.AddDays(-30);
        var indicadores = new IndicadoresDoadoresResponse(
            await doadores.CountAsync(ct),
            await confirmadas.Where(d => d.ProcessadaEm >= limite).Select(d => d.DoadorId).Distinct().CountAsync(ct),
            await confirmadas.GroupBy(d => d.DoadorId).Where(g => g.Count() >= 2).CountAsync(ct),
            await doadores.CountAsync(u => u.CriadoEm >= limite, ct));

        // Email é value object com conversão: a busca por nome ou e-mail vai em SQL e compõe com o restante da consulta.
        var termo = busca?.Trim();
        var filtrados = doadores;
        if (!string.IsNullOrEmpty(termo))
        {
            var padrao = Padrao(termo);
            filtrados = _db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE nome ILIKE {padrao} OR email ILIKE {padrao}")
                .AsNoTracking().Where(u => u.Perfil == Perfil.Doador);
        }

        var total = await filtrados.CountAsync(ct);
        // Email e Cpf saem do banco inteiros e viram texto em memória.
        var linhas = await filtrados
            .Select(u => new
            {
                u.Id, u.Nome, u.Email, u.Cpf, u.CriadoEm,
                Quantidade = confirmadas.Count(d => d.DoadorId == u.Id),
                Total = confirmadas.Where(d => d.DoadorId == u.Id).Sum(d => (decimal?)d.Valor) ?? 0m,
                Ultima = confirmadas.Where(d => d.DoadorId == u.Id).Max(d => d.ProcessadaEm)
            })
            .OrderByDescending(x => x.Total).ThenBy(x => x.Nome).ThenBy(x => x.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .ToListAsync(ct);

        var itens = linhas
            .Select(x => new DoadorGestaoResponse(x.Id, x.Nome, x.Email.Value, MascararCpf(x.Cpf.Value), x.CriadoEm, x.Quantidade, x.Total, x.Ultima))
            .ToList();
        return new DoadoresGestaoPaginaResponse(itens, total, pagina, tamanho, indicadores);
    }

    // Dias do calendário de Brasília. O período atual são os últimos `dias` dias contando hoje; o anterior, os `dias` antes dele.
    // Busca as doações processadas dos dois períodos e agrega em memória, numa passada só para o gráfico por dia.
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

        var porDiaAgrupado = atual.GroupBy(d => DatasUtc.DiaEmBrasilia(d.ProcessadaEm))
            .ToDictionary(g => g.Key, g => (Valor: g.Sum(d => d.Valor), Quantidade: g.Count()));
        var porDia = Enumerable.Range(0, dias)
            .Select(i => primeiroDia.AddDays(i))
            .Select(dia => porDiaAgrupado.TryGetValue(dia, out var x)
                ? new ArrecadacaoDiaResponse(dia, x.Valor, x.Quantidade)
                : new ArrecadacaoDiaResponse(dia, 0m, 0))
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

    // Trecho para ILIKE, com os curingas digitados pelo usuário escapados.
    private static string Padrao(string termo)
        => "%" + termo.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

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
    private static string NomeAbreviado(string nome)
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
    private static string MascararCpf(string cpf)
        => cpf.Length == 11 ? $"***.{cpf[3..6]}.{cpf[6..9]}-**" : "***";
}
