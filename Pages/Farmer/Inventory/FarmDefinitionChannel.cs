using System;

namespace Agriloco1.Models.Inventory
{
    public class FarmDefinitionChannel
    {
        public int Id { get; set; }

        public int FarmDefinitionId { get; set; }

        public int AvailabilityChannelId { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}