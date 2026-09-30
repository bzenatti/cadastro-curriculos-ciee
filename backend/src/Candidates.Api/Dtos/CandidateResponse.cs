using Candidates.Api.Models;

namespace Candidates.Api.Dtos;

public record CandidateResponse(
    int Id,
    string Name,
    string Email,
    string? Phone,
    string? Position,
    string? Summary,
    DateTime CreatedAt)
{
    public static CandidateResponse From(Candidate candidate) => new(
        candidate.Id,
        candidate.Name,
        candidate.Email,
        candidate.Phone,
        candidate.Position,
        candidate.Summary,
        candidate.CreatedAt);
}
