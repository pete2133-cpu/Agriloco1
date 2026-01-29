namespace Agriloco.Api.Dtos
{
    public class MemberRegisterOut
    {
        public int MemberId { get; set; }
        public int FarmId { get; set; }
        public string FarmName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
    }
}