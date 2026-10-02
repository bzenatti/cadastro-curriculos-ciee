using System.ComponentModel.DataAnnotations;
using Candidates.Api.Dtos;
using Candidates.Api.Models;
using Candidates.Api.Services;

namespace Candidates.Api.Tests.Services;

public class ResumeParserTests
{
    private static readonly ResumeExtractionResponse Nothing = new(null, null, null);

    private const string FullResume = """
        CURRICULUM VITAE
        MARIA DA SILVA
        Rua XV de Novembro, 100 - Curitiba/PR - CEP 80010-000
        Telefone: (41) 99876-5432 | E-mail: maria.silva@example.com
        CPF: 123.456.789-09

        EXPERIÊNCIA PROFISSIONAL
        Desenvolvedora - Empresa Exemplo (2019 – 2023)
        """;

    // Linhas que não parecem nome ("linha 1", "linha 2"...), para encher o início do texto.
    private static string Lines(int count) =>
        string.Join("\n", Enumerable.Range(1, count).Select(i => $"linha {i}"));

    // ---------- Texto sem conteúdo ----------

    [Theory(DisplayName = "Texto vazio ou só com espaços não encontra nada")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\r\n  \n")]
    public void Parse_BlankText_FindsNothing(string text)
    {
        Assert.Equal(Nothing, ResumeParser.Parse(text));
    }

    [Fact(DisplayName = "Texto sem nenhum dado de contato não encontra nada")]
    public void Parse_TextWithoutData_FindsNothing()
    {
        const string text = "Experiência com React e .NET.\nSem dados de contato.";

        Assert.Equal(Nothing, ResumeParser.Parse(text));
    }

    [Fact(DisplayName = "Símbolos soltos não geram erro nem dados")]
    public void Parse_Garbage_FindsNothing()
    {
        Assert.Equal(Nothing, ResumeParser.Parse("@@@ ((( ))) --- ??? ###"));
    }

    // ---------- E-mail ----------

    [Theory(DisplayName = "Encontra o e-mail no meio do texto, sem a pontuação em volta")]
    [InlineData("maria@example.com", "maria@example.com")]
    [InlineData("E-mail: maria.silva@example.com", "maria.silva@example.com")]
    [InlineData("E-mail:maria@example.org, telefone", "maria@example.org")]
    [InlineData("Contato (maria@example.net).", "maria@example.net")]
    [InlineData("joao_pedro+vaga@empresa.example.com", "joao_pedro+vaga@empresa.example.com")]
    [InlineData("Maria@Example.COM", "Maria@Example.COM")]
    [InlineData("maria@example.com e outro@example.com", "maria@example.com")]
    public void Parse_FindsEmail(string text, string expected)
    {
        Assert.Equal(expected, ResumeParser.Parse(text).Email);
    }

    [Theory(DisplayName = "Texto que não é e-mail devolve null")]
    [InlineData("Sem contato nesta linha")]
    [InlineData("Siga @mariasilva no Instagram")]
    [InlineData("maria@example")]
    [InlineData("maria @ example.com")]
    public void Parse_NotAnEmail_ReturnsNull(string text)
    {
        Assert.Null(ResumeParser.Parse(text).Email);
    }

    [Fact(DisplayName = "E-mail maior que o limite do cadastro devolve null")]
    public void Parse_EmailLongerThanLimit_ReturnsNull()
    {
        var text = new string('a', CandidateLimits.Email) + "@example.com";

        Assert.Null(ResumeParser.Parse(text).Email);
    }

    // ---------- Telefone ----------

    [Theory(DisplayName = "Encontra o telefone e devolve só os dígitos, com DDD e sem +55")]
    [InlineData("(41) 99876-5432", "41998765432")]
    [InlineData("41 99876-5432", "41998765432")]
    [InlineData("41998765432", "41998765432")]
    [InlineData("41.99876.5432", "41998765432")]
    [InlineData("(41)99876-5432", "41998765432")]
    [InlineData("+55 41 99876-5432", "41998765432")]
    [InlineData("+55 (41) 9 9876-5432", "41998765432")]
    [InlineData("5541998765432", "41998765432")]
    [InlineData("(41) 3333-4444", "4133334444")]
    [InlineData("41 3333 4444", "4133334444")]
    [InlineData("+55 41 3333-4444", "4133334444")]
    [InlineData("Tel.: (41) 99876-5432", "41998765432")]
    [InlineData("Cel/WhatsApp: (11) 98765-4321", "11987654321")]
    [InlineData("(55) 99999-1234", "55999991234")]
    [InlineData("RG 12.345.678-9 Tel (41) 99876-5432", "41998765432")]
    public void Parse_FindsPhone(string text, string expected)
    {
        Assert.Equal(expected, ResumeParser.Parse(text).Phone);
    }

    [Theory(DisplayName = "CPF, CNPJ, CEP, datas e números sem DDD não viram telefone")]
    [InlineData("CPF: 123.456.789-09")]
    [InlineData("CPF: 41998765432")]
    [InlineData("123.456.789-09")]
    [InlineData("CNPJ: 12.345.678/0001-90")]
    [InlineData("CEP: 80010-000")]
    [InlineData("CEP 80010000")]
    [InlineData("2019 – 2023")]
    [InlineData("2019-2023")]
    [InlineData("01/2019 - 12/2023")]
    [InlineData("2015 - 2019 | 2019 - 2023")]
    [InlineData("Telefone: 99876-5432")]
    [InlineData("Matrícula 123456789012345")]
    public void Parse_NotAPhone_ReturnsNull(string text)
    {
        Assert.Null(ResumeParser.Parse(text).Phone);
    }

    [Fact(DisplayName = "Fica com o primeiro telefone do texto")]
    public void Parse_TwoPhones_UsesTheFirst()
    {
        Assert.Equal("41998765432", ResumeParser.Parse("(41) 99876-5432 / (41) 3333-4444").Phone);
    }

    [Fact(DisplayName = "A linha do CPF é pulada, mas a busca continua nas outras linhas")]
    public void Parse_CpfLineThenPhoneLine_FindsThePhone()
    {
        Assert.Equal("2133334444", ResumeParser.Parse("CPF: 41998765432\nTel: (21) 3333-4444").Phone);
    }

    // ---------- Nome ----------

    [Theory(DisplayName = "Reconhece o nome e o devolve no formato 'Maria da Silva'")]
    [InlineData("Maria da Silva", "Maria da Silva")]
    [InlineData("MARIA DA SILVA", "Maria da Silva")]
    [InlineData("Maria DA SILVA", "Maria da Silva")]
    [InlineData("JOÃO CARLOS DE OLIVEIRA", "João Carlos de Oliveira")]
    [InlineData("ANA-MARIA DE SOUZA", "Ana-Maria de Souza")]
    [InlineData("JOÃO D'ÁVILA", "João D'Ávila")]
    [InlineData("MARIA DA SILVA E SOUZA", "Maria da Silva e Souza")]
    [InlineData("Maria Da Silva", "Maria Da Silva")]
    [InlineData("João McDonald", "João McDonald")]
    [InlineData("Ana Maria da Silva dos Santos", "Ana Maria da Silva dos Santos")]
    [InlineData("  Maria   da   Silva  ", "Maria da Silva")]
    [InlineData("Álvaro Souza", "Álvaro Souza")]
    [InlineData("Ana Lima", "Ana Lima")]
    public void Parse_FindsName(string text, string expected)
    {
        Assert.Equal(expected, ResumeParser.Parse(text).Name);
    }

    [Theory(DisplayName = "Linha que não parece nome devolve null")]
    [InlineData("Maria")]
    [InlineData("maria silva")]
    [InlineData("Maria da")]
    [InlineData("da Silva")]
    [InlineData("Rua XV de Novembro, 100")]
    [InlineData("maria.silva@example.com")]
    [InlineData("(41) 99876-5432")]
    [InlineData("Curriculum Vitae")]
    [InlineData("Resumo Profissional")]
    [InlineData("Dados Pessoais")]
    [InlineData("Formação Acadêmica")]
    [InlineData("Maria da Silva dos Santos de Oliveira Souza")]
    [InlineData("Ana Maria da Silva dos Santos Souza")]
    [InlineData("Maria da Silva - Desenvolvedora")]
    public void Parse_NotAName_ReturnsNull(string text)
    {
        Assert.Null(ResumeParser.Parse(text).Name);
    }

    [Fact(DisplayName = "Pula o cabeçalho do currículo e pega o nome da linha seguinte")]
    public void Parse_HeaderThenName_SkipsTheHeader()
    {
        Assert.Equal("Maria da Silva", ResumeParser.Parse("CURRICULUM VITAE\nMARIA DA SILVA").Name);
    }

    [Fact(DisplayName = "Fica com a primeira linha que parece nome")]
    public void Parse_TwoNameLikeLines_UsesTheFirst()
    {
        Assert.Equal("Maria da Silva", ResumeParser.Parse("Maria da Silva\nJoão Pereira").Name);
    }

    [Fact(DisplayName = "Aceita linhas em branco e quebras de linha do Windows")]
    public void Parse_WindowsLineEndings_Works()
    {
        const string text = "\r\n\r\n  Maria da Silva \r\n(41) 99876-5432\r\n";

        Assert.Equal(new ResumeExtractionResponse("Maria da Silva", null, "41998765432"), ResumeParser.Parse(text));
    }

    [Fact(DisplayName = "Procura o nome nas 10 primeiras linhas com texto")]
    public void Parse_NameOnTenthLine_IsFound()
    {
        Assert.Equal("Maria da Silva", ResumeParser.Parse(Lines(9) + "\nMaria da Silva").Name);
    }

    [Fact(DisplayName = "Não procura o nome depois da décima linha")]
    public void Parse_NameOnEleventhLine_IsIgnored()
    {
        Assert.Null(ResumeParser.Parse(Lines(10) + "\nMaria da Silva").Name);
    }

    [Fact(DisplayName = "Recusa nome maior que o limite do cadastro")]
    public void Parse_NameLongerThanLimit_ReturnsNull()
    {
        var word = "M" + new string('a', CandidateLimits.Name);

        Assert.Null(ResumeParser.Parse($"{word} {word}").Name);
    }

    [Fact(DisplayName = "LIMITE CONHECIDO: um cargo antes do nome é tomado como nome")]
    public void Parse_JobTitleBeforeName_IsTakenAsName()
    {
        Assert.Equal("Desenvolvedora de Sistemas", ResumeParser.Parse("Desenvolvedora de Sistemas\nMaria da Silva").Name);
    }

    // ---------- Currículo completo ----------

    [Fact(DisplayName = "Currículo completo com armadilhas (CPF, CEP, datas) devolve os 3 campos")]
    public void Parse_FullResume_FindsAllThree()
    {
        var expected = new ResumeExtractionResponse("Maria da Silva", "maria.silva@example.com", "41998765432");

        Assert.Equal(expected, ResumeParser.Parse(FullResume));
    }

    [Fact(DisplayName = "O que o parser devolve passa nas validações do cadastro")]
    public void Parse_Result_PassesRegistrationValidation()
    {
        var found = ResumeParser.Parse(FullResume);
        var request = new CreateCandidateRequest { Name = found.Name!, Email = found.Email!, Phone = found.Phone };
        var errors = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request, new ValidationContext(request), errors, validateAllProperties: true);

        Assert.True(isValid, string.Join("; ", errors.Select(e => e.ErrorMessage)));
    }

    [Fact(DisplayName = "Pula o e-mail maior que o limite e fica com o próximo")]
    public void Parse_FirstEmailTooLong_UsesTheNext()
    {
        var tooLong = new string('a', CandidateLimits.Email) + "@example.com";

        Assert.Equal("maria@example.com", ResumeParser.Parse($"{tooLong} maria@example.com").Email);
    }
}
