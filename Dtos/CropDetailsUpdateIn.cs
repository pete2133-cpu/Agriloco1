using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Dtos
{
    public class CropCreateIn
    {
        [Required]
        public int FarmId { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        public string? Variety { get; set; }

        public string? Availability { get; set; }
        public string? OfferingType { get; set; }
        public int? YearPlanted { get; set; }

        public string? Rootstock { get; set; }

        public string? Notes { get; set; }
    }

    public class CropSearchOut
    {
        public int Id { get; set; }

        public int FarmId { get; set; }

        public string Category { get; set; } = string.Empty;

        public string? Variety { get; set; }

        public string? Availability { get; set; }
        public string? OfferingType { get; set; }
        public int? YearPlanted { get; set; }

        public string? Rootstock { get; set; }

        public string? Notes { get; set; }
    }

    public class CropAvailabilityUpdateIn
    {
        public string? Availability { get; set; }
    }

    // NEW: update level-2 enrichment fields
    public class CropDetailsUpdateIn
    {
        public int? YearPlanted { get; set; }
        public string? Rootstock { get; set; }
        public string? Notes { get; set; }
        public string? OfferingType { get; set; }
    }
}