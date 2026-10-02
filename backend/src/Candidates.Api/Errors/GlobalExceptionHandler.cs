using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Candidates.Api.Errors;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, detail) = exception switch
        {
            DuplicateEmailException => (StatusCodes.Status409Conflict, exception.Message),
            CandidateNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            InvalidResumeFileException => (StatusCodes.Status400BadRequest, exception.Message),
            UnreadableResumeException => (StatusCodes.Status422UnprocessableEntity, exception.Message),

            // O servidor corta o envio grande demais antes de o validador ver o arquivo: a resposta é a mesma dele.
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } =>
                (StatusCodes.Status400BadRequest, InvalidResumeFileException.TooLargeMessage),
            // Envio ilegível (multipart malformado, corpo cortado, cliente que desconectou): erro de quem enviou. BadHttpRequestException é uma IOException.
            InvalidDataException or IOException =>
                (StatusCodes.Status400BadRequest, "Não foi possível ler o envio. Envie o PDF de novo."),

            _ => (StatusCodes.Status500InternalServerError,
                "Não foi possível concluir a operação. Tente novamente em instantes."),
        };

        // Os 4xx são esperados; só o inesperado vai para o log, com o stack trace (que não sai na resposta).
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Erro não tratado em {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detail },
        });
    }
}
