using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Campanhas.API.Controllers;

[ApiController]
[Route("api/doadores")]
[Tags("Doadores")]
public class DoadoresController : ControllerBase
{
    private readonly IDoadorService _doadores;

    public DoadoresController(IDoadorService doadores) => _doadores = doadores;

    /// <summary>Cadastrar doador</summary>
    /// <remarks>Acesso público. E-mail e CPF são únicos; o CPF é conferido pelos dígitos verificadores; a senha é guardada com BCrypt. A conta criada tem sempre o perfil <c>Doador</c>.</remarks>
    /// <param name="request">Dados do doador.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="201">Conta de doador criada.</response>
    /// <response code="400">Dados inválidos (CPF, e-mail ou senha).</response>
    /// <response code="409">E-mail ou CPF já cadastrado.</response>
    [HttpPost]
    [ProducesResponseType<DoadorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DoadorResponse>> Cadastrar([FromBody] CadastrarDoadorRequest request, CancellationToken ct)
    {
        var doador = await _doadores.CadastrarAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, doador);
    }
}
