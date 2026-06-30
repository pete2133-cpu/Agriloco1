using System;

namespace Agriloco1.Models.Inventory
{
    public class InventoryItemPackage
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int InventoryItemId { get; set; }

        // Example: 2L Jug, 4L Jug, Flat, Bin, 25kg Bag
        public string PackageName { get; set; } = "";

        // Example: Jug, Flat, Bin, Pail, Box, Case, Bag
        public string PackageType { get; set; } = "";

        // Example: 2, 4, 3, 25
        public decimal PackageQuantity { get; set; }

        // Example: L, kg, lbs, units
        public string PackageUnit { get; set; } = "units";

        public string StorageLocation { get; set; } = "";
        public string BarcodeValue { get; set; } = "";
        public string ShelfLife { get; set; } = "";

        public string Notes { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}