using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Agriloco.Api.Data;
using Agriloco.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Pages.Farmer;

[EnableRateLimiting("member-login")]
public class LoginModel(AgrilocoContext db) : PageModel
{
    [BindProperty, Required, StringLength(256)] public string Username { get; set; } = "";
    [BindProperty, Required, StringLength(1024)] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var member = await db.Members.Include(m => m.Farm).FirstOrDefaultAsync(m => m.Username == Username.Trim());
        if (member == null || !member.IsActive || member.Farm?.IsActive != true ||
            !MemberPasswords.Verify(member, Password, out var upgrade))
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return Page();
        }
        if (upgrade)
        {
            member.PasswordHash = MemberPasswords.Hash(member, Password);
            member.PasswordSalt = Array.Empty<byte>();
            await db.SaveChangesAsync();
        }
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()), new Claim(ClaimTypes.Name, member.Username)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : $"/Farmer/Dashboard?farmId={member.FarmId}");
    }
}
