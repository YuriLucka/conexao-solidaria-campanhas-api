using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Campanhas.IntegrationTests;

// RF3: cadastro público de doador.
[Collection(ApiCollection.Nome)]
public class DoadoresTests(ApiFixture api)
{
    private static string EmailUnico() => $"doador-{Guid.NewGuid():N}@email.demo";

    // CPFs válidos (dígitos verificadores corretos), um por teste para não colidir.
    private static readonly string[] Cpfs = ["092.275.377-67", "566.823.760-57", "649.815.966-93", "123.456.789-09", "987.654.321-00", "714.602.380-01"];

    [Fact]
    public async Task Cadastro_cria_doador_e_permite_login_como_Doador()
    {
        using var client = api.Cliente();
        var email = EmailUnico();

        var resposta = await client.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Ana Teste", email, cpf = Cpfs[0], senha = "Doar@2026" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, corpo.GetProperty("email").GetString());
        Assert.False(corpo.TryGetProperty("senha", out _));
        Assert.False(corpo.TryGetProperty("senhaHash", out _));

        var token = await api.TokenAsync(email.ToUpperInvariant(), "Doar@2026");
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task Email_repetido_devolve_409_mesmo_com_maiusculas()
    {
        using var client = api.Cliente();
        var email = EmailUnico();
        await client.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Bia", email, cpf = Cpfs[1], senha = "Doar@2026" });

        var resposta = await client.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Bia 2", email = email.ToUpperInvariant(), cpf = Cpfs[2], senha = "Doar@2026" });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("Já existe uma conta com este e-mail. Entre com ela ou use outro e-mail.", (await Problema.LerAsync(resposta)).Detail);
    }

    [Fact]
    public async Task Cpf_repetido_com_ou_sem_mascara_devolve_409()
    {
        using var client = api.Cliente();
        await client.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Caio", email = EmailUnico(), cpf = Cpfs[3], senha = "Doar@2026" });

        var resposta = await client.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Caio 2", email = EmailUnico(), cpf = "12345678909", senha = "Doar@2026" });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("Já existe uma conta com este CPF. Entre com ela ou fale com a ONG.", (await Problema.LerAsync(resposta)).Detail);
    }

    [Theory]
    [InlineData("123.456.789-00", "CPF inválido.")]
    [InlineData("111.111.111-11", "CPF inválido.")]
    [InlineData("abc", "CPF inválido.")]
    public async Task Cpf_invalido_devolve_400(string cpf, string mensagem)
    {
        using var client = api.Cliente();
        var resposta = await client.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Davi", email = EmailUnico(), cpf, senha = "Doar@2026" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(mensagem, (await Problema.LerAsync(resposta)).Detail);
    }

    [Fact]
    public async Task Senha_fraca_devolve_400()
    {
        using var client = api.Cliente();
        var resposta = await client.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Eva", email = EmailUnico(), cpf = Cpfs[4], senha = "fraca" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("senha", (await Problema.LerAsync(resposta)).Detail);
    }

    [Fact]
    public async Task Campos_ausentes_devolvem_400_em_portugues_com_erros_por_campo()
    {
        using var client = api.Cliente();
        var resposta = await client.PostAsJsonAsync("/api/doadores", new { email = EmailUnico() });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await Problema.LerAsync(resposta);
        Assert.Equal("Dados inválidos", problema.Title);
        Assert.Equal(["O campo cpf é obrigatório."], problema.Errors["cpf"]);
        Assert.Equal(["O campo senha é obrigatório."], problema.Errors["senha"]);
        Assert.Equal(["O campo nomeCompleto é obrigatório."], problema.Errors["nomeCompleto"]);
    }

    [Fact]
    public async Task Json_invalido_devolve_400_em_portugues()
    {
        using var client = api.Cliente();
        var resposta = await client.PostAsync("/api/doadores", new StringContent("{ isto não é json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await Problema.LerAsync(resposta);
        Assert.Contains(problema.Errors.Values.SelectMany(v => v), m => m.Contains("JSON inválido") || m.StartsWith("Valor inválido"));
    }

    [Fact]
    public async Task Cadastro_publico_nunca_cria_GestorONG()
    {
        using var client = api.Cliente();
        var email = EmailUnico();
        await client.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Fábio", email, cpf = Cpfs[5], senha = "Doar@2026", perfil = "GestorONG" });

        var token = await api.TokenAsync(email, "Doar@2026");
        using var comToken = api.Cliente(token);
        Assert.Equal(HttpStatusCode.Forbidden, (await comToken.GetAsync("/api/campanhas")).StatusCode);
    }
}
