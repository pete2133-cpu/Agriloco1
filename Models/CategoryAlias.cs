using System;

namespace Agriloco.Api.Models
{
    public class CategoryAlias
    {
        public int Id { get; set; }

        // Canonical category name (what your DB actually stores on crops)
        public string CanonicalCategory { get; set; } = "";

        // The alias term a user might type (e.g., "apples", "apple trees")
        public string Alias { get; set; } = "";

        // Stored lowercase keys for easy matching
        public string CanonicalKey { get; set; } = "";
        public string AliasKey { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}