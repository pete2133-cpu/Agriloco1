using System;

namespace Agriloco.Api.Models
{
    public class Member
    {
        public int Id { get; set; }

        // The "farmer id" you described is FarmId (Farm.Id)
        public int FarmId { get; set; }
        public Farm? Farm { get; set; }

        public string Username { get; set; } = "";
        public string Email { get; set; } = "";

        public string? Phone { get; set; }
        public string? AltEmail { get; set; }

        // Store password hash + salt (NOT plain password)
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}