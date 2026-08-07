using System;

namespace Agriloco1.Models.Inventory
{
    public class ReceivingLotCustomField
    {
        public int Id { get; set; }

        public int? FarmId { get; set; }

        public int ReceivingLotId { get; set; }

        public string FieldName { get; set; } = "";
        public string FieldValue { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}