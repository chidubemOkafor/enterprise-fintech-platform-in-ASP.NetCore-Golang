using Microsoft.EntityFrameworkCore;
using wallet.Models;
using MassTransit; 

namespace wallet.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<WalletModel> Wallets { get; set; } = null!;
    public DbSet<LedgerModel> Ledgers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WalletModel>(w =>
        {
            w.Property(x => x.CachedBalance).HasPrecision(18, 2);
            w.HasIndex(x => x.AccountNumber).IsUnique();
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}