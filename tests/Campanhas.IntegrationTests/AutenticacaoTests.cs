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
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(100));
    }

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
