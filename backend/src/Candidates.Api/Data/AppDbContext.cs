using Candidates.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Candidates.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Candidate> Candidates => Set<Candidate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Candidate>()
            .HasIndex(c => c.Email)
            .IsUnique();
    }
}
