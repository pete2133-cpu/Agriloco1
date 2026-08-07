using System;

namespace Agriloco1.Models.Inventory
{
    public class ProductionObservationValue
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int ProductionRunId { get; set; }

        public int ProductionObservationId { get; set; }

        public string MeasurementName { get; set; } = "";

        // Allows values such as Clear, Active, Sweet, Passed, etc.
        public string ValueText { get; set; } = "";

        // Optional numeric version for charts and calculations.
        public decimal? NumericValue { get; set; }

        public string Unit { get; set; } = "";

        public string Notes { get; set; } = "";

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}