using System.ComponentModel.DataAnnotations;

namespace Candidates.Api.Models;

public class Candidate
{
    public int Id { get; set; }

    [MaxLength(CandidateLimits.FullName)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(CandidateLimits.Email)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(CandidateLimits.Phone)]
    public string? Phone { get; set; }

    [MaxLength(CandidateLimits.Role)]
    public string? Role { get; set; }

    [MaxLength(CandidateLimits.Summary)]
    public string? Summary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
