
using System;
using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Dtos
{
    public class FarmMinimalIn
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string ContactMethod1 { get; set; } = string.Empty;

        [Required]
        public string FruitCategory1 { get; set; } = string.Empty;
    }

    public class FarmMinimalOut
    {
        public int Id { get; set; }
        public string AgrilocoId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string FruitCategory1 { get; set; } = string.Empty;
        public string ContactMethod1 { get; set; } = string.Empty;
    }

    public class PublicFarmOut
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string RegionCode { get; set; } = string.Empty;

        public string FruitCategory1 { get; set; } = string.Empty;

        public string ContactMethod1 { get; set; } = string.Empty;

        // FIX: used by Public/Farm page
        public string Address { get; set; } = string.Empty;
        public string? MapImageUrl { get; set; }

        // new profile placeholders (not required to show yet)
        public string? Hours { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? ParkingInfo { get; set; }
        public string? EntranceInfo { get; set; }
        public string? PaymentInfo { get; set; }
        public string? ReservationUrl { get; set; }

        public DateTime? ProfileLastUpdatedAt { get; set; }
        public int ProfileUpdateCount { get; set; }
    }
}