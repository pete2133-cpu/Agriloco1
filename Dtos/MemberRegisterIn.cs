namespace Agriloco.Api.Dtos
{
    public class MemberRegisterIn
    {
        // Farm fields (creates the farm)
        public string FarmName { get; set; } = "";
        public string Address { get; set; } = "";

        // Member fields
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? AltEmail { get; set; }

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}