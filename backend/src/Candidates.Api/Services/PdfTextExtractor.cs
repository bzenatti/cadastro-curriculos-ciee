using Candidates.Api.Errors;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace Candidates.Api.Services;

// PDF -> texto. Lê só as primeiras páginas, onde ficam os dados de contato. Nada é gravado nem logado.
public static class PdfTextExtractor
{
    private const int MaxPages = 5;

    public static string Extract(Stream pdf)
    {
        var text = ReadText(pdf);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new UnreadableResumeException(
                "Não encontramos texto neste PDF. Se ele for uma imagem (escaneado), preencha os campos manualmente.");
        }

        return text;
    }

    private static string ReadText(Stream pdf)
    {
        try
        {
            using var document = PdfDocument.Open(pdf);
            var pages = document.GetPages().Take(MaxPages).Select(page => ContentOrderTextExtractor.GetText(page));

            return string.Join("\n", pages);
        }
        catch (PdfDocumentEncryptedException)
        {
            throw new UnreadableResumeException("O PDF está protegido por senha. Envie uma versão sem senha.");
        }
        catch (Exception)
        {
            // O PdfPig lança tipos diferentes para PDF corrompido ou fora do padrão: para a pessoa é tudo "não consegui ler".
            throw new UnreadableResumeException("Não foi possível ler este PDF. Ele pode estar corrompido.");
        }
    }
}
