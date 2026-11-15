using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;

namespace Campanhas.Application.DTOs;

/// <summary>Intenção de doação. O doador vem do token.</summary>
/// <param name="IdCampanha">Campanha que recebe a doação.</param>
/// <param name="ValorDoacao">Valor em reais; maior que zero, até 2 casas decimais.</param>
public record CriarDoacaoRequest(Guid IdCampanha, decimal ValorDoacao);

/// <summary>Doação e o andamento do processamento.</summary>
/// <param name="Status"><c>Pendente</c> até o worker processar; depois <c>Processada</c> ou <c>Rejeitada</c>.</param>
public record DoacaoResponse(
    Guid Id,
    Guid IdCampanha,
    string? TituloCampanha,
    decimal ValorDoacao,
    StatusDoacao Status,
    string? MotivoRejeicao,
    DateTime CriadaEm,
    DateTime? ProcessadaEm)
{
    public static DoacaoResponse De(Doacao d, string? tituloCampanha) => new(
        d.Id, d.CampanhaId, tituloCampanha, d.Valor, d.Status, d.MotivoRejeicao, d.CriadaEm, d.ProcessadaEm);
}
