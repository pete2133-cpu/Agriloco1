using System;

namespace Agriloco1.Models.Inventory
{
    public class FarmDefinition
    {
        public int Id { get; set; }

        public int FarmId { get; set; }

        public int? DefinitionId { get; set; }

        public int? ParentFarmDefinitionId { get; set; }

        public string DisplayName { get; set; } = "";

        public string DefinitionType { get; set; } = "";

        public string Status { get; set; } = "Unavailable";

        public string PresentationMode { get; set; } = "Index";

        public bool IsPublic { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public string Notes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}