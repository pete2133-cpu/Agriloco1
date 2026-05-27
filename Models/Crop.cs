using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agriloco.Api.Models
{
    public class Crop
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FarmId { get; set; }

        [ForeignKey(nameof(FarmId))]
        public Farm Farm { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Variety { get; set; }

        // "Available", "NotAvailable", or null
        [MaxLength(20)]
        public string? Availability { get; set; }

        // (1) Child field for picking conditions
        // Only meaningful if Availability == "Available"
        [MaxLength(20)]
        public string? PickingCondition { get; set; } // Excellent / Good / Fair / null

        // (5) Legacy single offering type (keep for backward compatibility)
        [MaxLength(20)]
        public string? OfferingType { get; set; }

        // (5) NEW: multi-offering flags (CSV like: "PickYourOwn,ReadyPicked")
        [MaxLength(200)]
        public string? OfferingTypes { get; set; }

        public int? YearPlanted { get; set; }

        [MaxLength(100)]
        public string? Rootstock { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // (7) Optional note like "Limited supply", "Fresh to order", "Ending soon"
        [MaxLength(200)]
        public string? AvailabilityNote { get; set; }

        // (7) Inventory slots (manual or future external import)
        [MaxLength(30)]
        public string? InventorySource { get; set; }       // e.g., "Square", "Shopify", "Manual"
        [MaxLength(100)]
        public string? InventoryExternalId { get; set; }   // product/variant id
        public int? InventoryQuantity { get; set; }
        [MaxLength(50)]
        public string? InventoryStatus { get; set; }       // icon/status string
        public DateTime? InventoryLastSyncAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}