using System.ComponentModel.DataAnnotations;
using Agriloco.Api.Data;
using Agriloco.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers;

[ApiController]
[Route("api/UnityEditorAuth")]
public sealed class UnityEditorAuthController(AgrilocoContext db, UnityEditorTokens tokens) : ControllerBase
{
    [HttpPost("login")]
    [Consumes("application/json")]
    [EnableRateLimiting("member-login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        if (!Request.IsHttps) return BadRequest(new { message = "HTTPS is required." });
        // Native clients omit Origin. Browser callers must be same-origin; no cookie is issued.
        if (Request.Headers.ContainsKey("Origin") &&
            (!Uri.TryCreate(Request.Headers.Origin.ToString(), UriKind.Absolute, out var origin) ||
             !string.Equals(origin.GetLeftPart(UriPartial.Authority),
                 $"{Request.Scheme}://{Request.Host}", StringComparison.OrdinalIgnoreCase)))
            return StatusCode(403);

        var member = await db.Members.Include(m => m.Farm)
            .FirstOrDefaultAsync(m => m.Username == request.Username.Trim());
        if (member == null || !member.IsActive || member.Farm?.IsActive != true ||
            !MemberPasswords.Verify(member, request.Password, out var upgrade))
            return Unauthorized(new { message = "Invalid username or password." });
        if (upgrade)
        {
            member.PasswordHash = MemberPasswords.Hash(member, request.Password);
            member.PasswordSalt = Array.Empty<byte>();
            await db.SaveChangesAsync();
        }
        return Ok(new {
            accessToken = tokens.Issue(member), tokenType = "Bearer",
            expiresIn = UnityEditorTokens.LifetimeSeconds, farmId = member.FarmId
        });
    }

    public sealed class LoginRequest
    {
        [Required, StringLength(256)] public string Username { get; set; } = "";
        [Required, StringLength(1024)] public string Password { get; set; } = "";
    }
}