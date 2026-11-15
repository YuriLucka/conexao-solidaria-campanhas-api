using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campanhas.API.Controllers;

[ApiController]
[Route("api/doacoes")]
[Tags("Doações")]
[Authorize(Roles = "Doador")]
public class DoacoesController : ControllerBase
{
    private readonly IDoacaoService _doacoes;

    public DoacoesController(IDoacaoService doacoes) => _doacoes = doacoes;

    // O doador vem do token (claim "sub"), nunca do corpo.
    private Guid DoadorId => Guid.Parse(User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("Token sem identificação do usuário."));

    /// <summary>Doar</summary>
    /// <remarks>Só <c>Doador</c> logado. A API grava a doação como <c>Pendente</c> e publica <c>DoacaoRecebidaEvent</c> no RabbitMQ; quem soma o valor na campanha é o <c>doacoes-worker</c>. Por isso a resposta é 202. Campanha concluída, cancelada ou com prazo vencido não recebe doação.</remarks>
    /// <param name="request">Campanha e valor.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="202">Doação recebida; será processada pelo worker.</response>
    /// <response code="400">Valor inválido.</response>
    /// <response code="401">Sem token.</response>
    /// <response code="403">Token sem o perfil Doador.</response>
    /// <response code="404">Campanha não encontrada.</response>
    /// <response code="422">Campanha encerrada ou cancelada.</response>
    [HttpPost]
    [ProducesResponseType<DoacaoResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DoacaoResponse>> Doar([FromBody] CriarDoacaoRequest request, CancellationToken ct)
        => Accepted(await _doacoes.CriarAsync(DoadorId, request, ct));

    /// <summary>Minhas doações</summary>
    /// <remarks>Só <c>Doador</c> logado. Mostra o andamento de cada doação (<c>Pendente</c>, <c>Processada</c>, <c>Rejeitada</c>).</remarks>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Doações do usuário, mais recentes primeiro.</response>
    [HttpGet("minhas")]
    [ProducesResponseType<IReadOnlyList<DoacaoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DoacaoResponse>>> Minhas(CancellationToken ct)
        => Ok(await _doacoes.ListarDoDoadorAsync(DoadorId, ct));
}
