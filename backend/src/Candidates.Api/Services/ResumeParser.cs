using System.Text.RegularExpressions;
using Candidates.Api.Dtos;
using Candidates.Api.Models;

namespace Candidates.Api.Services;

// Texto -> campos. Função pura (sem PDF, banco ou disco): o que não for encontrado volta null.
public static partial class ResumeParser
{
    private const int NameSearchLines = 10;
    private const int MinNameWords = 2;
    private const int MaxNameWords = 6;

    // Conectivos que aparecem no meio de um nome ("Maria da Silva") e ficam em minúsculas.
    private static readonly HashSet<string> Connectors = new(StringComparer.OrdinalIgnoreCase)
    {
        "da", "de", "do", "das", "dos", "e",
    };

    // Palavras de títulos de seção ("Curriculum Vitae", "Dados Pessoais") que parecem um nome, mas não são.
    private static readonly HashSet<string> SectionWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "currículo", "curriculo", "curriculum", "vitae", "resumo", "objetivo",
        "experiência", "experiencia", "formação", "formacao", "acadêmica", "academica",
        "dados", "pessoais", "contato", "perfil", "profissional", "habilidades",
        "competências", "competencias", "idiomas", "cursos", "referências", "referencias",
        "endereço", "endereco", "telefone", "celular",
    };


    public static ResumeExtractionResponse Parse(string text)
    {
        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        return new ResumeExtractionResponse(FindName(lines), FindEmail(text), FindPhone(lines));
    }

    private static string? FindEmail(string text)
    {
        return EmailRegex().Matches(text)
            .Select(match => match.Value)
            .FirstOrDefault(email => email.Length <= CandidateLimits.Email);
    }


    private static string? FindPhone(List<string> lines)
    {
        foreach (var line in lines)
        {
            // CPF tem 11 dígitos, como um celular: a linha que cita CPF nunca vira telefone.
            if (line.Contains("CPF", StringComparison.OrdinalIgnoreCase)) continue;

            var match = PhoneRegex().Match(line);
            if (match.Success) return NormalizePhone(match.Value);
        }

        return null;
    }

    private static string NormalizePhone(string raw)
    {
        var digits = new string(raw.Where(char.IsAsciiDigit).ToArray());

        // Com +55 sobram 12 ou 13 dígitos: tira o 55 e ficam 10 (fixo) ou 11 (celular), o formato do cadastro.
        return digits.Length > 11 && digits.StartsWith("55", StringComparison.Ordinal) ? digits[2..] : digits;
    }

    private static string? FindName(List<string> lines)
    {
        foreach (var line in lines.Take(NameSearchLines))
        {
            var name = ReadName(line);
            if (name is not null) return name;
        }

        return null;
    }

    // Devolve o nome já formatado se a linha parece um nome; senão, null.
    private static string? ReadName(string line)
    {
        // Split sem separador quebra em qualquer espaço em branco (espaço, tab...) e descarta os vazios.
        var words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length is < MinNameWords or > MaxNameWords) return null;
        if (Connectors.Contains(words[0]) || Connectors.Contains(words[^1])) return null;
        if (!words.All(word => Connectors.Contains(word) || NameWordRegex().IsMatch(word))) return null;
        if (words.Any(SectionWords.Contains)) return null;

        var name = string.Join(' ', words.Select(FormatWord));
        return name.Length <= CandidateLimits.Name ? name : null;
    }

    // Só palavras escritas em CAIXA ALTA são convertidas; "McDonald" e "Da" ficam como vieram.
    private static string FormatWord(string word)
    {
        if (word != word.ToUpperInvariant()) return word;
        return Connectors.Contains(word) ? word.ToLowerInvariant() : TitleCase(word);
    }

    private static string TitleCase(string word)
    {
        var chars = new char[word.Length];
        var upper = true;
        for (var i = 0; i < word.Length; i++)
        {
            chars[i] = upper ? char.ToUpperInvariant(word[i]) : char.ToLowerInvariant(word[i]);
            // Depois de hífen ou apóstrofo começa outra parte do nome: "Ana-Maria", "D'Ávila".
            upper = word[i] is '-' or '\'' or '’';
        }

        return new string(chars);
    }

    // Letra maiúscula seguida de letras, hífen ou apóstrofo (reto ou tipográfico).
    [GeneratedRegex(@"^\p{Lu}[\p{L}'’-]*$")]
    private static partial Regex NameWordRegex();

    // texto@domínio.tld: letras, números e . _ % + - antes do @; letras, números, ponto e hífen depois.
    // É uma busca no meio do texto, por isso é mais estrita que a regex de validação do cadastro.
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    // [+55] DDD [9] 1234-5678, com ou sem parênteses, espaço, ponto ou hífen entre as partes.
    [GeneratedRegex(@"(?<![0-9])(?:\+?55[\s.-]?)?\(?[1-9]{2}\)?[\s.-]?(?:9[\s.-]?)?[0-9]{4}[\s.-]?[0-9]{4}(?![0-9])")]
    private static partial Regex PhoneRegex();
}
