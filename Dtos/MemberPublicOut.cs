using System;

namespace Agriloco.Api.Dtos
{
    public class MemberPublicOut
    {
        // Backward/compat fields used by some Razor pages
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;        // will mirror FarmName
        public string RegionCode { get; set; } = string.Empty;  // from Farm

        // Membership fields
        public int MemberId { get; set; }
        public int FarmId { get; set; }
        public string FarmName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? AltEmail { get; set; }
        public string? Phone { get; set; }
        public string Username { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}