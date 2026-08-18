namespace RegulatoryReportingPortal.Models;

public enum ReportStandard
{
    FATCA,
    CRS
}

public enum ReportStatus
{
    Draft,
    Generated
}

public sealed record ClientRecord(
    Guid Id,
    string LegalName,
    string TaxIdentificationNumber,
    string CountryCode,
    DateOnly DateOfBirth,
    decimal AccountBalance,
    string Currency,
    DateTimeOffset CreatedAt);

public sealed record RegulatoryReport(
    Guid Id,
    ReportStandard Standard,
    int ReportingYear,
    ReportStatus Status,
    IReadOnlyList<Guid> ClientIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset? GeneratedAt);

public sealed record AuditEvent(
    Guid Id,
    string Action,
    string EntityType,
    string EntityId,
    string Detail,
    DateTimeOffset Timestamp);

public sealed record CreateClientRequest(
    string LegalName,
    string TaxIdentificationNumber,
    string CountryCode,
    DateOnly DateOfBirth,
    decimal AccountBalance,
    string Currency);

public sealed record CreateReportRequest(string Standard);

public sealed record LoginRequest(string Username, string Password);
