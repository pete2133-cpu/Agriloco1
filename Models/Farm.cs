namespace Agriloco.Api.Models
{
    public class Farm
    {
        public int Id { get; set; }
        public string AgrilocoId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? RegionCode { get; set; }
        public string ContactMethod1 { get; set; } = string.Empty;
        public string FruitCategory1 { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}