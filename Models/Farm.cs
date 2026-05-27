using System;

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

        // ==========================
        // (2) Geo location (manual or future external import)
        // ==========================
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // ==========================
        // (3) Hours placeholder (manual or future Google import)
        // ==========================
        public string? Hours { get; set; }

        // ==========================
        // (4) Optional profile fields
        // ==========================
        public string? ParkingInfo { get; set; }
        public string? EntranceInfo { get; set; }
        public string? PaymentInfo { get; set; }
        public string? ReservationUrl { get; set; }

        // ==========================
        // (6) Profile update stats
        // ==========================
        public DateTime? ProfileLastUpdatedAt { get; set; }
        public int ProfileUpdateCount { get; set; } = 0;

        // ==========================
        // Basemap image (already used)
        // ==========================
        public string? MapImageUrl { get; set; }
        public DateTime? MapImageUploadedAt { get; set; }
    }
}