namespace Candidates.Api.Dtos;

public record CandidateListItem(int Id, string Name, string Email, string? Position, DateTime CreatedAt);