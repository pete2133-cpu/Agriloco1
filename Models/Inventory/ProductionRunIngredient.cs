using System;

namespace Agriloco1.Models.Inventory
{
    public class ProductionRunIngredient
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int ProductionRunId { get; set; }

        // Optional link back to the recipe ingredient.
        public int? RecipeIngredientId { get; set; }

        public int? InventoryItemId { get; set; }

        public int? InventoryItemPackageId { get; set; }

        // Snapshot values preserve the historical batch.
        public string IngredientName { get; set; } = "";

        public string VariationName { get; set; } = "";

        public decimal RecipeQuantity { get; set; }

        public string Unit { get; set; } = "units";

        public decimal? SuggestedQuantity { get; set; }

        public decimal? ActualQuantity { get; set; }

        public bool IsScaleAnchor { get; set; }

        // Amount already posted to the inventory ledger.
        public decimal CommittedQuantity { get; set; }

        public string Notes { get; set; } = "";

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}