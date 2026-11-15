using Campanhas.Application.Exceptions;
using Campanhas.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Campanhas.API.Http;

// Converte exceções em ProblemDetails com o status certo.
public class ErrorHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ErrorHandler> _logger;

    public ErrorHandler(IProblemDetailsService problemDetails, ILogger<ErrorHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        // Cliente desistiu da requisição: não é erro do servidor nem precisa de resposta.
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
            return true;
        }

        var (status, titulo) = exception switch
        {
            BadHttpRequestException bad => (bad.StatusCode, "Requisição inválida"),
            DomainException => (StatusCodes.Status400BadRequest, "Dados inválidos"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autenticado"),
            NotFoundException => (StatusCodes.Status404NotFound, "Não encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflito"),
            RegraNegocioException => (StatusCodes.Status422UnprocessableEntity, "Operação não permitida"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Erro inesperado em {Path}", context.Request.Path);
        else
            _logger.LogInformation("{Status} em {Path}: {Mensagem}", status, context.Request.Path, exception.Message);

        context.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "Ocorreu um erro interno. Tente novamente mais tarde."
                    : exception.Message
            }
        });
    }
}
