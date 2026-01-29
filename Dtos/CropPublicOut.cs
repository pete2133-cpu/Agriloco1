namespace Agriloco.Api.Dtos
{
    // Used for public listing + farm crop lists + dropdowns
    public class CropPublicOut
    {
        public int Id { get; set; }

        public int FarmId { get; set; }

        public string Category { get; set; } = string.Empty;

        public string? Variety { get; set; }

        public string? Availability { get; set; }

        // Optional "level 2" fields (keep nullable so old records still work)
        public int? YearPlanted { get; set; }

        public string? Rootstock { get; set; }

        public string? Notes { get; set; }
    }
}