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
