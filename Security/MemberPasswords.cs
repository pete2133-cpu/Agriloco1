using System.Security.Cryptography;
using System.Text;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Agriloco.Api.Security;

public static class MemberPasswords
{
    private const string Prefix = "ASPNET:";
    private static readonly PasswordHasher<Member> Hasher = new();

    public static byte[] Hash(Member member, string password) =>
        Encoding.UTF8.GetBytes(Prefix + Hasher.HashPassword(member, password));

    public static bool Verify(Member member, string password, out bool upgrade)
    {
        upgrade = false;
        var stored = Encoding.UTF8.GetString(member.PasswordHash);
        if (stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            try
            {
                var result = Hasher.VerifyHashedPassword(member, stored[Prefix.Length..], password);
                upgrade = result == PasswordVerificationResult.SuccessRehashNeeded;
                return result != PasswordVerificationResult.Failed;
            }
            catch (FormatException) { return false; }
        }
        // Previous registration stored UTF-8 password bytes with an empty salt.
        // Upgrade only after proving knowledge of that existing password.
        upgrade = member.PasswordSalt.Length == 0 && member.PasswordHash.Length > 0 &&
            CryptographicOperations.FixedTimeEquals(member.PasswordHash, Encoding.UTF8.GetBytes(password));
        return upgrade;
    }
}
