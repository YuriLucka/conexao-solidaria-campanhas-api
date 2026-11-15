using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Campanhas.IntegrationTests;

// RF5 e mensageria: a API registra a intenção, publica DoacaoRecebidaEvent e NÃO altera o valor arrecadado.
[Collection(ApiCollection.Nome)]
public class DoacoesTests(ApiFixture api)
{
    private const string ExchangeDoEvento = "ConexaoSolidaria.Contracts.Events:DoacaoRecebidaEvent";

    private async Task<Guid> CampanhaAsync(string status = "Ativa")
    {
        using var gestora = api.Cliente(await api.TokenGestoraAsync());
        var resposta = await gestora.PostAsJsonAsync("/api/campanhas", new
        {
            titulo = $"Doações {Guid.NewGuid():N}"[..20], descricao = "Teste",
            dataInicio = DateTime.UtcNow.AddDays(-1), dataFim = DateTime.UtcNow.AddDays(10), metaFinanceira = 500m
        });
        var campanha = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var id = campanha.GetProperty("id").GetGuid();
        if (status != "Ativa")
            await gestora.PutAsJsonAsync($"/api/campanhas/{id}", new
            {
                titulo = "Encerrada", descricao = "Teste",
                dataInicio = campanha.GetProperty("dataInicio").GetDateTime(), dataFim = campanha.GetProperty("dataFim").GetDateTime(),
                metaFinanceira = 500m, status
            });
        return id;
    }

    [Fact]
    public async Task Doacao_devolve_202_Pendente_publica_evento_e_nao_mexe_no_valor()
    {
        // Fila temporária ligada ao exchange do evento, para ver a mensagem que a API publicou.
        var fabrica = new ConnectionFactory { Uri = new Uri(api.RabbitConnectionString) };
        await using var conexao = await fabrica.CreateConnectionAsync();
        await using var canal = await conexao.CreateChannelAsync();
        await canal.ExchangeDeclareAsync(ExchangeDoEvento, ExchangeType.Fanout, durable: true, autoDelete: false);
        var fila = (await canal.QueueDeclareAsync()).QueueName;
        await canal.QueueBindAsync(fila, ExchangeDoEvento, "");

        var campanha = await CampanhaAsync();
        using var doadora = api.Cliente(await api.TokenDoadoraAsync());

        var resposta = await doadora.PostAsJsonAsync("/api/doacoes", new { idCampanha = campanha, valorDoacao = 123.45m });

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        var doacao = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pendente", doacao.GetProperty("status").GetString());
        var doacaoId = doacao.GetProperty("id").GetGuid();

        // O outbox entrega em segundo plano: espera até 15 s.
        JsonElement? mensagem = null;
        for (var i = 0; i < 30 && mensagem is null; i++)
        {
            var item = await canal.BasicGetAsync(fila, autoAck: true);
            if (item is null) { await Task.Delay(500); continue; }
            mensagem = JsonDocument.Parse(Encoding.UTF8.GetString(item.Body.Span)).RootElement.GetProperty("message");
        }

        Assert.NotNull(mensagem);
        Assert.Equal(doacaoId, mensagem.Value.GetProperty("doacaoId").GetGuid());
        Assert.Equal(campanha, mensagem.Value.GetProperty("idCampanha").GetGuid());
        Assert.Equal("123.45", mensagem.Value.GetProperty("valorDoacao").ToString());

        // Sem worker neste teste: o valor continua zero, prova de que a API não atualiza o total.
        var publico = await api.Cliente().GetFromJsonAsync<JsonElement>($"/api/campanhas/{campanha}");
        Assert.Equal(0m, publico.GetProperty("valorTotalArrecadado").GetDecimal());

        var minhas = await doadora.GetFromJsonAsync<JsonElement>("/api/doacoes/minhas");
        Assert.Contains(minhas.EnumerateArray(), d => d.GetProperty("id").GetGuid() == doacaoId);
    }

    [Theory]
    [InlineData("Cancelada")]
    [InlineData("Concluida")]
    public async Task Campanha_encerrada_ou_cancelada_devolve_422(string status)
    {
        var campanha = await CampanhaAsync(status);
        using var doadora = api.Cliente(await api.TokenDoadoraAsync());

        var resposta = await doadora.PostAsJsonAsync("/api/doacoes", new { idCampanha = campanha, valorDoacao = 10m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Contains("não está recebendo doações", (await Problema.LerAsync(resposta)).Detail);
    }

    [Fact]
    public async Task Campanha_inexistente_devolve_404()
    {
        using var doadora = api.Cliente(await api.TokenDoadoraAsync());
        var resposta = await doadora.PostAsJsonAsync("/api/doacoes", new { idCampanha = Guid.NewGuid(), valorDoacao = 10m });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10.123)]
    public async Task Valor_invalido_devolve_400(decimal valor)
    {
        var campanha = await CampanhaAsync();
        using var doadora = api.Cliente(await api.TokenDoadoraAsync());

        var resposta = await doadora.PostAsJsonAsync("/api/doacoes", new { idCampanha = campanha, valorDoacao = valor });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Doacao_sem_token_devolve_401()
    {
        using var anonimo = api.Cliente();
        var resposta = await anonimo.PostAsJsonAsync("/api/doacoes", new { idCampanha = Guid.NewGuid(), valorDoacao = 10m });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}

[Collection(ApiCollection.Nome)]
public class InfraTests(ApiFixture api)
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/metrics")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/")]
    public async Task Rotas_de_infraestrutura_respondem_200(string rota)
    {
        using var client = api.Cliente();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(rota)).StatusCode);
    }

    [Fact]
    public async Task OpenApi_tem_Bearer_e_exemplos_de_requisicao()
    {
        using var client = api.Cliente();
        var documento = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("\"bearer\"", documento);
        Assert.Contains("helena@esperancasolidaria.demo", documento);
        Assert.Contains("Agasalho Solidário", documento);
    }

    [Fact]
    public async Task Rota_inexistente_devolve_404_em_ProblemDetails()
    {
        using var client = api.Cliente();
        var resposta = await client.GetAsync("/api/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("Não encontrado", (await Problema.LerAsync(resposta)).Title);
    }
}
