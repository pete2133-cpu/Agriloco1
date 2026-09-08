using System;
using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Models
{
    public class CropAvailabilityAlertSubscription
    {
        [Key]
        public int Id { get; set; }

        public int FarmId { get; set; }

        // Subscribe to a specific crop row (your Crop.Id)
        public int CropId { get; set; }

        [MaxLength(320)]
        public string Email { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // one-time behavior: once set, we never send again (unless you later decide to reset)
        public DateTime? NotifiedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}