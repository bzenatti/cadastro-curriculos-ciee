using Candidates.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Candidates.Api.Tests.Errors;

public class GlobalExceptionHandlerTests
{
    private const string GenericServerError = "Não foi possível concluir a operação. Tente novamente em instantes.";
    private const string UnreadableUpload = "Não foi possível ler o envio. Envie o PDF de novo.";

    // Guarda o que o handler mandou escrever, em vez de montar a resposta de verdade.
    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.FromResult(true);
        }
    }

    private static async Task<(bool Handled, int StatusCode, ProblemDetails Problem)> Handle(Exception exception)
    {
        var service = new RecordingProblemDetailsService();
        var handler = new GlobalExceptionHandler(service, NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        return (handled, httpContext.Response.StatusCode, service.Written!.ProblemDetails);
    }

    public static TheoryData<Exception, int, string> ExpectedErrors => new()
    {
        { new DuplicateEmailException(), 409, "Já existe um candidato cadastrado com este e-mail." },
        { new CandidateNotFoundException(), 404, "Candidato não encontrado." },
        { new InvalidResumeFileException("O arquivo precisa ser um PDF."), 400, "O arquivo precisa ser um PDF." },
        { new UnreadableResumeException("O PDF está protegido por senha."), 422, "O PDF está protegido por senha." },
        // O servidor corta o envio grande demais: a pessoa lê a mesma frase do validador dos 5 MB.
        { new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge), 400, InvalidResumeFileException.TooLargeMessage },
        { new BadHttpRequestException("Malformed request.", StatusCodes.Status400BadRequest), 400, UnreadableUpload },
        { new IOException("Unexpected end of Stream, the content may have already been read by another component."), 400, UnreadableUpload },
        { new InvalidDataException("Multipart body length limit exceeded."), 400, UnreadableUpload },
    };

    [Theory(DisplayName = "Cada exceção conhecida vira o status e a mensagem do contrato")]
    [MemberData(nameof(ExpectedErrors))]
    public async Task TryHandle_KnownException_MapsToStatusAndMessage(Exception exception, int status, string detail)
    {
        var (handled, statusCode, problem) = await Handle(exception);

        Assert.True(handled);
        Assert.Equal(status, statusCode);
        Assert.Equal(status, problem.Status);
        Assert.Equal(detail, problem.Detail);
    }

    [Theory(DisplayName = "Erro inesperado vira 500 genérico, sem vazar a mensagem da exceção")]
    [InlineData("Server=db,1433;Database=CandidatesDb;User Id=sa;Password=segredo-do-banco")]
    [InlineData("Cannot insert duplicate key row in object 'dbo.Candidates'")]
    public async Task TryHandle_UnexpectedException_Returns500WithoutLeaking(string internalMessage)
    {
        var (handled, statusCode, problem) = await Handle(new InvalidOperationException(internalMessage));

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Equal(GenericServerError, problem.Detail);
        Assert.DoesNotContain(internalMessage, problem.Detail);
    }
}
