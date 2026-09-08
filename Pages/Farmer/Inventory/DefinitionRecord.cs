using System;

namespace Agriloco1.Models.Inventory
{
    public class DefinitionRecord
    {
        public int Id { get; set; }

        public int FarmId { get; set; }

        public int FarmDefinitionId { get; set; }

        public string RecordType { get; set; } = "";

        public DateTime RecordDate { get; set; } = DateTime.Now;

        public decimal? Quantity { get; set; }

        public string Unit { get; set; } = "";

        public string Notes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}