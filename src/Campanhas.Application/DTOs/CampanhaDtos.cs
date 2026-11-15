using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;

namespace Campanhas.Application.DTOs;

/// <summary>Dados para criar uma campanha. Toda campanha nasce com status <c>Ativa</c>.</summary>
/// <param name="Titulo">Título (até 150 caracteres).</param>
/// <param name="Descricao">Descrição (até 4000 caracteres).</param>
/// <param name="DataInicio">Início (UTC).</param>
/// <param name="DataFim">Término (UTC); não pode estar no passado.</param>
/// <param name="MetaFinanceira">Meta em reais; maior que zero.</param>
/// <param name="Categoria">Opcional: Datas especiais, Educação, Infraestrutura, Saúde, Alimentação, Esporte e lazer ou Inverno.</param>
/// <param name="Capa">Opcional: ilustração do card (sol-colinas, amanhecer, livros, presentes, casa, saude, horta, bola, inverno, estrelas, abraco, festa).</param>
/// <param name="CriancasAtendidas">Opcional: quantas crianças a campanha atende.</param>
public record CriarCampanhaRequest(string Titulo, string Descricao, DateTime DataInicio, DateTime DataFim, decimal MetaFinanceira,
    string? Categoria = null, string? Capa = null, int? CriancasAtendidas = null);

/// <summary>Dados para editar uma campanha, inclusive o status.</summary>
/// <param name="Titulo">Título (até 150 caracteres).</param>
/// <param name="Descricao">Descrição (até 4000 caracteres).</param>
/// <param name="DataInicio">Início (UTC).</param>
/// <param name="DataFim">Término (UTC); se mudar, ou se a campanha for reaberta, não pode estar no passado.</param>
/// <param name="MetaFinanceira">Meta em reais; maior que zero.</param>
/// <param name="Status"><c>Ativa</c>, <c>Concluida</c> ou <c>Cancelada</c>.</param>
/// <param name="Categoria">Opcional (mesmos valores da criação).</param>
/// <param name="Capa">Opcional (mesmos valores da criação).</param>
/// <param name="CriancasAtendidas">Opcional.</param>
public record AtualizarCampanhaRequest(string Titulo, string Descricao, DateTime DataInicio, DateTime DataFim, decimal MetaFinanceira, StatusCampanha Status,
    string? Categoria = null, string? Capa = null, int? CriancasAtendidas = null);

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
    bool PrazoEncerrado,
    string? Categoria,
    string? Capa,
    int? CriancasAtendidas)
{
    public static CampanhaResponse De(Campanha c, DateTime agora) => new(
        c.Id, c.Titulo, c.Descricao, c.DataInicio, c.DataFim, c.MetaFinanceira, c.Status, c.ValorArrecadado, c.PrazoEncerrado(agora),
        c.Categoria, c.Capa, c.CriancasAtendidas);
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
    bool PrazoEncerrado,
    string? Categoria,
    string? Capa,
    int? CriancasAtendidas)
{
    public static CampanhaAtivaResponse De(Campanha c, DateTime agora) => new(
        c.Id, c.Titulo, c.Descricao, c.MetaFinanceira, c.ValorArrecadado, c.DataInicio, c.DataFim, c.PrazoEncerrado(agora),
        c.Categoria, c.Capa, c.CriancasAtendidas);
}
