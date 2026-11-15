using System.Text.Json.Nodes;
using Campanhas.Application.DTOs;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Campanhas.API.OpenApi;

// Exemplos prontos nos corpos de requisição: o Scalar já abre o formulário preenchido.
internal sealed class ExemplosTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var inicio = DateTime.UtcNow.Date.ToString("yyyy-MM-ddT00:00:00Z");
        var fim = DateTime.UtcNow.Date.AddDays(60).ToString("yyyy-MM-ddT23:59:59Z");

        JsonNode? exemplo = context.JsonTypeInfo.Type.Name switch
        {
            nameof(LoginRequest) => new JsonObject { ["email"] = "helena@esperancasolidaria.demo", ["senha"] = "Gestao@2026" },
            nameof(CadastrarDoadorRequest) => new JsonObject
            {
                ["nomeCompleto"] = "João da Silva", ["email"] = "joao.silva@email.demo", ["cpf"] = "092.275.377-67", ["senha"] = "Doar@2026"
            },
            nameof(CriarCampanhaRequest) => new JsonObject
            {
                ["titulo"] = "Agasalho Solidário", ["descricao"] = "Casacos e cobertores para as 220 crianças acolhidas pela ONG.",
                ["dataInicio"] = inicio, ["dataFim"] = fim, ["metaFinanceira"] = 15000.00
            },
            nameof(AtualizarCampanhaRequest) => new JsonObject
            {
                ["titulo"] = "Agasalho Solidário", ["descricao"] = "Casacos e cobertores para as 220 crianças acolhidas pela ONG.",
                ["dataInicio"] = inicio, ["dataFim"] = fim, ["metaFinanceira"] = 18000.00, ["status"] = "Ativa"
            },
            // Troque pelo id de uma campanha de GET /api/campanhas/ativas.
            nameof(CriarDoacaoRequest) => new JsonObject { ["idCampanha"] = "00000000-0000-0000-0000-000000000000", ["valorDoacao"] = 50.00 },
            _ => null
        };

        if (exemplo is not null)
            schema.Example = exemplo;
        return Task.CompletedTask;
    }
}
