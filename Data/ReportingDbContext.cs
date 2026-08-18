using Microsoft.EntityFrameworkCore;

namespace RegulatoryReportingPortal.Data;

public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<ReportEntity> Reports => Set<ReportEntity>();
    public DbSet<ReportClientEntity> ReportClients => Set<ReportClientEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientEntity>(entity =>
        {
            entity.ToTable("Clients"); entity.HasKey(x => x.Id);
            entity.Property(x => x.LegalName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.TaxIdentificationNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.TaxIdentificationNumber).IsUnique();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsFixedLength().IsRequired();
            entity.Property(x => x.AccountBalance).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        });
        modelBuilder.Entity<ReportEntity>(entity =>
        {
            entity.ToTable("RegulatoryReports"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Standard).HasMaxLength(5).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(12).IsRequired();
        });
        modelBuilder.Entity<ReportClientEntity>(entity =>
        {
            entity.ToTable("ReportClients"); entity.HasKey(x => new { x.ReportId, x.ClientId });
            entity.HasOne(x => x.Report).WithMany(x => x.Clients).HasForeignKey(x => x.ReportId);
            entity.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId);
        });
        modelBuilder.Entity<AuditEventEntity>(entity =>
        {
            entity.ToTable("AuditEvents"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(40).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(60).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.Timestamp);
        });
    }
}

public sealed class ClientEntity
{
    public Guid Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string TaxIdentificationNumber { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public decimal AccountBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ReportEntity
{
    public Guid Id { get; set; }
    public string Standard { get; set; } = string.Empty;
    public int ReportingYear { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? GeneratedAt { get; set; }
    public List<ReportClientEntity> Clients { get; set; } = [];
}

public sealed class ReportClientEntity
{
    public Guid ReportId { get; set; }
    public ReportEntity Report { get; set; } = null!;
    public Guid ClientId { get; set; }
    public ClientEntity Client { get; set; } = null!;
}

public sealed class AuditEventEntity
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
