using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campanhas.API.Controllers;

[ApiController]
[Route("api/gestao")]
[Tags("Gestão")]
[Authorize(Roles = "GestorONG")]
public class GestaoController : ControllerBase
{
    private readonly IConsultas _consultas;
    private readonly TimeProvider _relogio;

    public GestaoController(IConsultas consultas, TimeProvider relogio)
    {
        _consultas = consultas;
        _relogio = relogio;
    }

    /// <summary>Visão geral</summary>
    /// <remarks>Só <c>GestorONG</c>. Arrecadação, doações confirmadas, doadores únicos e ticket médio no período, comparados ao período anterior, e a arrecadação por dia.</remarks>
    /// <param name="dias">Tamanho do período: 7, 30 ou 90 (padrão 30).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Indicadores.</response>
    [HttpGet("resumo")]
    [ProducesResponseType<ResumoGestaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResumoGestaoResponse>> Resumo([FromQuery] int dias = 30, CancellationToken ct = default)
    {
        if (dias is not (7 or 30 or 90))
            dias = 30;
        return Ok(await _consultas.ResumoAsync(dias, _relogio.GetUtcNow().UtcDateTime, ct));
    }

    /// <summary>Listar doadores</summary>
    /// <remarks>Só <c>GestorONG</c>. Com total doado (doações processadas) e CPF mascarado.</remarks>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Doadores, quem mais doou primeiro.</response>
    [HttpGet("doadores")]
    [ProducesResponseType<IReadOnlyList<DoadorGestaoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<DoadorGestaoResponse>>> Doadores(CancellationToken ct)
        => Ok(await _consultas.DoadoresAsync(ct));
}
