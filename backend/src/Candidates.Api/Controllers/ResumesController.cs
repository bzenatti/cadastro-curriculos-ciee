using Candidates.Api.Dtos;
using Candidates.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidates.Api.Controllers;

[ApiController]
[Route("api/resumes")]
public class ResumesController : ControllerBase
{
    // Mantenha igual ao client_max_body_size do nginx (frontend/nginx.conf). Acima disso o envio é recusado sem ser lido.
    private const long MaxRequestBytes = PdfFileValidator.MaxSizeBytes + 1024 * 1024;

    // Só sugere nome, e-mail e telefone: não grava nada, e o arquivo não é guardado.
    // Sem parâmetros de propósito: com um IFormFile, o MVC leria o formulário antes da action e responderia o 400 dele, em inglês.
    [HttpPost("parse")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<ActionResult<ResumeExtractionResponse>> Parse()
    {
        var form = Request.HasFormContentType ? await Request.ReadFormAsync(HttpContext.RequestAborted) : null;
        var file = form?.Files.GetFile("file");
        PdfFileValidator.Validate(file);

        using var stream = file.OpenReadStream();
        var text = PdfTextExtractor.Extract(stream);

        return ResumeParser.Parse(text);
    }
}
