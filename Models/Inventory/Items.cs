using System;

namespace Agriloco1.Models.Inventory
{
    public class InventoryItem
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public string ItemName { get; set; } = "";
        public string ItemCategory { get; set; } = "Ingredient";

        public string BaseUnit { get; set; } = "units";
        public string DefaultStorageLocation { get; set; } = "";

        public string ShelfLife { get; set; } = "";
        public string Notes { get; set; } = "";

        public string BarcodeValue { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}