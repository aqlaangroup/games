using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Platform.Domain;

namespace Platform.Api.Data;

public class PlatformDb(DbContextOptions<PlatformDb> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerIdentity> Identities => Set<PlayerIdentity>();
    public DbSet<PlayerSession> Sessions => Set<PlayerSession>();
    public DbSet<PlayerSave> Saves => Set<PlayerSave>();
    public DbSet<SaveFlag> SaveFlags => Set<SaveFlag>();
    public DbSet<WalletBalance> WalletBalances => Set<WalletBalance>();
    public DbSet<WalletEntry> WalletEntries => Set<WalletEntry>();
    public DbSet<RewardClaim> RewardClaims => Set<RewardClaim>();
    public DbSet<GemSpend> GemSpends => Set<GemSpend>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<ConfigVersion> Configs => Set<ConfigVersion>();
    public DbSet<LiveEvent> Events => Set<LiveEvent>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminRole> AdminRoles => Set<AdminRole>();
    public DbSet<AuditEntry> Audit => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Game>(e =>
        {
            e.HasIndex(g => g.Slug).IsUnique();
        });

        model.Entity<Player>(e =>
        {
            e.HasIndex(p => p.GameId);
            e.HasIndex(p => p.DeleteBy);
        });

        model.Entity<PlayerIdentity>(e =>
        {
            e.HasIndex(i => new { i.GameId, i.Provider, i.Subject }).IsUnique();
            e.HasIndex(i => new { i.PlayerId, i.Provider }).IsUnique();
        });

        model.Entity<PlayerSession>(e =>
        {
            e.HasIndex(s => s.TokenHash).IsUnique();
            e.HasIndex(s => s.PlayerId);
            e.HasIndex(s => s.FamilyId);
        });

        model.Entity<PlayerSave>(e =>
        {
            e.HasKey(s => s.PlayerId);
            e.HasIndex(s => s.GameId);
            e.Property(s => s.Json).HasColumnType("jsonb");
            // Two uploads racing on the same version: only one wins.
            e.Property(s => s.Version).IsConcurrencyToken();
        });

        model.Entity<SaveFlag>(e => e.HasIndex(f => new { f.GameId, f.PlayerId }));

        model.Entity<WalletBalance>(e =>
        {
            e.HasKey(b => new { b.PlayerId, b.Currency });
            e.HasIndex(b => b.GameId);
            e.ToTable(t => t.HasCheckConstraint("CK_WalletBalances_NotNegative", "\"Amount\" >= 0"));
        });

        model.Entity<WalletEntry>(e => e.HasIndex(w => new { w.GameId, w.PlayerId }));

        // Each reward grants once, and each spend request spends once.
        model.Entity<RewardClaim>(e => e.HasIndex(r => new { r.PlayerId, r.RewardId }).IsUnique());
        model.Entity<GemSpend>(e =>
        {
            e.HasIndex(s => new { s.PlayerId, s.RequestId }).IsUnique();
            e.HasIndex(s => new { s.PlayerId, s.Item });
            e.Property(s => s.GrantJson).HasColumnType("jsonb");
        });

        model.Entity<Product>(e =>
        {
            e.HasIndex(p => new { p.GameId, p.Sku }).IsUnique();
            e.Property(p => p.Grants).HasColumnType("jsonb").HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new(),
                new ValueComparer<Dictionary<string, string>>(
                    (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
                    v => v.Aggregate(0, (h, p) => HashCode.Combine(h, p.Key, p.Value)),
                    v => new Dictionary<string, string>(v)));
        });

        model.Entity<Purchase>(e =>
        {
            // The same store transaction can only grant once.
            e.HasIndex(p => new { p.Store, p.StoreTransactionId }).IsUnique();
            e.HasIndex(p => new { p.GameId, p.PlayerId });
            e.Property(p => p.Price).HasPrecision(12, 4);
        });

        model.Entity<SupportTicket>(e => e.HasIndex(t => new { t.GameId, t.Status }));

        model.Entity<ConfigVersion>(e => e.HasIndex(c => new { c.GameId, c.Version }).IsUnique());
        model.Entity<LiveEvent>(e => e.HasIndex(l => l.GameId));

        model.Entity<AdminUser>(e => e.HasIndex(a => a.Email).IsUnique());
        model.Entity<AdminRole>(e => e.HasKey(r => new { r.AdminUserId, r.GameId }));
        model.Entity<AuditEntry>(e => e.HasIndex(a => new { a.GameId, a.CreatedAt }));
    }
}

/// <summary>Lets `dotnet ef` build the model without starting the app.</summary>
public class PlatformDbDesignFactory : IDesignTimeDbContextFactory<PlatformDb>
{
    public PlatformDb CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PlatformDb>()
            .UseNpgsql("Host=localhost;Database=aqlaan_platform;Username=postgres")
            .Options);
}
