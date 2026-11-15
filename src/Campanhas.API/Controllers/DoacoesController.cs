using Campanhas.API.Seguranca;
using Campanhas.Application.DTOs;
using Campanhas.Application.Exceptions;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Campanhas.API.Controllers;

[ApiController]
[Route("api/doacoes")]
[Tags("Doações")]
[Authorize]
public class DoacoesController : ControllerBase
{
    private readonly IDoacaoService _doacoes;
    private readonly IConsultas _consultas;

    public DoacoesController(IDoacaoService doacoes, IConsultas consultas)
    {
        _doacoes = doacoes;
        _consultas = consultas;
    }

    // O doador vem do token (claim "sub"), nunca do corpo.
    private Guid DoadorId => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new UnauthorizedAccessException("Token sem identificação do usuário."));

    /// <summary>Doar</summary>
    /// <remarks>Só <c>Doador</c> logado. A API grava a doação como <c>Pendente</c> e publica <c>DoacaoRecebidaEvent</c> no RabbitMQ; quem soma o valor ao total arrecadado da campanha é o <c>doacoes-worker</c>. Por isso a resposta é 202. Campanha concluída, cancelada ou com prazo encerrado não recebe doação.</remarks>
    /// <param name="request">Campanha e valor.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="202">Doação recebida; será processada pelo worker.</response>
    /// <response code="400">Valor inválido.</response>
    /// <response code="401">Sem token.</response>
    /// <response code="403">Token sem o perfil Doador.</response>
    /// <response code="404">Campanha não encontrada.</response>
    /// <response code="422">Campanha concluída, cancelada ou com prazo encerrado.</response>
    [HttpPost]
    [Authorize(Roles = Perfis.Doador)]
    [ProducesResponseType<DoacaoResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DoacaoResponse>> Doar([FromBody] CriarDoacaoRequest request, CancellationToken ct)
    {
        var doacao = await _doacoes.CriarAsync(DoadorId, request, ct);
        return AcceptedAtAction(nameof(Obter), new { id = doacao.Id }, doacao);
    }

    /// <summary>Minhas doações</summary>
    /// <remarks>Só <c>Doador</c> logado. Mostra o andamento de cada doação (<c>Pendente</c>, <c>Processada</c>, <c>Rejeitada</c>).</remarks>
    /// <param name="pagina">Página, a partir de 1.</param>
    /// <param name="tamanho">Itens por página (1 a 100, padrão 20).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Página das doações do usuário (mais recentes primeiro) e o resumo de todas elas.</response>
    [HttpGet("minhas")]
    [Authorize(Roles = Perfis.Doador)]
    [ProducesResponseType<MinhasDoacoesPaginaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MinhasDoacoesPaginaResponse>> Minhas(
        [FromQuery] int pagina = 1, [FromQuery] int tamanho = 20, CancellationToken ct = default)
        => Ok(await _consultas.DoacoesDoDoadorAsync(DoadorId, Math.Max(1, pagina), Math.Clamp(tamanho, 1, 100), ct));

    /// <summary>Acompanhar uma doação</summary>
    /// <remarks>Só o <c>Doador</c> dono da doação. Usado pelo site para mostrar a doação passando de <c>Pendente</c> para <c>Processada</c>.</remarks>
    /// <param name="id">Id da doação.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Doação encontrada.</response>
    /// <response code="404">Doação inexistente ou de outro doador.</response>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Perfis.Doador)]
    [ProducesResponseType<DoacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoacaoResponse>> Obter(Guid id, CancellationToken ct)
        => Ok(await _consultas.DoacaoDoDoadorAsync(DoadorId, id, ct) ?? throw new NotFoundException("Doação não encontrada."));

    /// <summary>Listar doações (gestão)</summary>
    /// <remarks>Só <c>GestorONG</c>. Paginado, mais recentes primeiro, com filtros opcionais. Os totais (valor confirmado, quantidades por situação, doadores únicos e ticket médio) consideram todas as doações da campanha filtrada, não só a página.</remarks>
    /// <param name="idCampanha">Filtra por campanha.</param>
    /// <param name="status"><c>Pendente</c>, <c>Processada</c> ou <c>Rejeitada</c>.</param>
    /// <param name="busca">Trecho do nome do doador (opcional).</param>
    /// <param name="pagina">Página, a partir de 1.</param>
    /// <param name="tamanho">Itens por página (1 a 100, padrão 20).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Página de doações e os totais.</response>
    [HttpGet]
    [Authorize(Roles = Perfis.GestorONG)]
    [ProducesResponseType<DoacoesGestaoPaginaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DoacoesGestaoPaginaResponse>> Listar(
        [FromQuery] Guid? idCampanha, [FromQuery] StatusDoacao? status, [FromQuery] string? busca, [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20, CancellationToken ct = default)
        => Ok(await _consultas.DoacoesGestaoAsync(idCampanha, status, busca, Math.Max(1, pagina), Math.Clamp(tamanho, 1, 100), ct));
}
