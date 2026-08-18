using Microsoft.AspNetCore.Identity;

namespace RegulatoryReportingPortal.Services;

public sealed record LocalUser(string Username, string Role, string PasswordHash);

public sealed class LocalUserService
{
    private readonly PasswordHasher<LocalUser> _hasher = new();
    private readonly LocalUser _analyst;

    public LocalUserService()
    {
        var initial = new LocalUser("analyst", "Analyst", string.Empty);
        _analyst = initial with { PasswordHash = _hasher.HashPassword(initial, "Analyst2026!") };
    }

    public LocalUser? Validate(string username, string password)
    {
        if (!string.Equals(username, _analyst.Username, StringComparison.OrdinalIgnoreCase))
            return null;

        var result = _hasher.VerifyHashedPassword(_analyst, _analyst.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded
            ? _analyst
            : null;
    }
}
