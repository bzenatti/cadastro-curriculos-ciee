using Candidates.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Candidates.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Candidate> Candidates => Set<Candidate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var candidate = modelBuilder.Entity<Candidate>();

        candidate.HasIndex(c => c.Email).IsUnique();

        // O SQL Server não guarda se a data é UTC; ao ler, marca como UTC para o JSON sair com "Z".
        candidate.Property(c => c.CreatedAt)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
