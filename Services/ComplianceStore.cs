using Microsoft.EntityFrameworkCore;
using RegulatoryReportingPortal.Data;
using RegulatoryReportingPortal.Models;

namespace RegulatoryReportingPortal.Services;

public sealed class ComplianceStore
{
    private readonly IDbContextFactory<ReportingDbContext> _contextFactory;

    public ComplianceStore(IDbContextFactory<ReportingDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        using var context = _contextFactory.CreateDbContext();
        context.Database.EnsureCreated();

        if (!context.Clients.Any())
        {
            context.Clients.AddRange(
                CreateEntity("Ana Solís", "CR-104560789", "CR", new DateOnly(1989, 4, 12), 24500m, "USD"),
                CreateEntity("Northwind Services Ltd", "US-92837465", "US", new DateOnly(1992, 8, 23), 81750m, "USD"));
            context.SaveChanges();
        }
    }

    public IReadOnlyList<ClientRecord> GetClients()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Clients.AsNoTracking().OrderBy(x => x.LegalName).AsEnumerable().Select(ToRecord).ToArray();
    }

    public ClientRecord? GetClient(Guid id)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Clients.AsNoTracking().FirstOrDefault(x => x.Id == id);
        return entity is null ? null : ToRecord(entity);
    }

    public IReadOnlyList<ClientRecord> GetClientsByIds(IEnumerable<Guid> ids)
    {
        var selected = ids.ToHashSet();
        using var context = _contextFactory.CreateDbContext();
        return context.Clients.AsNoTracking().Where(x => selected.Contains(x.Id)).AsEnumerable().Select(ToRecord).ToArray();
    }

    public ClientRecord AddClient(CreateClientRequest request)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = CreateEntity(request.LegalName.Trim(), request.TaxIdentificationNumber.Trim().ToUpperInvariant(),
            request.CountryCode.Trim().ToUpperInvariant(), request.DateOfBirth, request.AccountBalance,
            request.Currency.Trim().ToUpperInvariant());
        context.Clients.Add(entity);
        AddAudit(context, "CREATE", "Client", entity.Id.ToString(), $"Client {entity.LegalName} registered.");
        context.SaveChanges();
        return ToRecord(entity);
    }

    public IReadOnlyList<RegulatoryReport> GetReports()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Reports.AsNoTracking().Include(x => x.Clients).ToArray()
            .OrderByDescending(x => x.CreatedAt).Select(ToRecord).ToArray();
    }

    public RegulatoryReport? GetReport(Guid id)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Reports.AsNoTracking().Include(x => x.Clients).FirstOrDefault(x => x.Id == id);
        return entity is null ? null : ToRecord(entity);
    }

    public RegulatoryReport CreateReport(ReportStandard standard)
    {
        using var context = _contextFactory.CreateDbContext();
        var clientIds = context.Clients.AsNoTracking().Select(x => x.Id).ToArray();
        var entity = new ReportEntity
        {
            Id = Guid.NewGuid(), Standard = standard.ToString(), ReportingYear = DateTime.UtcNow.Year,
            Status = ReportStatus.Draft.ToString(), CreatedAt = DateTimeOffset.UtcNow,
            Clients = clientIds.Select(id => new ReportClientEntity { ClientId = id }).ToList()
        };
        context.Reports.Add(entity);
        AddAudit(context, "CREATE", "Report", entity.Id.ToString(), $"{standard} report created with {clientIds.Length} records.");
        context.SaveChanges();
        return ToRecord(entity);
    }

    public void MarkReportGenerated(Guid id)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Reports.FirstOrDefault(x => x.Id == id);
        if (entity is null) return;
        entity.Status = ReportStatus.Generated.ToString();
        entity.GeneratedAt = DateTimeOffset.UtcNow;
        AddAudit(context, "GENERATE_XML", "Report", id.ToString(), $"XML generated for {entity.Standard} report.");
        context.SaveChanges();
    }

    public IReadOnlyList<AuditEvent> GetAuditEvents()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.AuditEvents.AsNoTracking().ToArray().OrderByDescending(x => x.Timestamp)
            .Select(x => new AuditEvent(x.Id, x.Action, x.EntityType, x.EntityId, x.Detail, x.Timestamp)).ToArray();
    }

    private static ClientEntity CreateEntity(string name, string tin, string country, DateOnly birthDate, decimal balance, string currency) =>
        new() { Id = Guid.NewGuid(), LegalName = name, TaxIdentificationNumber = tin, CountryCode = country,
            DateOfBirth = birthDate, AccountBalance = balance, Currency = currency, CreatedAt = DateTimeOffset.UtcNow };

    private static ClientRecord ToRecord(ClientEntity entity) =>
        new(entity.Id, entity.LegalName, entity.TaxIdentificationNumber, entity.CountryCode,
            entity.DateOfBirth, entity.AccountBalance, entity.Currency, entity.CreatedAt);

    private static RegulatoryReport ToRecord(ReportEntity entity) =>
        new(entity.Id, Enum.Parse<ReportStandard>(entity.Standard), entity.ReportingYear,
            Enum.Parse<ReportStatus>(entity.Status), entity.Clients.Select(x => x.ClientId).ToArray(), entity.CreatedAt, entity.GeneratedAt);

    private static void AddAudit(ReportingDbContext context, string action, string entityType, string entityId, string detail) =>
        context.AuditEvents.Add(new AuditEventEntity { Id = Guid.NewGuid(), Action = action, EntityType = entityType,
            EntityId = entityId, Detail = detail, Timestamp = DateTimeOffset.UtcNow });
}
