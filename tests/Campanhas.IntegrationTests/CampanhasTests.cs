using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Campanhas.IntegrationTests;

// RF2 (criar/editar, só GestorONG) e RF4 (painel público só com Ativa).
[Collection(ApiCollection.Nome)]
public class CampanhasTests(ApiFixture api)
{
    private static object NovaCampanha(string titulo = "Campanha de teste", int fimEmDias = 30, decimal meta = 1000m) => new
    {
        titulo,
        descricao = "Descrição da campanha de teste",
        dataInicio = DateTime.UtcNow.AddDays(-1),
        dataFim = DateTime.UtcNow.AddDays(fimEmDias),
        metaFinanceira = meta
    };

    private async Task<JsonElement> CriarAsync(HttpClient gestora, object campanha)
    {
        var resposta = await gestora.PostAsJsonAsync("/api/campanhas", campanha);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Criar_campanha_valida_devolve_201_Ativa_com_Location()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await gestora.PostAsJsonAsync("/api/campanhas", NovaCampanha("Natal 2026", meta: 1500.50m));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ativa", corpo.GetProperty("status").GetString());
        Assert.Equal(1500.50m, corpo.GetProperty("metaFinanceira").GetDecimal());
        Assert.Equal(0m, corpo.GetProperty("valorTotalArrecadado").GetDecimal());
        Assert.EndsWith($"/api/campanhas/{corpo.GetProperty("id").GetGuid()}", resposta.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Termino_no_passado_devolve_400()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await gestora.PostAsJsonAsync("/api/campanhas", NovaCampanha(fimEmDias: -1));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("A data de término não pode estar no passado.", (await Problema.LerAsync(resposta)).Detail);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Meta_menor_ou_igual_a_zero_devolve_400(decimal meta)
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await gestora.PostAsJsonAsync("/api/campanhas", NovaCampanha(meta: meta));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("A meta financeira deve ser maior que zero.", (await Problema.LerAsync(resposta)).Detail);
    }

    [Fact]
    public async Task Campos_obrigatorios_ausentes_devolvem_400_por_campo()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await gestora.PostAsJsonAsync("/api/campanhas", new { metaFinanceira = 10 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await Problema.LerAsync(resposta);
        Assert.Contains("titulo", problema.Errors.Keys);
        Assert.Contains("descricao", problema.Errors.Keys);
    }

    [Fact]
    public async Task Editar_altera_campos_e_status_e_lista_da_gestao_mostra()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var criada = await CriarAsync(gestora, NovaCampanha("Para editar"));
        var id = criada.GetProperty("id").GetGuid();

        var resposta = await gestora.PutAsJsonAsync($"/api/campanhas/{id}", new
        {
            titulo = "Editada",
            descricao = "Nova descrição",
            dataInicio = criada.GetProperty("dataInicio").GetDateTime(),
            dataFim = criada.GetProperty("dataFim").GetDateTime(),
            metaFinanceira = 2000m,
            status = "Concluida"
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var editada = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Editada", editada.GetProperty("titulo").GetString());
        Assert.Equal("Concluida", editada.GetProperty("status").GetString());

        var lista = await gestora.GetFromJsonAsync<JsonElement>("/api/campanhas");
        Assert.Contains(lista.EnumerateArray(), c => c.GetProperty("id").GetGuid() == id && c.GetProperty("status").GetString() == "Concluida");
    }

    [Fact]
    public async Task Editar_campanha_inexistente_devolve_404()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await gestora.PutAsJsonAsync($"/api/campanhas/{Guid.NewGuid()}", new
        {
            titulo = "X", descricao = "Y", dataInicio = DateTime.UtcNow, dataFim = DateTime.UtcNow.AddDays(1), metaFinanceira = 10m, status = "Ativa"
        });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Status_inexistente_devolve_400_em_portugues()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var criada = await CriarAsync(gestora, NovaCampanha());
        var resposta = await gestora.PutAsJsonAsync($"/api/campanhas/{criada.GetProperty("id").GetGuid()}", new
        {
            titulo = "X", descricao = "Y", dataInicio = DateTime.UtcNow, dataFim = DateTime.UtcNow.AddDays(1), metaFinanceira = 10m, status = "Encerrada"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("Valor inválido para o campo status.", (await Problema.LerAsync(resposta)).Errors["status"]);
    }

    [Fact]
    public async Task Editar_nao_altera_o_valor_arrecadado_mesmo_se_enviado()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var criada = await CriarAsync(gestora, NovaCampanha());
        var id = criada.GetProperty("id").GetGuid();

        var resposta = await gestora.PutAsJsonAsync($"/api/campanhas/{id}", new
        {
            titulo = "Tentativa", descricao = "Y",
            dataInicio = criada.GetProperty("dataInicio").GetDateTime(), dataFim = criada.GetProperty("dataFim").GetDateTime(),
            metaFinanceira = 1000m, status = "Ativa",
            valorTotalArrecadado = 999999m, valorArrecadado = 999999m
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var publico = await api.Cliente().GetFromJsonAsync<JsonElement>($"/api/campanhas/{id}");
        Assert.Equal(0m, publico.GetProperty("valorTotalArrecadado").GetDecimal());
    }

    [Fact]
    public async Task Painel_publico_mostra_so_Ativa_com_titulo_meta_e_valor_arrecadado()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var ativa = await CriarAsync(gestora, NovaCampanha("Aparece no painel"));
        var cancelada = await CriarAsync(gestora, NovaCampanha("Some do painel"));
        await gestora.PutAsJsonAsync($"/api/campanhas/{cancelada.GetProperty("id").GetGuid()}", new
        {
            titulo = "Some do painel", descricao = "Y",
            dataInicio = cancelada.GetProperty("dataInicio").GetDateTime(), dataFim = cancelada.GetProperty("dataFim").GetDateTime(),
            metaFinanceira = 1000m, status = "Cancelada"
        });

        using var anonimo = api.Cliente();
        var painel = (await anonimo.GetFromJsonAsync<JsonElement>("/api/campanhas/ativas")).EnumerateArray().ToList();

        Assert.Contains(painel, c => c.GetProperty("id").GetGuid() == ativa.GetProperty("id").GetGuid());
        Assert.DoesNotContain(painel, c => c.GetProperty("id").GetGuid() == cancelada.GetProperty("id").GetGuid());
        Assert.DoesNotContain(painel, c => c.GetProperty("titulo").GetString() is "Inverno Quentinho" or "Passeio ao Parque Ecológico");
        foreach (var c in painel)
        {
            Assert.True(c.TryGetProperty("titulo", out _));
            Assert.True(c.TryGetProperty("metaFinanceira", out _));
            Assert.True(c.TryGetProperty("valorTotalArrecadado", out _));
        }
    }

    [Fact]
    public async Task Painel_e_detalhe_sao_publicos()
    {
        using var anonimo = api.Cliente();
        var painel = await anonimo.GetFromJsonAsync<JsonElement>("/api/campanhas/ativas");
        var id = painel.EnumerateArray().First().GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await anonimo.GetAsync($"/api/campanhas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonimo.GetAsync($"/api/campanhas/{Guid.NewGuid()}")).StatusCode);
    }
}
