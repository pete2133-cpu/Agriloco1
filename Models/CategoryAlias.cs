using System;

namespace Agriloco.Api.Models
{
    public class CropCatalogAlias
    {
        public int Id { get; set; }

        public string CanonicalCategory { get; set; } = string.Empty;

        public string Alias { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}