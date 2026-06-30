using System;

namespace Agriloco1.Models.Inventory
{
    public class Supplier
    {
        public int Id { get; set; }

        // The Agriloco farm/business that owns this private supplier record
        public int? FarmId { get; set; }

        public string SupplierName { get; set; } = "";
        public string SupplierType { get; set; } = "Farm";

        // Optional: link this private supplier to a real Agriloco farm profile
        public int? LinkedFarmId { get; set; }

        public string ContactName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string Notes { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}