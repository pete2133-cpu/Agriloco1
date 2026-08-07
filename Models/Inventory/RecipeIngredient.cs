using System;

namespace Agriloco1.Models.Inventory
{
    public class RecipeIngredient
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int RecipeId { get; set; }

        // Optional link to the business inventory definition
        public int? InventoryItemId { get; set; }

        // Optional link to a specific item variation/package
        public int? InventoryItemPackageId { get; set; }

        // Saved text ensures the recipe still reads correctly
        // even if an inventory item is renamed later
        public string IngredientName { get; set; } = "";

        public string VariationName { get; set; } = "";

        public decimal Quantity { get; set; }

        public string Unit { get; set; } = "units";

        // Optional costing information
        public decimal? EstimatedUnitCost { get; set; }

        public decimal? EstimatedTotalCost { get; set; }

        public string Notes { get; set; } = "";

        // Controls the order shown in the recipe
        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}