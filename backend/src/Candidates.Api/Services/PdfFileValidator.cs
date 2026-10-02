using System.Diagnostics.CodeAnalysis;
using Candidates.Api.Errors;

namespace Candidates.Api.Services;

// Confere o arquivo enviado antes de ler qualquer coisa dele. Mensagens iguais às do front (ResumeUpload).
public static class PdfFileValidator
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    private static ReadOnlySpan<byte> PdfSignature => "%PDF-"u8;

    public static void Validate([NotNull] IFormFile? file)
    {
        if (file is null) throw new InvalidResumeFileException("Envie um arquivo PDF.");
        if (file.Length > MaxSizeBytes) throw new InvalidResumeFileException(InvalidResumeFileException.TooLargeMessage);
        if (!StartsWithPdfSignature(file)) throw new InvalidResumeFileException("O arquivo precisa ser um PDF.");
    }

    // Todo PDF começa com "%PDF-". Confere esses bytes, e não a extensão nem o Content-Type, que o cliente escolhe.
    private static bool StartsWithPdfSignature(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var buffer = new byte[PdfSignature.Length];
        var read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);

        return read == buffer.Length && buffer.AsSpan().SequenceEqual(PdfSignature);
    }
}
