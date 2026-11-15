using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Campanhas.IntegrationTests;

// Sobe PostgreSQL, Redis e RabbitMQ em containers (Testcontainers) e a API inteira em memória.
// Tudo real: migrações, seed, JWT, outbox do MassTransit e cache. Precisa de Docker (o runner do GitHub tem).
public class ApiFixture : IAsyncLifetime
{
    public const string SegredoJwt = "chave-de-teste-de-integracao-com-mais-de-32-caracteres";
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4.4-management").WithUsername("teste").WithPassword("teste").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string RabbitConnectionString => _rabbit.GetConnectionString();
    public string PostgresConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), _rabbit.StartAsync());

        var rabbit = new Uri(_rabbit.GetConnectionString());
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
            b.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
            b.UseSetting("RabbitMq:Host", rabbit.Host);
            b.UseSetting("RabbitMq:Port", rabbit.Port.ToString());
            b.UseSetting("RabbitMq:User", "teste");
            b.UseSetting("RabbitMq:Pass", "teste");
            b.UseSetting("JwtSettings:SecretKey", SegredoJwt);
            b.UseSetting("Seed:Enabled", "true");
        });

        // Força a subida (migrações + seed) antes dos testes.
        using var client = Factory.CreateClient();
        (await client.GetAsync("/health/live")).EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask(), _rabbit.DisposeAsync().AsTask());
    }

    public HttpClient Cliente(string? token = null)
    {
        var client = Factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<string> TokenAsync(string email, string senha)
    {
        using var client = Cliente();
        var resposta = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
    }

    public Task<string> TokenGestoraAsync() => TokenAsync("helena@esperancasolidaria.demo", "Gestao@2026");
    public Task<string> TokenDoadoraAsync() => TokenAsync("mariana@email.demo", "Doar@2026");
}

[CollectionDefinition(Nome)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Nome = "api";
}
