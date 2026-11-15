using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Campanhas.API.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Autenticação")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Login</summary>
    /// <remarks>Devolve o JWT com o perfil (<c>GestorONG</c> ou <c>Doador</c>) na claim <c>role</c>. Use no cabeçalho <c>Authorization: Bearer {token}</c>.</remarks>
    /// <param name="request">E-mail e senha.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Login realizado.</response>
    /// <response code="401">E-mail ou senha incorretos.</response>
    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));
}
