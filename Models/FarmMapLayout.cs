using System;

namespace Agriloco.Api.Models
{
    public class FarmMapLayout
    {
        public int Id { get; set; }

        // One layout per farm
        public int FarmId { get; set; }
        public Farm? Farm { get; set; }

        // Store the Unity payload JSON as-is
        public string Json { get; set; } = "{}";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}