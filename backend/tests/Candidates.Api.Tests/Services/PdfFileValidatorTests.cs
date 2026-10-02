using System.Text;
using Candidates.Api.Errors;
using Candidates.Api.Services;
using Microsoft.AspNetCore.Http;

namespace Candidates.Api.Tests.Services;

public class PdfFileValidatorTests
{
    private static readonly byte[] PdfStart = Encoding.ASCII.GetBytes("%PDF-1.7\n");

    // Arquivo de upload de mentira, em memória.
    private static FormFile FileWith(byte[] content, string fileName = "curriculo.pdf") =>
        new(new MemoryStream(content), 0, content.Length, "file", fileName);

    // "PDF" do tamanho pedido: a assinatura e zeros, o bastante para o validador.
    private static byte[] PdfOfSize(long size)
    {
        var content = new byte[size];
        PdfStart.CopyTo(content, 0);
        return content;
    }

    private static string MessageOfRefusal(IFormFile? file) =>
        Assert.Throws<InvalidResumeFileException>(() => PdfFileValidator.Validate(file)).Message;

    [Fact(DisplayName = "PDF dentro do limite é aceito")]
    public void Validate_ValidPdf_DoesNotThrow()
    {
        Assert.Null(Record.Exception(() => PdfFileValidator.Validate(FileWith(PdfOfSize(1000)))));
    }

    [Fact(DisplayName = "O conteúdo decide, não o nome: PDF com outra extensão é aceito")]
    public void Validate_PdfWithOtherExtension_DoesNotThrow()
    {
        var file = FileWith(PdfOfSize(1000), "curriculo.txt");

        Assert.Null(Record.Exception(() => PdfFileValidator.Validate(file)));
    }

    [Fact(DisplayName = "Sem arquivo pede o envio de um PDF")]
    public void Validate_NullFile_Throws()
    {
        Assert.Equal("Envie um arquivo PDF.", MessageOfRefusal(null));
    }

    [Fact(DisplayName = "Exatamente 5 MB ainda é aceito")]
    public void Validate_ExactlyMaxSize_DoesNotThrow()
    {
        var file = FileWith(PdfOfSize(PdfFileValidator.MaxSizeBytes));

        Assert.Null(Record.Exception(() => PdfFileValidator.Validate(file)));
    }

    [Fact(DisplayName = "Mais de 5 MB é recusado")]
    public void Validate_OverMaxSize_Throws()
    {
        var file = FileWith(PdfOfSize(PdfFileValidator.MaxSizeBytes + 1));

        Assert.Equal("O PDF deve ter no máximo 5 MB.", MessageOfRefusal(file));
    }

    [Theory(DisplayName = "Arquivo que não começa com %PDF- é recusado, mesmo com nome .pdf")]
    [InlineData("")]
    [InlineData("texto qualquer")]
    [InlineData("%PDF")]
    [InlineData("%pdf-1.7")]
    [InlineData(" %PDF-1.7")]
    [InlineData("\u0089PNG\r\n\u001a\n")]
    public void Validate_NotAPdf_Throws(string content)
    {
        var file = FileWith(Encoding.Latin1.GetBytes(content));

        Assert.Equal("O arquivo precisa ser um PDF.", MessageOfRefusal(file));
    }
}
