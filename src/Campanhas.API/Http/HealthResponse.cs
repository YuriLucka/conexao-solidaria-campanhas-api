using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Campanhas.API.Http;

// Resposta do /health em JSON: {"status":"Healthy","checks":{"postgres":"Healthy"}}. 503 quando algo falha.
public static class HealthResponse
{
    public static Task EscreverAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var corpo = new
        {
            status = report.Status.ToString(),
            servico = "campanhas-api",
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString())
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(corpo));
    }
}
