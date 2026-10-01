using Candidates.Api.Dtos;
using Candidates.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Candidates.Api.Controllers;

[ApiController]
[Route("api/candidates")]
public class CandidatesController(CandidateService service) : ControllerBase
{
    private const int DefaultPageSize = 10;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CandidateListItem>>> List(
        int page = 1, int pageSize = DefaultPageSize, CancellationToken cancellationToken = default)
    {
        return await service.List(page, pageSize, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CandidateResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var candidate = await service.GetById(id, cancellationToken);
        return CandidateResponse.From(candidate);
    }

    [HttpPost]
    public async Task<ActionResult<CandidateResponse>> Create(
        CreateCandidateRequest request, CancellationToken cancellationToken)
    {
        var candidate = await service.Create(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = candidate.Id }, CandidateResponse.From(candidate));
    }
}
