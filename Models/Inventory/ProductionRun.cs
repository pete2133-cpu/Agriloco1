using System;

namespace Agriloco1.Models.Inventory
{
    public class ProductionRun
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int RecipeId { get; set; }

        // Snapshot of the recipe name when the batch was created.
        public string RecipeName { get; set; } = "";

        public string BatchNumber { get; set; } = "";

        public string ProcessType { get; set; } = "Other Food Processing";

        public string Status { get; set; } = "Draft";

        public DateTime ProductionDate { get; set; } = DateTime.Today;

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public decimal? ScaleFactor { get; set; }

        public int? ScaleAnchorIngredientId { get; set; }

        public decimal? ExpectedYieldQuantity { get; set; }

        public string ExpectedYieldUnit { get; set; } = "";

        public decimal? ActualYieldQuantity { get; set; }

        public string ActualYieldUnit { get; set; } = "";

        // Optional inventory item created by this batch.
        public int? OutputInventoryItemId { get; set; }

        public int? OutputInventoryItemPackageId { get; set; }

        public bool IngredientsCommitted { get; set; }

        public bool OutputPosted { get; set; }

        public bool InventoryReturned { get; set; }

        public string Notes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}