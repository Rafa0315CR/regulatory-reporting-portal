using System.Xml.Linq;
using RegulatoryReportingPortal.Models;

namespace RegulatoryReportingPortal.Services;

public sealed class ReportXmlService
{
    public string Generate(RegulatoryReport report, IReadOnlyList<ClientRecord> clients)
    {
        XNamespace ns = "urn:regulatory-reporting:internal:v1";

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "RegulatoryReport",
                new XAttribute("standard", report.Standard),
                new XAttribute("reportingYear", report.ReportingYear),
                new XAttribute("generatedAt", DateTimeOffset.UtcNow.ToString("O")),
                new XElement(ns + "ReportId", report.Id),
                new XElement(ns + "Records",
                    new XAttribute("count", clients.Count),
                    clients.Select(client => new XElement(ns + "Client",
                        new XElement(ns + "ClientId", client.Id),
                        new XElement(ns + "LegalName", client.LegalName),
                        new XElement(ns + "TaxIdentificationNumber", client.TaxIdentificationNumber),
                        new XElement(ns + "CountryCode", client.CountryCode),
                        new XElement(ns + "DateOfBirth", client.DateOfBirth.ToString("yyyy-MM-dd")),
                        new XElement(ns + "Account",
                            new XAttribute("currency", client.Currency),
                            client.AccountBalance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)))))));

        return document.ToString();
    }
}
