using System;

namespace Agriloco.Api.Models
{
    // One row can represent:
    // - a Category (Variety == null)
    // - a Variety under a Category (Variety != null)
    public class CropCatalogItem
    {
        public int Id { get; set; }

        public string Category { get; set; } = "";

        // null means "this row is a category record"
        public string? Variety { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}