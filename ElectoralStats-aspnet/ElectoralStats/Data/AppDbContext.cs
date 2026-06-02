using ElectoralStats.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectoralStats.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }
    public DbSet<VoterRecord> Voters => Set<VoterRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<VoterRecord>().HasIndex(v => v.RegistrationDate);
        b.Entity<VoterRecord>().HasIndex(v => v.Commune);
        b.Entity<VoterRecord>().HasIndex(v => v.Circumscription);
        b.Entity<VoterRecord>().HasIndex(v => v.Kind);
    }
}
