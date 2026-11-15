using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Campanhas.API.OpenApi;

// Informações do documento, esquema Bearer (botão de autenticação no Scalar) e servidor relativo.
// Sem "servers" fixo o Scalar usa a origem atual: funciona direto na API e atrás do Kong.
internal sealed class DocumentoTransformer(IAuthenticationSchemeProvider esquemas) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Conexão Solidária - Campanhas API",
            Version = "v1",
            Description = """
                API da ONG Esperança Solidária: autenticação, cadastro de doadores, gestão de campanhas,
                painel de transparência e intenção de doação.

                **Fluxo da doação:** `POST /api/doacoes` grava a doação como `Pendente` e publica `DoacaoRecebidaEvent`
                no RabbitMQ. O `doacoes-worker` consome o evento e soma o valor ao total arrecadado da campanha. A API nunca altera o valor arrecadado.

                **Contas de demonstração:** gestora `helena@esperancasolidaria.demo` / `Gestao@2026`;
                doadora `mariana@email.demo` / `Doar@2026`.
                """
        };
        document.Servers = [];

        if (!(await esquemas.GetAllSchemesAsync()).Any(s => s.Name == "Bearer"))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "Token obtido em POST /api/auth/login."
            }
        };
    }
}
