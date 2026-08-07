using System;

namespace Agriloco1.Models.Inventory
{
    public class InventoryMovement
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int? InventoryItemId { get; set; }

        public int? InventoryItemPackageId { get; set; }

        public int? ReceivingLotId { get; set; }

        public int? ProductionRunId { get; set; }

        public int? ProductionRunIngredientId { get; set; }

        public string MovementType { get; set; } = "";

        // Positive quantities add inventory.
        // Negative quantities remove inventory.
        public decimal Quantity { get; set; }

        public string Unit { get; set; } = "units";

        public string ReferenceNumber { get; set; } = "";

        public string Notes { get; set; } = "";

        public DateTime OccurredAt { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}