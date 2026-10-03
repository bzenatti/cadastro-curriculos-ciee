using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Candidates.Api.Dtos;
using Candidates.Api.Models;

namespace Candidates.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class CandidatesApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private Task<HttpResponseMessage> Post(object body) => _client.PostAsJsonAsync("/api/candidates", body);

    private static async Task<JsonElement> JsonOf(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static string ErrorOn(JsonElement problem, string field) =>
        problem.GetProperty("errors").GetProperty(field)[0].GetString()!;

    private async Task<CandidateResponse> Register(string name, string email)
    {
        var response = await Post(new { name, email });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CandidateResponse>())!;
    }

    private Task<PagedResponse<CandidateListItem>> List(string query = "") =>
        _client.GetFromJsonAsync<PagedResponse<CandidateListItem>>($"/api/candidates{query}")!;

    // ---------- Cadastro ----------

    [Fact(DisplayName = "Cadastro completo: 201, o Location leva ao detalhe e o candidato aparece na lista")]
    public async Task Create_ValidCandidate_AppearsInDetailAndList()
    {
        var response = await Post(new
        {
            name = "Maria da Silva",
            email = "maria@example.com",
            phone = "41998765432",
            position = "Desenvolvedora .NET",
            summary = "Seis anos com APIs.",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CandidateResponse>();
        Assert.NotNull(created);
        Assert.EndsWith($"/api/candidates/{created.Id}", response.Headers.Location!.ToString());

        var detail = await _client.GetFromJsonAsync<CandidateResponse>(response.Headers.Location);
        Assert.Equal(created, detail);
        Assert.Equal("Maria da Silva", detail.Name);
        Assert.Equal("41998765432", detail.Phone);

        var list = await List();
        Assert.Equal(1, list.TotalCount);
        Assert.Equal(created.Id, Assert.Single(list.Items).Id);
    }

    [Fact(DisplayName = "Só nome e e-mail: 201, e telefone, cargo e resumo voltam null")]
    public async Task Create_OnlyRequiredFields_OptionalFieldsAreNull()
    {
        var created = await Register("Maria da Silva", "maria@example.com");

        Assert.Null(created.Phone);
        Assert.Null(created.Position);
        Assert.Null(created.Summary);
    }

    [Fact(DisplayName = "Nome e e-mail vazios: 400 com errors em camelCase e traceId, e nada é gravado")]
    public async Task Create_MissingRequiredFields_Returns400WithFieldErrors()
    {
        var response = await Post(new { name = "", email = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await JsonOf(response);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.Equal("Informe o nome completo.", ErrorOn(problem, "name"));
        Assert.Equal("Informe o e-mail.", ErrorOn(problem, "email"));
        Assert.True(problem.TryGetProperty("traceId", out _));
        Assert.Equal(0, (await List()).TotalCount);
    }

    [Fact(DisplayName = "Todos os campos inválidos: cada um aparece em errors, com o nome do formulário")]
    public async Task Create_AllFieldsInvalid_ReportsEachFieldInCamelCase()
    {
        var response = await Post(new
        {
            name = new string('n', CandidateLimits.Name + 1),
            email = "ana",
            phone = "123",
            position = new string('p', CandidateLimits.Position + 1),
            summary = new string('s', CandidateLimits.Summary + 1),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await JsonOf(response);
        Assert.Equal($"Use no máximo {CandidateLimits.Name} caracteres.", ErrorOn(problem, "name"));
        Assert.Equal("Informe um e-mail válido, como nome@exemplo.com.", ErrorOn(problem, "email"));
        Assert.Equal("Informe só números, com DDD. Ex.: 41998765432.", ErrorOn(problem, "phone"));
        Assert.Equal($"Use no máximo {CandidateLimits.Position} caracteres.", ErrorOn(problem, "position"));
        Assert.Equal($"Use no máximo {CandidateLimits.Summary} caracteres.", ErrorOn(problem, "summary"));
    }

    [Fact(DisplayName = "Todos os campos no limite cabem na coluna e voltam inteiros")]
    public async Task Create_FieldsAtLimit_AreStoredWhole()
    {
        var body = new
        {
            name = new string('n', CandidateLimits.Name),
            email = new string('a', CandidateLimits.Email - "@example.com".Length) + "@example.com",
            phone = "41998765432",
            position = new string('p', CandidateLimits.Position),
            summary = new string('s', CandidateLimits.Summary),
        };

        var response = await Post(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await _client.GetFromJsonAsync<CandidateResponse>(response.Headers.Location);
        Assert.Equal(body.name, detail!.Name);
        Assert.Equal(body.email, detail.Email);
        Assert.Equal(body.position, detail.Position);
        Assert.Equal(body.summary, detail.Summary);
    }

    [Fact(DisplayName = "Emoji conta 2 no limite, igual à coluna: 75 no nome e 1000 no resumo cabem")]
    public async Task Create_EmojiAtLimit_IsStoredWhole()
    {
        var name = string.Concat(Enumerable.Repeat("😀", CandidateLimits.Name / 2));
        var summary = string.Concat(Enumerable.Repeat("😀", CandidateLimits.Summary / 2));

        var response = await Post(new { name, email = "emoji@example.com", summary });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await _client.GetFromJsonAsync<CandidateResponse>(response.Headers.Location);
        Assert.Equal(name, detail!.Name);
        Assert.Equal(summary, detail.Summary);
    }

    [Theory(DisplayName = "E-mail já cadastrado: 409 com mensagem, inclusive com outra caixa, e só o primeiro fica")]
    [InlineData("maria@example.com")]
    [InlineData("MARIA@EXAMPLE.COM")]
    [InlineData("Maria@Example.com")]
    public async Task Create_DuplicateEmail_Returns409(string secondEmail)
    {
        await Register("Maria da Silva", "maria@example.com");

        var response = await Post(new { name = "Outra Maria", email = secondEmail });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await JsonOf(response);
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Equal("Já existe um candidato cadastrado com este e-mail.", problem.GetProperty("detail").GetString());
        Assert.Equal("Maria da Silva", Assert.Single((await List()).Items).Name);
    }

    [Fact(DisplayName = "10 cadastros simultâneos do mesmo e-mail: um 201, nove 409 e um registro só")]
    public async Task Create_SameEmailInParallel_OnlyOneWins()
    {
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(i => Post(new { name = $"Maria {i}", email = "corrida@example.com" })));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(9, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, (await List()).TotalCount);
    }

    [Fact(DisplayName = "Espaços nas pontas saem; campo opcional só com espaços vira null")]
    public async Task Create_SurroundingWhitespace_IsTrimmed()
    {
        var response = await Post(new
        {
            name = "  Maria da Silva  ",
            email = "maria@example.com",
            phone = "",
            position = "   ",
            summary = "  Seis anos com APIs.  ",
        });

        var created = await response.Content.ReadFromJsonAsync<CandidateResponse>();
        Assert.Equal("Maria da Silva", created!.Name);
        Assert.Null(created.Phone);
        Assert.Null(created.Position);
        Assert.Equal("Seis anos com APIs.", created.Summary);
    }

    [Theory(DisplayName = "Texto de ataque ou fora do comum é gravado e devolvido exatamente como veio")]
    [InlineData("Robert'); DROP TABLE Candidates;--")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("José d'Ávila-Souza 😀")]
    [InlineData("Zoë \"Z\" O'Neil & Filhos <fi@example.com>")]
    public async Task Create_HostileOrUnusualText_RoundTripsUntouched(string text)
    {
        var summary = "linha 1\nlinha 2\n" + text;

        var response = await Post(new { name = text, email = "texto@example.com", summary });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await _client.GetFromJsonAsync<CandidateResponse>(response.Headers.Location);
        Assert.Equal(text, detail!.Name);
        Assert.Equal(summary, detail.Summary);
        Assert.Equal(1, (await List()).TotalCount);
    }

    [Fact(DisplayName = "Campos a mais no corpo (id, createdAt) são ignorados: quem decide é o servidor")]
    public async Task Create_ExtraFieldsInBody_AreIgnored()
    {
        var response = await Post(new
        {
            id = 999,
            createdAt = "2000-01-01T00:00:00Z",
            isAdmin = true,
            name = "Maria da Silva",
            email = "maria@example.com",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CandidateResponse>();
        Assert.NotEqual(999, created!.Id);
        Assert.True(created.CreatedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    [Theory(DisplayName = "Corpo que não é um JSON de cadastro: 400, não 500")]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task Create_MalformedBody_Returns400(string body)
    {
        var response = await _client.PostAsync(
            "/api/candidates", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Lista ----------

    [Fact(DisplayName = "Sem candidatos: lista vazia, não erro")]
    public async Task List_NoCandidates_ReturnsEmptyPage()
    {
        var list = await List();

        Assert.Empty(list.Items);
        Assert.Equal(0, list.TotalCount);
        Assert.Equal(1, list.Page);
    }

    [Fact(DisplayName = "O mais recente vem primeiro")]
    public async Task List_ReturnsNewestFirst()
    {
        await Register("Primeira", "primeira@example.com");
        await Register("Segunda", "segunda@example.com");
        await Register("Terceira", "terceira@example.com");

        var names = (await List()).Items.Select(item => item.Name);

        Assert.Equal(["Terceira", "Segunda", "Primeira"], names);
    }

    [Fact(DisplayName = "12 candidatos em páginas de 5: 5, 5 e 2, sem repetir nem pular ninguém")]
    public async Task List_Pagination_CoversEveryoneExactlyOnce()
    {
        var createdIds = new List<int>();
        for (var i = 1; i <= 12; i++)
        {
            createdIds.Add((await Register($"Candidato {i}", $"c{i}@example.com")).Id);
        }

        var pages = new[] { await List("?page=1&pageSize=5"), await List("?page=2&pageSize=5"), await List("?page=3&pageSize=5") };

        Assert.Equal([5, 5, 2], pages.Select(page => page.Items.Count));
        Assert.All(pages, page => Assert.Equal(12, page.TotalCount));
        var listedIds = pages.SelectMany(page => page.Items).Select(item => item.Id).ToList();
        Assert.Equal(createdIds.AsEnumerable().Reverse(), listedIds);
    }

    [Theory(DisplayName = "Página ou tamanho fora do intervalo são ajustados, sem erro")]
    [InlineData(0, 10, 1, 10, 3)]
    [InlineData(-1, 10, 1, 10, 3)]
    [InlineData(999, 10, 1, 10, 3)]
    [InlineData(999, 2, 2, 2, 1)]
    [InlineData(1, 0, 1, 1, 1)]
    [InlineData(1, -5, 1, 1, 1)]
    [InlineData(1, 1000, 1, 50, 3)]
    public async Task List_OutOfRangePaging_IsClamped(int page, int pageSize, int expectedPage, int expectedPageSize, int expectedItems)
    {
        await Register("Primeira", "primeira@example.com");
        await Register("Segunda", "segunda@example.com");
        await Register("Terceira", "terceira@example.com");

        var list = await List($"?page={page}&pageSize={pageSize}");

        Assert.Equal(expectedPage, list.Page);
        Assert.Equal(expectedPageSize, list.PageSize);
        Assert.Equal(expectedItems, list.Items.Count);
        Assert.Equal(3, list.TotalCount);
    }

    [Fact(DisplayName = "A lista só traz o resumo: sem telefone nem resumo profissional")]
    public async Task List_Item_DoesNotExposePhoneOrSummary()
    {
        await Post(new { name = "Maria da Silva", email = "maria@example.com", phone = "41998765432", summary = "Texto" });

        var response = await _client.GetAsync("/api/candidates");

        var item = (await JsonOf(response)).GetProperty("items")[0];
        Assert.True(item.TryGetProperty("name", out _));
        Assert.True(item.TryGetProperty("email", out _));
        Assert.False(item.TryGetProperty("phone", out _));
        Assert.False(item.TryGetProperty("summary", out _));
    }

    // ---------- Detalhe ----------

    [Fact(DisplayName = "Detalhe traz todos os campos, com opcionais null e a data em UTC (termina em Z)")]
    public async Task Detail_ReturnsAllFields_WithUtcDate()
    {
        var created = await Register("Maria da Silva", "maria@example.com");

        var detail = await JsonOf(await _client.GetAsync($"/api/candidates/{created.Id}"));

        Assert.Equal("Maria da Silva", detail.GetProperty("name").GetString());
        Assert.Equal("maria@example.com", detail.GetProperty("email").GetString());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("phone").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("position").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("summary").ValueKind);
        Assert.EndsWith("Z", detail.GetProperty("createdAt").GetString());
    }

    [Theory(DisplayName = "Id que não existe: 404 com mensagem, nunca 500")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("999999")]
    public async Task Detail_UnknownId_Returns404WithMessage(string id)
    {
        var response = await _client.GetAsync($"/api/candidates/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Candidato não encontrado.", (await JsonOf(response)).GetProperty("detail").GetString());
    }

    [Theory(DisplayName = "Id que nem é um número (link editado à mão, tentativa de injeção): 404")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("99999999999")]
    [InlineData("1'--")]
    [InlineData("1;DROP")]
    public async Task Detail_InvalidId_Returns404(string id)
    {
        var response = await _client.GetAsync($"/api/candidates/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
