using System;

namespace Agriloco.Api.Dtos
{
    public class CropSearchOut
    {
        // Keep Id as the true field
        public int Id { get; set; }

        // Alias so older pages that expect CropId keep working
        public int CropId => Id;

        public int FarmId { get; set; }
        public string FarmName { get; set; } = "";
        public string RegionCode { get; set; } = "";

        public string Category { get; set; } = "";
        public string? Variety { get; set; }
        public string? Availability { get; set; }

        public int? YearPlanted { get; set; }
        public string? Rootstock { get; set; }
        public string? Notes { get; set; }

        public int SearchLevel { get; set; } // 1 or 2
        public DateTime CreatedAt { get; set; }
    }
}