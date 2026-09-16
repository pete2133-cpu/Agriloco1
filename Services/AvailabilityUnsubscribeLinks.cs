using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace Agriloco.Api.Services;

// Reuses the application's persisted Data Protection keys; no subscriber data in URLs.
public sealed class AvailabilityUnsubscribeLinks(IDataProtectionProvider protection, IConfiguration configuration)
{
    private readonly IDataProtector protector = protection.CreateProtector("Agriloco.Availability.Unsubscribe.v1");
    public string Create(int farmId, string email) =>
        configuration["Application:PublicBaseUrl"]!.TrimEnd('/') +
        "/api/AvailabilitySubscriptions/unsubscribe?token=" +
        Uri.EscapeDataString(protector.Protect(JsonSerializer.Serialize(new Recipient(farmId, email))));

    public Recipient? Read(string token)
    {
        try { return JsonSerializer.Deserialize<Recipient>(protector.Unprotect(token)); }
        catch { return null; }
    }
    public sealed record Recipient(int FarmId, string Email);
}
