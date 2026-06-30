using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agriloco1.Models.Inventory
{
    [Table("ReceivingLots")]
    public class ReceivingLot
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public string LotNumber { get; set; } = "";

        public DateTime ReceivedDate { get; set; } = DateTime.Today;

        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = "";

        public int? InventoryItemId { get; set; }
        public string InventoryItemName { get; set; } = "";

        public int? InventoryItemPackageId { get; set; }
        public string VariationName { get; set; } = "";
        public string PackageType { get; set; } = "";

        public decimal UnitsReceived { get; set; }

        public decimal UsableQuantityPerUnit { get; set; }
        public string UsableUnit { get; set; } = "units";
        public decimal TotalUsableQuantity { get; set; }

        // Legacy columns from earlier ReceivingLots table.
        public string ItemName { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "units";

        public string Status { get; set; } = "Received";
        public string Condition { get; set; } = "Good";

        public string StorageLocation { get; set; } = "";
        public string BarcodeValue { get; set; } = "";

        public string InvoiceNumber { get; set; } = "";
        public decimal? PriceTotal { get; set; }

        public int PhotoCount { get; set; }
        public int DocumentCount { get; set; }

        public int? SourceHarvestLotId { get; set; }

        public string Notes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}