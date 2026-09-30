using System.ComponentModel.DataAnnotations;
using Candidates.Api.Models;

namespace Candidates.Api.Dtos;

public class CreateCandidateRequest
{
    [Required(ErrorMessage = "Informe o nome completo.")]
    [MaxLength(CandidateLimits.Name, ErrorMessage = "Use no máximo {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [MaxLength(CandidateLimits.Email, ErrorMessage = "Use no máximo {1} caracteres.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Informe um e-mail válido, como nome@exemplo.com.")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(CandidateLimits.Phone, ErrorMessage = "Use no máximo {1} caracteres.")]
    [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Informe só números, com DDD. Ex.: 41998765432.")]
    public string? Phone { get; set; }

    [MaxLength(CandidateLimits.Position, ErrorMessage = "Use no máximo {1} caracteres.")]
    public string? Position { get; set; }

    [MaxLength(CandidateLimits.Summary, ErrorMessage = "Use no máximo {1} caracteres.")]
    public string? Summary { get; set; }
}
