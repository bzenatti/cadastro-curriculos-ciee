using System.ComponentModel.DataAnnotations;
using Candidates.Api.Dtos;
using Candidates.Api.Models;

namespace Candidates.Api.Tests.Dtos;

public class CreateCandidateRequestTests
{
    private const string NameRequired = "Informe o nome completo.";
    private const string EmailRequired = "Informe o e-mail.";
    private const string EmailInvalid = "Informe um e-mail válido, como nome@exemplo.com.";
    private const string PhoneInvalid = "Informe só números, com DDD. Ex.: 41998765432.";

    private static CreateCandidateRequest ValidRequest() =>
        new() { Name = "Maria da Silva", Email = "maria@example.com" };

    // As mensagens que o POST devolve em "errors" para o campo: é a mesma validação que o MVC roda.
    private static string[] ErrorsOn(CreateCandidateRequest request, string member)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results
            .Where(result => result.MemberNames.Contains(member))
            .Select(result => result.ErrorMessage!)
            .ToArray();
    }

    // Valor com o tamanho pedido. O e-mail precisa continuar sendo um e-mail, para só o tamanho poder falhar.
    private static string OfLength(string member, int length) =>
        member == nameof(CreateCandidateRequest.Email)
            ? new string('a', length - "@example.com".Length) + "@example.com"
            : new string('x', length);

    private static CreateCandidateRequest With(string member, string? value)
    {
        var request = ValidRequest();
        typeof(CreateCandidateRequest).GetProperty(member)!.SetValue(request, value);
        return request;
    }

    public static TheoryData<string, int> Limits => new()
    {
        { nameof(CreateCandidateRequest.Name), CandidateLimits.Name },
        { nameof(CreateCandidateRequest.Email), CandidateLimits.Email },
        { nameof(CreateCandidateRequest.Position), CandidateLimits.Position },
        { nameof(CreateCandidateRequest.Summary), CandidateLimits.Summary },
    };

    // ---------- Campos obrigatórios ----------

    [Fact(DisplayName = "Só nome e e-mail já é um cadastro válido")]
    public void Validate_NameAndEmailOnly_IsValid()
    {
        var request = ValidRequest();

        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), null, true));
    }

    [Fact(DisplayName = "Cadastro com todos os campos é válido")]
    public void Validate_AllFields_IsValid()
    {
        var request = new CreateCandidateRequest
        {
            Name = "Maria da Silva",
            Email = "maria@example.com",
            Phone = "41998765432",
            Position = "Desenvolvedora .NET",
            Summary = "Seis anos com APIs.\nGosto de testes.",
        };

        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), null, true));
    }

    [Theory(DisplayName = "Nome vazio, em branco ou ausente é recusado")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Validate_BlankName_Refused(string? name)
    {
        Assert.Equal([NameRequired], ErrorsOn(With(nameof(CreateCandidateRequest.Name), name), "Name"));
    }

    [Theory(DisplayName = "E-mail vazio, em branco ou ausente é recusado")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankEmail_Refused(string? email)
    {
        Assert.Equal([EmailRequired], ErrorsOn(With(nameof(CreateCandidateRequest.Email), email), "Email"));
    }

    [Fact(DisplayName = "Formulário vazio acusa nome e e-mail de uma vez")]
    public void Validate_EmptyRequest_ReportsBothRequiredFields()
    {
        var request = new CreateCandidateRequest();

        Assert.Equal([NameRequired], ErrorsOn(request, "Name"));
        Assert.Equal([EmailRequired], ErrorsOn(request, "Email"));
    }

    // ---------- Formato do e-mail ----------

    [Theory(DisplayName = "E-mail em formato inválido é recusado")]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    [InlineData("ana@example")]
    [InlineData("ana@.com")]
    [InlineData("ana@@example.com")]
    [InlineData("ana @example.com")]
    [InlineData("ana@exam ple.com")]
    [InlineData(" ana@example.com")]
    [InlineData("ana@example.com ")]
    [InlineData("ana@example.com\n")]
    [InlineData("ana@example.com, bia@example.com")]
    public void Validate_InvalidEmail_Refused(string email)
    {
        Assert.Equal([EmailInvalid], ErrorsOn(With(nameof(CreateCandidateRequest.Email), email), "Email"));
    }

    [Theory(DisplayName = "E-mails válidos, mesmo incomuns, são aceitos")]
    [InlineData("ana@example.com")]
    [InlineData("ana.silva+rh@empresa.example.com.br")]
    [InlineData("ANA@EXAMPLE.COM")]
    [InlineData("joão@example.com")]
    [InlineData("o'brien@example.org")]
    [InlineData("a@b.co")]
    public void Validate_ValidEmail_Accepted(string email)
    {
        Assert.Empty(ErrorsOn(With(nameof(CreateCandidateRequest.Email), email), "Email"));
    }

    // ---------- Telefone ----------

    [Theory(DisplayName = "Telefone ausente ou com 10 ou 11 dígitos é aceito")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("4133334444")]
    [InlineData("41998765432")]
    public void Validate_ValidPhone_Accepted(string? phone)
    {
        Assert.Empty(ErrorsOn(With(nameof(CreateCandidateRequest.Phone), phone), "Phone"));
    }

    [Theory(DisplayName = "Telefone com máscara, tamanho errado ou caracteres que não são 0-9 é recusado")]
    [InlineData("419987654")]
    [InlineData("419987654321")]
    [InlineData("(41) 99876-5432")]
    [InlineData("41 99876-5432")]
    [InlineData("+5541998765432")]
    [InlineData("4199876543a")]
    [InlineData("41998765432\n")]
    [InlineData("٤١٩٩٨٧٦٥٤٣٢")] // dígitos arábico-indianos: \d os aceitaria, [0-9] não
    public void Validate_InvalidPhone_Refused(string phone)
    {
        Assert.Equal([PhoneInvalid], ErrorsOn(With(nameof(CreateCandidateRequest.Phone), phone), "Phone"));
    }

    // ---------- Tamanhos máximos ----------

    [Theory(DisplayName = "Campo exatamente no limite é aceito")]
    [MemberData(nameof(Limits))]
    public void Validate_FieldAtLimit_Accepted(string member, int limit)
    {
        Assert.Empty(ErrorsOn(With(member, OfLength(member, limit)), member));
    }

    [Theory(DisplayName = "Um caractere acima do limite é recusado, com o limite na mensagem")]
    [MemberData(nameof(Limits))]
    public void Validate_FieldOverLimit_Refused(string member, int limit)
    {
        var errors = ErrorsOn(With(member, OfLength(member, limit + 1)), member);

        Assert.Equal([$"Use no máximo {limit} caracteres."], errors);
    }

    [Fact(DisplayName = "O limite conta como a coluna nvarchar: um emoji vale 2")]
    public void Validate_EmojiCountsAsTwo()
    {
        var atLimit = string.Concat(Enumerable.Repeat("😀", CandidateLimits.Name / 2));
        var overLimit = atLimit + "😀";

        Assert.Empty(ErrorsOn(With(nameof(CreateCandidateRequest.Name), atLimit), "Name"));
        Assert.Single(ErrorsOn(With(nameof(CreateCandidateRequest.Name), overLimit), "Name"));
    }
}
