using System.Text;
using Candidates.Api.Dtos;
using Candidates.Api.Errors;
using Candidates.Api.Services;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Candidates.Api.Tests.Services;

public class PdfTextExtractorTests
{
    // Monta um PDF em memória. Cada argumento é uma página; as linhas (separadas por \n) descem a partir do topo.
    private static MemoryStream PdfWith(params string[] pages)
    {
        using var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var text in pages)
        {
            var page = builder.AddPage(595, 842);
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                page.AddText(lines[i], 12, new PdfPoint(50, 780 - 20 * i), font);
            }
        }

        return new MemoryStream(builder.Build());
    }

    [Fact(DisplayName = "Cada linha do PDF vira uma linha do texto")]
    public void Extract_TextPdf_ReturnsOneLinePerPdfLine()
    {
        var text = PdfTextExtractor.Extract(PdfWith("Maria da Silva\nmaria@example.com"));
        var lines = text.Split('\n', StringSplitOptions.TrimEntries);

        Assert.Contains("Maria da Silva", lines);
        Assert.Contains("maria@example.com", lines);
    }

    [Fact(DisplayName = "PDF -> texto -> parser encontra nome, e-mail e telefone")]
    public void Extract_ThenParse_FindsTheFields()
    {
        var text = PdfTextExtractor.Extract(PdfWith("Maria da Silva\nTel: 41 99876-5432\nmaria@example.com"));

        var expected = new ResumeExtractionResponse("Maria da Silva", "maria@example.com", "41998765432");
        Assert.Equal(expected, ResumeParser.Parse(text));
    }

    [Fact(DisplayName = "Lê só as 5 primeiras páginas")]
    public void Extract_SixPages_ReadsOnlyTheFirstFive()
    {
        var text = PdfTextExtractor.Extract(PdfWith("pagina 1", "pagina 2", "pagina 3", "pagina 4", "pagina 5", "pagina 6"));

        Assert.Contains("pagina 5", text);
        Assert.DoesNotContain("pagina 6", text);
    }

    [Fact(DisplayName = "PDF sem texto (página em branco ou imagem) é ilegível")]
    public void Extract_PdfWithoutText_Throws()
    {
        var error = Assert.Throws<UnreadableResumeException>(() => PdfTextExtractor.Extract(PdfWith("")));

        Assert.Contains("texto", error.Message);
    }

    [Theory(DisplayName = "Arquivo que não abre como PDF é ilegível")]
    [InlineData("")]
    [InlineData("isto não é um PDF")]
    [InlineData("%PDF-1.7\n1 0 obj\n<< /Type /Catalog")]
    public void Extract_CorruptedFile_Throws(string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        Assert.Throws<UnreadableResumeException>(() => PdfTextExtractor.Extract(stream));
    }

    [Fact(DisplayName = "PDF protegido por senha é ilegível, com mensagem própria")]
    public void Extract_PasswordProtectedPdf_Throws()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "com-senha.pdf"));

        var error = Assert.Throws<UnreadableResumeException>(() => PdfTextExtractor.Extract(stream));

        Assert.Contains("senha", error.Message);
    }
}
