using System.Net.Http.Json;
using System.Text.Json;

namespace Campanhas.IntegrationTests;

// Leitura de ProblemDetails das respostas de erro.
public record Problema(int? Status, string? Title, string? Detail, Dictionary<string, string[]> Errors)
{
    public static async Task<Problema> LerAsync(HttpResponseMessage resposta)
    {
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        var erros = new Dictionary<string, string[]>();
        if (json.TryGetProperty("errors", out var e))
            foreach (var campo in e.EnumerateObject())
                erros[campo.Name] = campo.Value.EnumerateArray().Select(x => x.GetString()!).ToArray();

        return new Problema(
            json.TryGetProperty("status", out var s) ? s.GetInt32() : null,
            json.TryGetProperty("title", out var t) ? t.GetString() : null,
            json.TryGetProperty("detail", out var d) ? d.GetString() : null,
            erros);
    }
}
