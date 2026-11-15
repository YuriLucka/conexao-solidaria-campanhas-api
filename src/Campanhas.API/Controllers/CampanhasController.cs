using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campanhas.API.Controllers;

[ApiController]
[Route("api/campanhas")]
[Tags("Campanhas")]
public class CampanhasController : ControllerBase
{
    private readonly ICampanhaService _campanhas;

    public CampanhasController(ICampanhaService campanhas) => _campanhas = campanhas;

    /// <summary>Painel de transparência</summary>
    /// <remarks>Acesso público. Só campanhas com status <c>Ativa</c>, com título, meta e valor total arrecadado (soma das doações já processadas pelo worker). Campanha com prazo vencido continua na lista com <c>prazoEncerrado = true</c> e não recebe doações.</remarks>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Campanhas ativas.</response>
    [HttpGet("ativas")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<CampanhaAtivaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CampanhaAtivaResponse>>> ListarAtivas(CancellationToken ct)
        => Ok(await _campanhas.ListarAtivasAsync(ct));

    /// <summary>Detalhe da campanha</summary>
    /// <remarks>Acesso público (transparência).</remarks>
    /// <param name="id">Id da campanha.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Campanha encontrada.</response>
    /// <response code="404">Campanha não encontrada.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<CampanhaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampanhaResponse>> Obter(Guid id, CancellationToken ct)
        => Ok(await _campanhas.ObterAsync(id, ct));

    /// <summary>Doações recentes da campanha</summary>
    /// <remarks>Acesso público (transparência). Só doações já processadas, com o primeiro nome e a inicial do sobrenome de quem doou, e os totais de doações e doadores.</remarks>
    /// <param name="id">Id da campanha.</param>
    /// <param name="quantidade">Quantas doações recentes trazer (1 a 50, padrão 10).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Totais e doações recentes.</response>
    /// <response code="404">Campanha não encontrada.</response>
    [HttpGet("{id:guid}/doacoes-recentes")]
    [AllowAnonymous]
    [ProducesResponseType<DoacoesDaCampanhaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoacoesDaCampanhaResponse>> DoacoesRecentes(Guid id, [FromServices] IConsultas consultas,
        [FromQuery] int quantidade = 10, CancellationToken ct = default)
    {
        await _campanhas.ObterAsync(id, ct); // 404 se não existe
        return Ok(await consultas.DoacoesDaCampanhaAsync(id, Math.Clamp(quantidade, 1, 50), ct));
    }

    /// <summary>Listar todas as campanhas</summary>
    /// <remarks>Só <c>GestorONG</c>. Inclui concluídas e canceladas.</remarks>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Campanhas.</response>
    /// <response code="401">Sem token.</response>
    /// <response code="403">Token sem o perfil GestorONG.</response>
    [HttpGet]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType<IReadOnlyList<CampanhaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<CampanhaResponse>>> Listar(CancellationToken ct)
        => Ok(await _campanhas.ListarAsync(ct));

    /// <summary>Criar campanha</summary>
    /// <remarks>Só <c>GestorONG</c>. A campanha nasce com status <c>Ativa</c>. A data de término não pode estar no passado e a meta deve ser maior que zero.</remarks>
    /// <param name="request">Dados da campanha.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="201">Campanha criada.</response>
    /// <response code="400">Dados inválidos (término no passado, meta menor ou igual a zero, campos vazios).</response>
    /// <response code="401">Sem token.</response>
    /// <response code="403">Token sem o perfil GestorONG.</response>
    [HttpPost]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType<CampanhaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CampanhaResponse>> Criar([FromBody] CriarCampanhaRequest request, CancellationToken ct)
    {
        var campanha = await _campanhas.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = campanha.Id }, campanha);
    }

    /// <summary>Editar campanha</summary>
    /// <remarks>Só <c>GestorONG</c>. Altera todos os campos, inclusive o status (<c>Ativa</c>, <c>Concluida</c>, <c>Cancelada</c>). Ao mudar o término ou reabrir a campanha, o término não pode estar no passado. O valor arrecadado não é editável.</remarks>
    /// <param name="id">Id da campanha.</param>
    /// <param name="request">Novos dados.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Campanha atualizada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Sem token.</response>
    /// <response code="403">Token sem o perfil GestorONG.</response>
    /// <response code="404">Campanha não encontrada.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType<CampanhaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampanhaResponse>> Atualizar(Guid id, [FromBody] AtualizarCampanhaRequest request, CancellationToken ct)
        => Ok(await _campanhas.AtualizarAsync(id, request, ct));
}
