CREATE DATABASE RegulatoryReporting;
GO

USE RegulatoryReporting;
GO

CREATE TABLE Clients (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    LegalName NVARCHAR(160) NOT NULL,
    TaxIdentificationNumber NVARCHAR(20) NOT NULL UNIQUE,
    CountryCode CHAR(2) NOT NULL,
    DateOfBirth DATE NOT NULL,
    AccountBalance DECIMAL(18, 2) NOT NULL CHECK (AccountBalance >= 0),
    Currency CHAR(3) NOT NULL CHECK (Currency IN ('CRC', 'USD', 'EUR')),
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

CREATE TABLE RegulatoryReports (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Standard VARCHAR(5) NOT NULL CHECK (Standard IN ('FATCA', 'CRS')),
    ReportingYear INT NOT NULL,
    Status VARCHAR(12) NOT NULL CHECK (Status IN ('Draft', 'Generated')),
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    GeneratedAt DATETIMEOFFSET NULL
);

CREATE TABLE ReportClients (
    ReportId UNIQUEIDENTIFIER NOT NULL REFERENCES RegulatoryReports(Id),
    ClientId UNIQUEIDENTIFIER NOT NULL REFERENCES Clients(Id),
    CONSTRAINT PK_ReportClients PRIMARY KEY (ReportId, ClientId)
);

CREATE TABLE AuditEvents (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Action NVARCHAR(40) NOT NULL,
    EntityType NVARCHAR(60) NOT NULL,
    EntityId NVARCHAR(80) NOT NULL,
    Detail NVARCHAR(500) NOT NULL,
    Timestamp DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

CREATE INDEX IX_AuditEvents_Timestamp ON AuditEvents (Timestamp DESC);
CREATE INDEX IX_Clients_CountryCode ON Clients (CountryCode);
