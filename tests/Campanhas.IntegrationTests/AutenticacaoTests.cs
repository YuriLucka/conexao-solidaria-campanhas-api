using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Campanhas.IntegrationTests;

// RF1: JWT, perfis GestorONG e Doador, gestão só para GestorONG.
[Collection(ApiCollection.Nome)]
public class AutenticacaoTests(ApiFixture api)
{
    [Fact]
    public async Task Login_da_gestora_devolve_JWT_com_perfil_GestorONG()
    {
        using var client = api.Cliente();
        var resposta = await client.PostAsJsonAsync("/api/auth/login", new { email = "helena@esperancasolidaria.demo", senha = "Gestao@2026" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bearer", corpo.GetProperty("tokenType").GetString());
        Assert.Equal("GestorONG", corpo.GetProperty("perfil").GetString());

        var jwt = new JsonWebToken(corpo.GetProperty("accessToken").GetString());
        Assert.Equal("GestorONG", jwt.GetClaim("role").Value);
        Assert.Equal("conexao-solidaria", jwt.Issuer);
        // JWT curto: a sessão continua pelo refresh token.
        Assert.InRange(jwt.ValidTo, DateTime.UtcNow.AddMinutes(13), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task Login_grava_o_refresh_token_em_cookie_HttpOnly_restrito_a_api_auth()
    {
        using var client = api.Cliente();
        var resposta = await client.PostAsJsonAsync("/api/auth/login", new { email = "mariana@email.demo", senha = "Doar@2026" });

        var cookie = Assert.Single(resposta.Headers.GetValues("Set-Cookie"), c => c.StartsWith("cs_refresh=")).ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("path=/api/auth", cookie);
    }

    [Fact]
    public async Task Refresh_com_o_cookie_devolve_novo_JWT_e_troca_o_cookie()
    {
        using var client = api.Cliente();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "mariana@email.demo", senha = "Doar@2026" });
        var cookieLogin = CookieRefresh(login);

        var renovada = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, renovada.StatusCode);
        var corpo = await renovada.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Doador", corpo.GetProperty("perfil").GetString());
        Assert.NotEqual(cookieLogin, CookieRefresh(renovada));

        // O JWT renovado funciona nas rotas protegidas.
        using var autenticado = api.Cliente(corpo.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await autenticado.GetAsync("/api/doacoes/minhas")).StatusCode);
    }

    [Fact]
    public async Task Refresh_sem_cookie_devolve_401()
    {
        using var client = api.Cliente();
        var resposta = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("Sua sessão expirou. Entre de novo para continuar.", (await Problema.LerAsync(resposta)).Detail);
    }

    [Fact]
    public async Task Logout_encerra_a_sessao_no_servidor()
    {
        using var client = api.Cliente();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "mariana@email.demo", senha = "Doar@2026" });
        var cookieLogin = CookieRefresh(login);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);

        // Mesmo reapresentando o cookie antigo, a sessão não volta.
        using var outro = api.Cliente();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        req.Headers.Add("Cookie", cookieLogin);
        Assert.Equal(HttpStatusCode.Unauthorized, (await outro.SendAsync(req)).StatusCode);
    }

    private static string CookieRefresh(HttpResponseMessage resposta)
        => resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("cs_refresh=")).Split(';')[0];

    [Fact]
    public async Task Login_da_doadora_devolve_perfil_Doador()
    {
        var token = await api.TokenDoadoraAsync();
        Assert.Equal("Doador", new JsonWebToken(token).GetClaim("role").Value);
    }

    [Theory]
    [InlineData("mariana@email.demo", "senha-errada")]
    [InlineData("ninguem@email.demo", "Doar@2026")]
    [InlineData("email-sem-arroba", "Doar@2026")]
    public async Task Credenciais_invalidas_devolvem_401_sem_dizer_qual_campo_errou(string email, string senha)
    {
        using var client = api.Cliente();
        var resposta = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var problema = await Problema.LerAsync(resposta);
        Assert.Equal("E-mail ou senha incorretos.", problema.Detail);
    }

    [Theory]
    [InlineData("GET", "/api/campanhas")]
    [InlineData("POST", "/api/campanhas")]
    [InlineData("PUT", "/api/campanhas/00000000-0000-0000-0000-000000000001")]
    public async Task Gestao_sem_token_devolve_401(string metodo, string rota)
    {
        using var client = api.Cliente();
        var resposta = await client.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), rota) { Content = JsonContent.Create(new { }) });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var problema = await Problema.LerAsync(resposta);
        Assert.StartsWith("Não autenticado", problema.Title);
    }

    [Theory]
    [InlineData("GET", "/api/campanhas")]
    [InlineData("POST", "/api/campanhas")]
    [InlineData("PUT", "/api/campanhas/00000000-0000-0000-0000-000000000001")]
    public async Task Gestao_com_token_de_doador_devolve_403(string metodo, string rota)
    {
        using var client = api.Cliente(await api.TokenDoadoraAsync());
        var resposta = await client.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), rota) { Content = JsonContent.Create(new { }) });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        var problema = await Problema.LerAsync(resposta);
        Assert.StartsWith("Acesso negado", problema.Title);
    }

    [Fact]
    public async Task Doacao_com_token_da_gestora_devolve_403()
    {
        using var client = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await client.PostAsJsonAsync("/api/doacoes", new { idCampanha = Guid.NewGuid(), valorDoacao = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_adulterado_devolve_401()
    {
        var token = await api.TokenGestoraAsync();
        using var client = api.Cliente(token[..^4] + "AAAA");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/campanhas")).StatusCode);
    }
}
