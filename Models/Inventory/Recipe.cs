using System;

namespace Agriloco1.Models.Inventory
{
    public class Recipe
    {
        public int Id { get; set; }

        // The farm or business that owns this private recipe
        public int? FarmId { get; set; }

        public string RecipeName { get; set; } = "";

        public string RecipeCategory { get; set; } = "Other";

        // These can remain blank until the recipe details page is completed
        public decimal? ExpectedYieldQuantity { get; set; }

        public string ExpectedYieldUnit { get; set; } = "";

        public string Status { get; set; } = "Draft";

        public string Notes { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}