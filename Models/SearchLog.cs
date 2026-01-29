using System;

namespace Agriloco.Api.Models
{
    public class SearchLog
    {
        public int Id { get; set; }

        // Example: "CA-ON" (user-provided region bucket)
        public string RegionCode { get; set; } = "";

        // Normalized search term, e.g. "apple"
        public string Term { get; set; } = "";

        // Optional structured filters the user used
        public string? Category { get; set; }
        public string? Availability { get; set; }
        public int? FarmId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}