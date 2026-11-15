using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace Campanhas.IntegrationTests;

// Endpoints extras usados pelo site (etapa B do plano de execução).
[Collection(ApiCollection.Nome)]
public class ApoioAoFrontTests(ApiFixture api)
{
    private async Task<JsonElement> CriarCampanhaAsync(object? extra = null)
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await gestora.PostAsJsonAsync("/api/campanhas", extra ?? new
        {
            titulo = "Campanha do front", descricao = "Teste",
            dataInicio = DateTime.UtcNow.AddDays(-1), dataFim = DateTime.UtcNow.AddDays(20), metaFinanceira = 1000m,
            categoria = "Educação", capa = "livros", criancasAtendidas = 180
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    // Simula o doacoes-worker (outro serviço): marca a doação Processada e soma o valor.
    private async Task ProcessarComoWorkerAsync(Guid doacaoId)
    {
        await using var conexao = new NpgsqlConnection(api.PostgresConnectionString);
        await conexao.OpenAsync();
        await using var cmd = new NpgsqlCommand("""
            WITH d AS (UPDATE doacoes SET status = 'Processada', processada_em = now() WHERE id = @id AND status = 'Pendente' RETURNING campanha_id, valor)
            UPDATE campanhas c SET valor_arrecadado = c.valor_arrecadado + d.valor FROM d WHERE c.id = d.campanha_id
            """, conexao);
        cmd.Parameters.AddWithValue("id", doacaoId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<(HttpClient Cliente, string Email)> NovoDoadorAsync(string cpf)
    {
        var email = $"front-{Guid.NewGuid():N}@email.demo";
        using var anonimo = api.Cliente();
        (await anonimo.PostAsJsonAsync("/api/doadores", new { nomeCompleto = "Paulo Roberto Souza", email, cpf, senha = "Doar@2026" })).EnsureSuccessStatusCode();
        return (api.Cliente(await api.TokenAsync(email, "Doar@2026")), email);
    }

    [Fact]
    public async Task Categoria_capa_e_criancas_aparecem_no_painel_e_no_detalhe()
    {
        var campanha = await CriarCampanhaAsync();
        var id = campanha.GetProperty("id").GetGuid();

        var painel = await api.Cliente().GetFromJsonAsync<JsonElement>("/api/campanhas/ativas");
        var item = painel.EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == id);
        Assert.Equal("Educação", item.GetProperty("categoria").GetString());
        Assert.Equal("livros", item.GetProperty("capa").GetString());
        Assert.Equal(180, item.GetProperty("criancasAtendidas").GetInt32());
    }

    [Fact]
    public async Task Capa_ou_categoria_fora_da_lista_devolvem_400()
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await gestora.PostAsJsonAsync("/api/campanhas", new
        {
            titulo = "X", descricao = "Y", dataInicio = DateTime.UtcNow, dataFim = DateTime.UtcNow.AddDays(5), metaFinanceira = 10m, capa = "foguete"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.StartsWith("Capa inválida", (await Problema.LerAsync(resposta)).Detail);
    }

    [Fact]
    public async Task Doacoes_recentes_sao_publicas_so_processadas_e_com_nome_abreviado()
    {
        var campanha = (await CriarCampanhaAsync()).GetProperty("id").GetGuid();
        var (doador, _) = await NovoDoadorAsync("083.016.613-05");
        var processada = (await (await doador.PostAsJsonAsync("/api/doacoes", new { idCampanha = campanha, valorDoacao = 80m }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await doador.PostAsJsonAsync("/api/doacoes", new { idCampanha = campanha, valorDoacao = 5m }); // fica Pendente
        await ProcessarComoWorkerAsync(processada);

        var resposta = await api.Cliente().GetFromJsonAsync<JsonElement>($"/api/campanhas/{campanha}/doacoes-recentes");

        Assert.Equal(1, resposta.GetProperty("quantidadeDoacoes").GetInt32());
        Assert.Equal(1, resposta.GetProperty("quantidadeDoadores").GetInt32());
        var recente = Assert.Single(resposta.GetProperty("recentes").EnumerateArray());
        Assert.Equal("Paulo S.", recente.GetProperty("doador").GetString());
        Assert.Equal(80m, recente.GetProperty("valor").GetDecimal());
        Assert.DoesNotContain("@", resposta.ToString());
    }

    [Fact]
    public async Task Acompanhamento_mostra_a_doacao_so_para_o_dono_e_traz_o_titulo()
    {
        var campanha = await CriarCampanhaAsync();
        using var mariana = api.Cliente(await api.TokenDoadoraAsync());
        var doacao = await (await mariana.PostAsJsonAsync("/api/doacoes", new { idCampanha = campanha.GetProperty("id").GetGuid(), valorDoacao = 20m }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = doacao.GetProperty("id").GetGuid();

        var dela = await mariana.GetAsync($"/api/doacoes/{id}");
        Assert.Equal(HttpStatusCode.OK, dela.StatusCode);
        Assert.Equal("Campanha do front", (await dela.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tituloCampanha").GetString());

        var (outro, _) = await NovoDoadorAsync("526.018.159-06");
        Assert.Equal(HttpStatusCode.NotFound, (await outro.GetAsync($"/api/doacoes/{id}")).StatusCode);
    }

    [Fact]
    public async Task Listas_da_gestao_paginam_e_os_totais_consideram_todas_as_doacoes()
    {
        var campanha = (await CriarCampanhaAsync()).GetProperty("id").GetGuid();
        using var mariana = api.Cliente(await api.TokenDoadoraAsync());
        foreach (var valor in new[] { 10m, 20m, 30m })
        {
            var id = (await (await mariana.PostAsJsonAsync("/api/doacoes", new { idCampanha = campanha, valorDoacao = valor }))
                .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            await ProcessarComoWorkerAsync(id);
        }
        using var gestora = api.Cliente(await api.TokenGestoraAsync());

        var segunda = await gestora.GetFromJsonAsync<JsonElement>($"/api/doacoes?idCampanha={campanha}&pagina=2&tamanho=2");

        Assert.Equal(3, segunda.GetProperty("total").GetInt32());
        Assert.Equal(2, segunda.GetProperty("pagina").GetInt32());
        Assert.Single(segunda.GetProperty("itens").EnumerateArray());
        // Totais da campanha inteira, não só da página.
        Assert.Equal(60m, segunda.GetProperty("totais").GetProperty("totalConfirmado").GetDecimal());
        Assert.Equal(20m, segunda.GetProperty("totais").GetProperty("ticketMedio").GetDecimal());
    }

    [Fact]
    public async Task Gestao_lista_doacoes_doadores_e_resumo_so_para_GestorONG()
    {
        var campanha = (await CriarCampanhaAsync()).GetProperty("id").GetGuid();
        using var mariana = api.Cliente(await api.TokenDoadoraAsync());
        var doacao = (await (await mariana.PostAsJsonAsync("/api/doacoes", new { idCampanha = campanha, valorDoacao = 300m }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await ProcessarComoWorkerAsync(doacao);

        using var gestora = api.Cliente(await api.TokenGestoraAsync());

        var doacoes = await gestora.GetFromJsonAsync<JsonElement>($"/api/doacoes?idCampanha={campanha}&status=Processada");
        var item = Assert.Single(doacoes.GetProperty("itens").EnumerateArray());
        Assert.Equal("Mariana Albuquerque", item.GetProperty("nomeDoador").GetString());
        Assert.Equal(1, doacoes.GetProperty("total").GetInt32());
        var totais = doacoes.GetProperty("totais");
        Assert.Equal(300m, totais.GetProperty("totalConfirmado").GetDecimal());
        Assert.Equal(1, totais.GetProperty("confirmadas").GetInt32());
        Assert.Equal(1, totais.GetProperty("doadoresUnicos").GetInt32());

        var doadores = await gestora.GetFromJsonAsync<JsonElement>("/api/gestao/doadores?busca=mariana@email&tamanho=5");
        var marianaNaLista = doadores.GetProperty("itens").EnumerateArray().Single(d => d.GetProperty("email").GetString() == "mariana@email.demo");
        Assert.True(doadores.GetProperty("indicadores").GetProperty("cadastrados").GetInt32() >= 1);
        Assert.Equal("***.982.247-**", marianaNaLista.GetProperty("cpfMascarado").GetString());
        Assert.True(marianaNaLista.GetProperty("totalDoado").GetDecimal() >= 300m);

        var resumo = await gestora.GetFromJsonAsync<JsonElement>("/api/gestao/resumo?dias=7");
        Assert.Equal(7, resumo.GetProperty("dias").GetInt32());
        Assert.Equal(7, resumo.GetProperty("porDia").GetArrayLength());
        Assert.True(resumo.GetProperty("arrecadado").GetDecimal() >= 300m);
        Assert.True(resumo.GetProperty("arrecadadoHoje").GetDecimal() >= 300m);

        Assert.Equal(HttpStatusCode.Forbidden, (await mariana.GetAsync("/api/doacoes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await mariana.GetAsync("/api/gestao/doadores")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await mariana.GetAsync("/api/gestao/resumo")).StatusCode);
    }
}
