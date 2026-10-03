using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Candidates.Api.Dtos;
using Candidates.Api.Services;
using UglyToad.PdfPig.Writer;

namespace Candidates.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ResumesApiTests(ApiFactory factory) : IAsyncLifetime
{
    private const string NoFile = "Envie um arquivo PDF.";
    private const string NotAPdf = "O arquivo precisa ser um PDF.";
    private const string TooLarge = "O PDF deve ter no máximo 5 MB.";

    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Os PDFs de samples/ (o mesmo que o README mostra) e o PDF com senha, copiados para a pasta dos testes.
    private static byte[] Sample(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Samples", name));

    private static byte[] PasswordProtected() =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "com-senha.pdf"));

    // "PDF" do tamanho pedido: a assinatura e zeros. Passa no validador, mas não abre como PDF.
    private static byte[] PdfSignatureOfSize(long size)
    {
        var content = new byte[size];
        "%PDF-1.7\n"u8.CopyTo(content);
        return content;
    }

    private static byte[] BlankPagePdf()
    {
        using var builder = new PdfDocumentBuilder();
        builder.AddPage(595, 842);
        return builder.Build();
    }

    private Task<HttpResponseMessage> Upload(byte[] content, string fieldName = "file", string fileName = "curriculo.pdf")
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent(content), fieldName, fileName } };
        return _client.PostAsync("/api/resumes/parse", form);
    }

    private static async Task<string> DetailOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()!;

    private async Task<int> CandidateCount() =>
        (await _client.GetFromJsonAsync<PagedResponse<CandidateListItem>>("/api/candidates"))!.TotalCount;

    // ---------- Currículos que dão certo ----------

    [Fact(DisplayName = "O currículo fictício do repositório devolve nome, e-mail e telefone")]
    public async Task Parse_FictitiousResume_FindsNameEmailAndPhone()
    {
        var response = await Upload(Sample("curriculo-ficticio.pdf"), fileName: "curriculo-ficticio.pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.Content.ReadFromJsonAsync<ResumeExtractionResponse>();
        Assert.Equal(new ResumeExtractionResponse("Maria Fictícia de Souza", "maria.ficticia@example.com", "41900001234"), found);
    }

    [Theory(DisplayName = "Os PDFs de samples/ dão o que o README documenta, inclusive as limitações")]
    [InlineData("02-duas-colunas.pdf", "John Placeholder Doe", "john.doe@example.com", "41900005678")]
    [InlineData("03-caixa-alta.pdf", "Beltrano Sicrano de Tal", "beltrano.sicrano@example.com", "4130001111")]
    [InlineData("04-rotulos.pdf", null, "fulana.exemplo@example.com", "47900003333")] // "Nome: ..." não é reconhecido
    [InlineData("05-cargo-no-topo.pdf", "Senior Software Engineer", "jane.roe@example.com", "41900004444")] // o cargo é tomado como nome
    [InlineData("06-sem-contato.pdf", "Ana-Beatriz Fictícia D'Ávila", null, null)] // sem e-mail nem telefone não é erro
    public async Task Parse_Samples_MatchTheDocumentedBehavior(string file, string? name, string? email, string? phone)
    {
        var response = await Upload(Sample(file), fileName: file);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.Content.ReadFromJsonAsync<ResumeExtractionResponse>();
        Assert.Equal(new ResumeExtractionResponse(name, email, phone), found);
    }

    [Fact(DisplayName = "Importar não grava nada: a lista continua vazia")]
    public async Task Parse_DoesNotSaveAnything()
    {
        await Upload(Sample("curriculo-ficticio.pdf"));

        Assert.Equal(0, await CandidateCount());
    }

    [Fact(DisplayName = "Importar e depois salvar o que veio: o mesmo cadastro, com as mesmas regras")]
    public async Task Parse_ThenCreateWithTheResult_RegistersTheCandidate()
    {
        var found = await (await Upload(Sample("curriculo-ficticio.pdf"))).Content.ReadFromJsonAsync<ResumeExtractionResponse>();

        var created = await _client.PostAsJsonAsync("/api/candidates", new { found!.Name, found.Email, found.Phone });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var list = await _client.GetFromJsonAsync<PagedResponse<CandidateListItem>>("/api/candidates");
        var item = Assert.Single(list!.Items);
        Assert.Equal("Maria Fictícia de Souza", item.Name);
        Assert.Equal("maria.ficticia@example.com", item.Email);
    }

    // ---------- Envio sem arquivo ou com arquivo que não é PDF (400) ----------

    [Fact(DisplayName = "Multipart sem o arquivo: 400 pedindo o PDF")]
    public async Task Parse_NoFilePart_Returns400()
    {
        var form = new MultipartFormDataContent { { new StringContent("qualquer coisa"), "observacao" } };

        var response = await _client.PostAsync("/api/resumes/parse", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(NoFile, await DetailOf(response));
    }

    [Fact(DisplayName = "Arquivo num campo com outro nome: 400 pedindo o PDF")]
    public async Task Parse_WrongFieldName_Returns400()
    {
        var response = await Upload(Sample("curriculo-ficticio.pdf"), fieldName: "arquivo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(NoFile, await DetailOf(response));
    }

    [Fact(DisplayName = "Corpo JSON em vez de multipart: 400 pedindo o PDF")]
    public async Task Parse_JsonBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/resumes/parse", new { file = "curriculo.pdf" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(NoFile, await DetailOf(response));
    }

    [Theory(DisplayName = "O conteúdo decide: arquivo que não é PDF é recusado, mesmo chamado de .pdf")]
    [InlineData("texto qualquer, mas com nome .pdf", "curriculo.pdf")]
    [InlineData("%pdf-1.7 minúsculo", "curriculo.pdf")]
    [InlineData("\u0089PNG\r\n\u001a\n", "foto.pdf")]
    [InlineData("<script>alert(1)</script>", "../../etc/passwd.pdf")]
    public async Task Parse_NotAPdf_Returns400(string content, string fileName)
    {
        var response = await Upload(Encoding.Latin1.GetBytes(content), fileName: fileName);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(NotAPdf, await DetailOf(response));
    }

    [Fact(DisplayName = "Arquivo vazio: 400, não 500")]
    public async Task Parse_EmptyFile_Returns400()
    {
        var response = await Upload([]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Tamanho ----------

    // O servidor de teste não aplica o [RequestSizeLimit] do Kestrel: aqui quem responde é sempre o validador dos 5 MB.
    [Theory(DisplayName = "Acima de 5 MB: 400 com a mensagem do limite")]
    [InlineData(5 * 1024 * 1024 + 1)]
    [InlineData(7 * 1024 * 1024)]
    public async Task Parse_OverFiveMegabytes_Returns400(int size)
    {
        var response = await Upload(PdfSignatureOfSize(size));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(TooLarge, await DetailOf(response));
    }

    [Fact(DisplayName = "Exatamente 5 MB passa pelo validador (e só falha por não abrir como PDF)")]
    public async Task Parse_ExactlyFiveMegabytes_IsNotRefusedForSize()
    {
        var response = await Upload(PdfSignatureOfSize(PdfFileValidator.MaxSizeBytes));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---------- PDF que não dá para ler (422) ----------

    [Fact(DisplayName = "PDF protegido por senha: 422 pedindo uma versão sem senha")]
    public async Task Parse_PasswordProtectedPdf_Returns422()
    {
        var response = await Upload(PasswordProtected());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("senha", await DetailOf(response));
    }

    [Fact(DisplayName = "PDF corrompido: 422 dizendo que não foi possível ler")]
    public async Task Parse_CorruptedPdf_Returns422()
    {
        var response = await Upload("%PDF-1.7\n1 0 obj\n<< /Type /Catalog"u8.ToArray());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("Não foi possível ler este PDF", await DetailOf(response));
    }

    [Fact(DisplayName = "PDF sem texto (página em branco ou escaneado): 422 orientando o preenchimento manual")]
    public async Task Parse_PdfWithoutText_Returns422()
    {
        var response = await Upload(BlankPagePdf());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("preencha os campos manualmente", await DetailOf(response));
    }

    // ---------- Envio cortado ----------

    [Fact(DisplayName = "Multipart cortado no meio: 400 genérico, não 500")]
    public async Task Parse_TruncatedMultipart_Returns400()
    {
        const string truncated = "--limite\r\nContent-Disposition: form-data; name=\"file\"; filename=\"a.pdf\"\r\n\r\n%PDF-1.7 o envio acaba aqui";
        var content = new StringContent(truncated, Encoding.ASCII);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=limite");

        var response = await _client.PostAsync("/api/resumes/parse", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Não foi possível ler o envio. Envie o PDF de novo.", await DetailOf(response));
    }
}
