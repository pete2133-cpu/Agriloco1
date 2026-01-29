using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agriloco.Api.Models
{
    public class Crop
    {
        [Key]
        public int Id { get; set; }

        // REQUIRED — ties this crop asset to a farm
        [Required]
        public int FarmId { get; set; }

        [ForeignKey(nameof(FarmId))]
        public Farm Farm { get; set; } = null!;

        // REQUIRED — dropdown-controlled crop type
        // Examples: Apple, Strawberry, Blueberry, Raspberry, Corn, Pumpkin
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        // OPTIONAL — descriptive only, does NOT affect search level
        [MaxLength(100)]
        public string? Variety { get; set; }

        // LEVEL 2 — ranking signal
        // "Available", "NotAvailable", or null
        [MaxLength(20)]
        public string? Availability { get; set; }
        [MaxLength(20)]
        public string? OfferingType { get; set; }
        // OPTIONAL metadata (Level 2 enrichment)
        public int? YearPlanted { get; set; }

        [MaxLength(100)]
        public string? Rootstock { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
