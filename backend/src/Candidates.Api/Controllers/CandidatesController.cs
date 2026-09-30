using Candidates.Api.Data;
using Candidates.Api.Dtos;
using Candidates.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Candidates.Api.Controllers;

[ApiController]
[Route("api/candidates")]
public class CandidatesController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CandidateResponse>> Create(
        CreateCandidateRequest request, CancellationToken cancellationToken)
    {
        var candidate = new Candidate
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = NullIfBlank(request.Phone),
            Position = NullIfBlank(request.Position),
            Summary = NullIfBlank(request.Summary),
        };

        db.Candidates.Add(candidate);
        await db.SaveChangesAsync(cancellationToken);

        return Created($"/api/candidates/{candidate.Id}", CandidateResponse.From(candidate));
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
