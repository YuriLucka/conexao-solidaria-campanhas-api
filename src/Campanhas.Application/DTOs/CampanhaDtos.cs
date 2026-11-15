using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;

namespace Campanhas.Application.DTOs;

/// <summary>Dados para criar uma campanha. Toda campanha nasce com status <c>Ativa</c>.</summary>
/// <param name="Titulo">Título (até 150 caracteres).</param>
/// <param name="Descricao">Descrição (até 4000 caracteres).</param>
/// <param name="DataInicio">Início (UTC).</param>
/// <param name="DataFim">Término (UTC); não pode estar no passado.</param>
/// <param name="MetaFinanceira">Meta em reais; maior que zero.</param>
public record CriarCampanhaRequest(string Titulo, string Descricao, DateTime DataInicio, DateTime DataFim, decimal MetaFinanceira);

/// <summary>Dados para editar uma campanha, inclusive o status.</summary>
/// <param name="Titulo">Título (até 150 caracteres).</param>
/// <param name="Descricao">Descrição (até 4000 caracteres).</param>
/// <param name="DataInicio">Início (UTC).</param>
/// <param name="DataFim">Término (UTC); se mudar, ou se a campanha for reaberta, não pode estar no passado.</param>
/// <param name="MetaFinanceira">Meta em reais; maior que zero.</param>
/// <param name="Status"><c>Ativa</c>, <c>Concluida</c> ou <c>Cancelada</c>.</param>
public record AtualizarCampanhaRequest(string Titulo, string Descricao, DateTime DataInicio, DateTime DataFim, decimal MetaFinanceira, StatusCampanha Status);

/// <summary>Campanha completa (visão da gestão).</summary>
/// <param name="ValorTotalArrecadado">Soma das doações já processadas pelo worker.</param>
/// <param name="PrazoEncerrado">Verdadeiro quando a data de término já passou.</param>
public record CampanhaResponse(
    Guid Id,
    string Titulo,
    string Descricao,
    DateTime DataInicio,
    DateTime DataFim,
    decimal MetaFinanceira,
    StatusCampanha Status,
    decimal ValorTotalArrecadado,
    bool PrazoEncerrado)
{
    public static CampanhaResponse De(Campanha c, DateTime agora) => new(
        c.Id, c.Titulo, c.Descricao, c.DataInicio, c.DataFim, c.MetaFinanceira, c.Status, c.ValorArrecadado, c.PrazoEncerrado(agora));
}

/// <summary>Item do painel de transparência (só campanhas com status <c>Ativa</c>).</summary>
/// <param name="ValorTotalArrecadado">Soma das doações já processadas pelo worker.</param>
/// <param name="PrazoEncerrado">Verdadeiro quando a data de término já passou: a campanha segue no painel, mas não recebe doações.</param>
public record CampanhaAtivaResponse(
    Guid Id,
    string Titulo,
    string Descricao,
    decimal MetaFinanceira,
    decimal ValorTotalArrecadado,
    DateTime DataInicio,
    DateTime DataFim,
    bool PrazoEncerrado)
{
    public static CampanhaAtivaResponse De(Campanha c, DateTime agora) => new(
        c.Id, c.Titulo, c.Descricao, c.MetaFinanceira, c.ValorArrecadado, c.DataInicio, c.DataFim, c.PrazoEncerrado(agora));
}
