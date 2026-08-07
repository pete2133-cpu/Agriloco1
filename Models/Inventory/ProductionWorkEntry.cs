using System;

namespace Agriloco1.Models.Inventory
{
    public class ProductionWorkEntry
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int ProductionRunId { get; set; }

        public string WorkType { get; set; } = "Production";

        public DateTime WorkDate { get; set; } = DateTime.Now;

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        // Used as the default when another worker is added.
        public decimal DefaultHours { get; set; } = 1;

        public string Notes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}