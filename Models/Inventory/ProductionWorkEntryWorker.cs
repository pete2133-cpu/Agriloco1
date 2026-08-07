using System;

namespace Agriloco1.Models.Inventory
{
    public class ProductionWorkEntryWorker
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int ProductionRunId { get; set; }

        public int ProductionWorkEntryId { get; set; }

        public string WorkerName { get; set; } = "";

        public decimal HoursWorked { get; set; }

        public decimal? HourlyRate { get; set; }

        public decimal? LabourCost { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}