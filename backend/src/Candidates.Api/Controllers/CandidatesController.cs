using Candidates.Api.Data;
using Candidates.Api.Dtos;
using Candidates.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Candidates.Api.Controllers;

[ApiController]
[Route("api/candidates")]
public class CandidatesController(AppDbContext db) : ControllerBase
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CandidateListItem>>> List(
        int page = 1, int pageSize = DefaultPageSize, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var totalCount = await db.Candidates.CountAsync(cancellationToken);
        var lastPage = Math.Max(1, (totalCount + pageSize - 1) / pageSize);
        page = Math.Clamp(page, 1, lastPage);

        var items = await db.Candidates
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CandidateListItem(c.Id, c.Name, c.Email, c.Position, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<CandidateListItem>(items, page, pageSize, totalCount);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CandidateResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var candidate = await db.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (candidate is null)
        {
            return Problem(detail: "Candidato não encontrado.", statusCode: StatusCodes.Status404NotFound);
        }

        return CandidateResponse.From(candidate);
    }

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

        return CreatedAtAction(nameof(GetById), new { id = candidate.Id }, CandidateResponse.From(candidate));
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
