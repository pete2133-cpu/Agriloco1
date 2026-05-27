namespace Agriloco.Api.Dtos
{
    public class MemberRegisterIn
    {
        public string FarmName { get; set; } = "";
        public string Address { get; set; } = "";

        // NEW: optional geo at signup (manual now; later can be filled by anything)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? AltEmail { get; set; }

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}