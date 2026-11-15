using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Campanhas.API.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Autenticação")]
public class AuthController : ControllerBase
{
    // HttpOnly: o JavaScript do site não lê o refresh token. Path restrito: o navegador só o envia para /api/auth.
    public const string CookieRefresh = "cs_refresh";
    private const string CaminhoCookie = "/api/auth";

    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Login</summary>
    /// <remarks>Devolve o JWT (15 minutos) com o perfil (<c>GestorONG</c> ou <c>Doador</c>) na claim <c>role</c>. Use no cabeçalho <c>Authorization: Bearer {token}</c>. O refresh token vai no cookie HttpOnly <c>cs_refresh</c>, usado por <c>POST /api/auth/refresh</c>.</remarks>
    /// <param name="request">E-mail e senha.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Login realizado.</response>
    /// <response code="401">E-mail ou senha incorretos.</response>
    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var sessao = await _auth.LoginAsync(request, ct);
        GravarCookie(sessao);
        return Ok(sessao.Token);
    }

    /// <summary>Renovar a sessão</summary>
    /// <remarks>Lê o refresh token do cookie <c>cs_refresh</c> e devolve um novo JWT. O refresh token é trocado a cada uso; um token já trocado que volte a ser usado encerra todas as sessões abertas a partir daquele login.</remarks>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="200">Sessão renovada.</response>
    /// <response code="401">Sem cookie, ou sessão expirada ou encerrada.</response>
    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Renovar(CancellationToken ct)
    {
        try
        {
            var sessao = await _auth.RenovarAsync(Request.Cookies[CookieRefresh], ct);
            GravarCookie(sessao);
            return Ok(sessao.Token);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Respondido aqui, e não pelo ErrorHandler, para o cookie inválido ser apagado na mesma resposta.
            ApagarCookie();
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Não autenticado", detail: ex.Message);
        }
    }

    /// <summary>Sair</summary>
    /// <remarks>Encerra a sessão no servidor (revoga o refresh token do cookie) e apaga o cookie. O JWT já emitido vale até expirar (no máximo 15 minutos).</remarks>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="204">Sessão encerrada.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Sair(CancellationToken ct)
    {
        await _auth.SairAsync(Request.Cookies[CookieRefresh], ct);
        ApagarCookie();
        return NoContent();
    }

    private void GravarCookie(SessaoAutenticada sessao)
        => Response.Cookies.Append(CookieRefresh, sessao.RefreshToken, OpcoesCookie(sessao.RefreshExpiraEm));

    private void ApagarCookie() => Response.Cookies.Delete(CookieRefresh, OpcoesCookie(expiraEm: null));

    private CookieOptions OpcoesCookie(DateTime? expiraEm) => new()
    {
        HttpOnly = true,
        Secure = !HostLocal(),
        SameSite = SameSiteMode.Strict,
        Path = CaminhoCookie,
        Expires = expiraEm,
        IsEssential = true
    };

    // Atrás do Kong o Host é o do container; o endereço que o navegador usou vem em X-Forwarded-Host.
    // Em http://localhost (Compose, testes) o cookie sai sem Secure: o Safari recusaria um cookie Secure em http.
    private bool HostLocal()
    {
        var host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Host;
        host = host.Split(':')[0].Trim('[', ']');
        return host is "localhost" or "127.0.0.1" or "::1" || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }
}
