using Candidates.Api.Data;
using Candidates.Api.Dtos;
using Candidates.Api.Errors;
using Candidates.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Candidates.Api.Services;

public class CandidateService(AppDbContext db)
{
    private const int MaxPageSize = 50;

    // Códigos do SQL Server: 2601 = violou índice único, 2627 = violou constraint única.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task<PagedResponse<CandidateListItem>> List(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        // Página ou tamanho fora do intervalo são ajustados, sem erro (contrato da API).
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

    public async Task<Candidate> GetById(int id, CancellationToken cancellationToken)
    {
        var candidate = await db.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return candidate ?? throw new CandidateNotFoundException();
    }

    public async Task<Candidate> Create(CreateCandidateRequest request, CancellationToken cancellationToken)
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
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
        {
            Number: UniqueIndexViolation or UniqueConstraintViolation
        })
        {
            throw new DuplicateEmailException();
        }

        return candidate;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
