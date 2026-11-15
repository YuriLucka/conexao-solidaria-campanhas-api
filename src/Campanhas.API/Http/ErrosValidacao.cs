using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Campanhas.API.Http;

// Erros de validação automática (corpo ausente, JSON inválido, campo obrigatório) em português,
// no mesmo formato ProblemDetails dos erros de domínio, com a lista por campo em "errors".
public static class ErrosValidacao
{
    public static IActionResult Responder(ActionContext context)
    {
        var erros = new Dictionary<string, string[]>();
        foreach (var (chave, entrada) in context.ModelState)
        {
            if (entrada.Errors.Count == 0)
                continue;

            var campo = NomeDoCampo(chave);
            erros[campo] = entrada.Errors.Select(e => Traduzir(campo, e)).Distinct().ToArray();
        }

        var problema = new ValidationProblemDetails(erros)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Dados inválidos",
            Detail = erros.Count == 1 && erros.First().Value.Length == 1
                ? erros.First().Value[0]
                : "Revise os campos indicados na lista errors.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };
        problema.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
    }

    // "$.metaFinanceira" -> "metaFinanceira"; "Cpf" -> "cpf"; "" ou "$" -> "corpo".
    private static string NomeDoCampo(string chave)
    {
        var nome = chave.StartsWith("$.") ? chave[2..] : chave;
        if (nome is "" or "$" or "request")
            return "corpo";
        return char.ToLowerInvariant(nome[0]) + nome[1..];
    }

    private static string Traduzir(string campo, ModelError erro)
    {
        if (campo == "corpo")
            return "Corpo da requisição ausente ou com JSON inválido.";
        if (erro.Exception is JsonException || erro.ErrorMessage.Contains("could not be converted", StringComparison.OrdinalIgnoreCase))
            return $"Valor inválido para o campo {campo}.";
        if (erro.ErrorMessage.Contains("is required", StringComparison.OrdinalIgnoreCase))
            return $"O campo {campo} é obrigatório.";
        if (erro.ErrorMessage.Contains("is not valid", StringComparison.OrdinalIgnoreCase)
            || erro.ErrorMessage.Contains("is invalid", StringComparison.OrdinalIgnoreCase))
            return $"Valor inválido para o campo {campo}.";
        return string.IsNullOrWhiteSpace(erro.ErrorMessage) ? $"Valor inválido para o campo {campo}." : erro.ErrorMessage;
    }

    // Títulos em português para respostas sem corpo (401 do JwtBearer, 403, 404 de rota, 405, 415).
    public static string? TituloPorStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Dados inválidos",
        StatusCodes.Status401Unauthorized => "Não autenticado: envie o token JWT no cabeçalho Authorization (Bearer)",
        StatusCodes.Status403Forbidden => "Acesso negado: seu perfil não tem permissão para este recurso",
        StatusCodes.Status404NotFound => "Não encontrado",
        StatusCodes.Status405MethodNotAllowed => "Método não permitido",
        StatusCodes.Status415UnsupportedMediaType => "Tipo de conteúdo não suportado: envie o corpo como application/json",
        StatusCodes.Status422UnprocessableEntity => "Operação não permitida",
        StatusCodes.Status500InternalServerError => "Erro interno",
        _ => null
    };
}
