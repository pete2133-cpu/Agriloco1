using System;

namespace Agriloco1.Models.Inventory
{
    public class ProductionObservation
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int ProductionRunId { get; set; }

        // Optional labour/work event associated with this check.
        public int? ProductionWorkEntryId { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        public string ObservationType { get; set; } = "Batch Check";

        public string Notes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}