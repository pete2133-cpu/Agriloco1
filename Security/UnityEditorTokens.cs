using System.Security.Claims;
using System.Security.Cryptography;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Security;

// A separate protection purpose prevents member cookies being used as editor tokens.
public sealed class UnityEditorTokens(IDataProtectionProvider protection, AgrilocoContext db)
{
    public const string Scheme = "Agriloco.UnityEditor";
    public const int LifetimeSeconds = 1800;
    private readonly TicketDataFormat format = new(protection.CreateProtector("Agriloco.UnityEditor.AccessToken.v1"));

    public string Issue(Member member)
    {
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
            new Claim("farmId", member.FarmId.ToString()),
            new Claim("passwordVersion", Convert.ToBase64String(SHA256.HashData(member.PasswordHash)))
        }, Scheme);
        return format.Protect(new AuthenticationTicket(new ClaimsPrincipal(identity),
            new AuthenticationProperties {
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(LifetimeSeconds)
            }, Scheme));
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string token)
    {
        if (token.Length > 8192) return null;
        var ticket = format.Unprotect(token);
        if (ticket == null || ticket.AuthenticationScheme != Scheme ||
            ticket.Properties.ExpiresUtc is not { } expires || expires <= DateTimeOffset.UtcNow ||
            !int.TryParse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ||
            !int.TryParse(ticket.Principal.FindFirstValue("farmId"), out var farmId)) return null;
        var member = await db.Members.AsNoTracking().Include(m => m.Farm).FirstOrDefaultAsync(m => m.Id == id);
        if (member == null || !member.IsActive || member.Farm?.IsActive != true || member.FarmId != farmId ||
            ticket.Principal.FindFirstValue("passwordVersion") != Convert.ToBase64String(SHA256.HashData(member.PasswordHash)))
            return null;
        return ticket.Principal;
    }
}