using Campanhas.Domain.Enums;

namespace Campanhas.Application.DTOs;

/// <summary>Doação recente na página pública da campanha. Só o primeiro nome e a inicial do sobrenome.</summary>
public record DoacaoRecenteResponse(string Doador, decimal Valor, DateTime ProcessadaEm);

/// <summary>Números públicos de uma campanha (só doações já processadas).</summary>
public record DoacoesDaCampanhaResponse(int QuantidadeDoacoes, int QuantidadeDoadores, IReadOnlyList<DoacaoRecenteResponse> Recentes);

/// <summary>Doação na visão da gestão.</summary>
public record DoacaoGestaoResponse(
    Guid Id,
    Guid IdCampanha,
    string TituloCampanha,
    Guid DoadorId,
    string NomeDoador,
    decimal ValorDoacao,
    StatusDoacao Status,
    string? MotivoRejeicao,
    DateTime CriadaEm,
    DateTime? ProcessadaEm);

/// <summary>Doador na visão da gestão. O CPF vem mascarado.</summary>
public record DoadorGestaoResponse(
    Guid Id,
    string NomeCompleto,
    string Email,
    string CpfMascarado,
    DateTime CriadoEm,
    int QuantidadeDoacoes,
    decimal TotalDoado,
    DateTime? UltimaDoacaoEm);

/// <summary>Arrecadação de um dia (doações processadas).</summary>
public record ArrecadacaoDiaResponse(DateOnly Dia, decimal Valor, int Doacoes);

/// <summary>Indicadores da visão geral da gestão, com comparação ao período anterior de mesmo tamanho.</summary>
public record ResumoGestaoResponse(
    int Dias,
    decimal Arrecadado,
    decimal ArrecadadoPeriodoAnterior,
    int DoacoesConfirmadas,
    int DoacoesConfirmadasPeriodoAnterior,
    int DoadoresUnicos,
    int DoadoresUnicosPeriodoAnterior,
    decimal TicketMedio,
    decimal TicketMedioPeriodoAnterior,
    decimal ArrecadadoHoje,
    int DoacoesHoje,
    int DoacoesPendentes,
    IReadOnlyList<ArrecadacaoDiaResponse> PorDia);

/// <summary>Totais das doações (todas ou de uma campanha), calculados no banco sobre o conjunto inteiro, não só a página.</summary>
public record TotaisDoacoesResponse(decimal TotalConfirmado, int Confirmadas, int Pendentes, int Rejeitadas, int DoadoresUnicos, decimal TicketMedio);

/// <summary>Uma página de doações na visão da gestão, com os totais do filtro de campanha.</summary>
/// <param name="Total">Quantidade de doações com os filtros aplicados (todas as páginas).</param>
/// <param name="Totais">Totais da campanha filtrada (ou de todas), sem o filtro de situação.</param>
public record DoacoesGestaoPaginaResponse(IReadOnlyList<DoacaoGestaoResponse> Itens, int Total, int Pagina, int Tamanho, TotaisDoacoesResponse Totais);

/// <summary>Indicadores de doadores sobre todo o cadastro.</summary>
public record IndicadoresDoadoresResponse(int Cadastrados, int DoaramUltimos30Dias, int Recorrentes, int NovosUltimos30Dias);

/// <summary>Uma página de doadores, com os indicadores do cadastro inteiro.</summary>
/// <param name="Total">Quantidade de doadores com a busca aplicada (todas as páginas).</param>
public record DoadoresGestaoPaginaResponse(IReadOnlyList<DoadorGestaoResponse> Itens, int Total, int Pagina, int Tamanho, IndicadoresDoadoresResponse Indicadores);

/// <summary>Quanto o doador já doou para uma campanha.</summary>
public record ApoioCampanhaResponse(Guid IdCampanha, string TituloCampanha, string? Capa, string? Categoria, decimal Valor);

/// <summary>Resumo das doações do doador logado (sobre todas as doações dele).</summary>
/// <param name="PorCampanha">Campanhas que mais receberam do doador (até 6).</param>
public record ResumoDoadorResponse(decimal TotalDoado, int Confirmadas, int CampanhasApoiadas, int Pendentes, IReadOnlyList<ApoioCampanhaResponse> PorCampanha);

/// <summary>Uma página das doações do doador logado, com o resumo de todas elas.</summary>
public record MinhasDoacoesPaginaResponse(IReadOnlyList<DoacaoResponse> Itens, int Total, int Pagina, int Tamanho, ResumoDoadorResponse Resumo);
