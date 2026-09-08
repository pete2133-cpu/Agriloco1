using System;

namespace Agriloco1.Models.Inventory
{
    public class AvailabilityChannel
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Code { get; set; } = "";

        public string Description { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}