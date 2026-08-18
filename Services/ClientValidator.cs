using RegulatoryReportingPortal.Models;

namespace RegulatoryReportingPortal.Services;

public static class ClientValidator
{
    private static readonly HashSet<string> SupportedCurrencies =
        new(StringComparer.OrdinalIgnoreCase) { "CRC", "USD", "EUR" };

    public static Dictionary<string, string[]> Validate(CreateClientRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.LegalName) || request.LegalName.Length < 3)
            errors["legalName"] = ["Legal name must contain at least 3 characters."];

        if (string.IsNullOrWhiteSpace(request.TaxIdentificationNumber) ||
            request.TaxIdentificationNumber.Length is < 6 or > 20)
            errors["taxIdentificationNumber"] = ["Tax identification number must contain 6 to 20 characters."];

        if (request.CountryCode?.Length != 2 || !request.CountryCode.All(char.IsLetter))
            errors["countryCode"] = ["Country code must use the two-letter ISO format."];

        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
            errors["dateOfBirth"] = ["The client must be at least 18 years old."];

        if (request.AccountBalance < 0)
            errors["accountBalance"] = ["Account balance cannot be negative."];

        if (!SupportedCurrencies.Contains(request.Currency ?? string.Empty))
            errors["currency"] = ["Currency must be CRC, USD, or EUR."];

        return errors;
    }
}
