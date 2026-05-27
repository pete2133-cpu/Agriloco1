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

        // (1) child of Availability=Available
        public string? PickingCondition { get; set; } // Excellent / Good / Fair

        // legacy (keep)
        public string? OfferingType { get; set; }

        // (5) multi offering flags CSV: "PickYourOwn,ReadyPicked,Frozen"
        public string? OfferingTypes { get; set; }

        public int? YearPlanted { get; set; }

        public string? Rootstock { get; set; }

        public string? Notes { get; set; }

        // (7) availability note + inventory slots
        public string? AvailabilityNote { get; set; }

        public string? InventorySource { get; set; }       // "Square", "Shopify", "Manual"
        public string? InventoryExternalId { get; set; }   // product/variant id
        public int? InventoryQuantity { get; set; }
        public string? InventoryStatus { get; set; }       // "InStock", "Low", etc (string now)
    }

    public class CropSearchOut
    {
        public int Id { get; set; }
        public int FarmId { get; set; }

        public string Category { get; set; } = string.Empty;
        public string? Variety { get; set; }

        public string? Availability { get; set; }
        public string? PickingCondition { get; set; }

        public string? OfferingType { get; set; }
        public string? OfferingTypes { get; set; }

        public int? YearPlanted { get; set; }
        public string? Rootstock { get; set; }
        public string? Notes { get; set; }

        public string? AvailabilityNote { get; set; }

        public string? InventorySource { get; set; }
        public string? InventoryExternalId { get; set; }
        public int? InventoryQuantity { get; set; }
        public string? InventoryStatus { get; set; }
        public DateTime? InventoryLastSyncAt { get; set; }
    }

    public class CropAvailabilityUpdateIn
    {
        public string? Availability { get; set; }

        // (1) allow updating in the availability endpoint
        public string? PickingCondition { get; set; }
    }

    // NEW: update level-2 enrichment fields
    public class CropDetailsUpdateIn
    {
        public int? YearPlanted { get; set; }
        public string? Rootstock { get; set; }
        public string? Notes { get; set; }

        public string? OfferingType { get; set; }
        public string? OfferingTypes { get; set; }

        // (7)
        public string? AvailabilityNote { get; set; }
        public string? InventorySource { get; set; }
        public string? InventoryExternalId { get; set; }
        public int? InventoryQuantity { get; set; }
        public string? InventoryStatus { get; set; }

        // (1) allow updating here too (optional)
        public string? PickingCondition { get; set; }
    }
}