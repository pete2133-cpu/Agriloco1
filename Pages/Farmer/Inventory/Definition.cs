using System;

namespace Agriloco1.Models.Inventory
{
    public class Definition
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string DefinitionType { get; set; } = "";

        public int? ParentDefinitionId { get; set; }

        public string CanonicalKey { get; set; } = "";

        public string Description { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}