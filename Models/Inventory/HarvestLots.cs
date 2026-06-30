using System;

namespace Agriloco1.Models.Inventory
{
    public class HarvestLot
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public string LotNumber { get; set; } = "";

        public string CropName { get; set; } = "";
        public string VarietyName { get; set; } = "";

        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "lbs";

        public DateTime HarvestDate { get; set; }

        public string Status { get; set; } = "Private";

        public string Notes { get; set; } = "";
        public string Location { get; set; } = "";
        public string Workers { get; set; } = "";
        public string Condition { get; set; } = "Good";

        public int PhotoCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}