namespace Candidates.Api.Errors;

public class InvalidResumeFileException(string message) : Exception(message)
{
    // Usada pelo validador (arquivo acima de 5 MB) e pelo handler (envio cortado pelo servidor): a mesma frase nos dois.
    public const string TooLargeMessage = "O PDF deve ter no máximo 5 MB.";
}
