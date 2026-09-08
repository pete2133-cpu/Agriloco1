using System;
using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Models
{
    public class FarmDefinitionAvailabilitySubscription
    {
        [Key]
        public int Id { get; set; }

        // Farm being followed.
        public int FarmId { get; set; }

        // null = entire farm / All Crops
        //
        // Product ID = that product and its relevant descendants
        // Example:
        // Apple = receive availability updates for Apple varieties.
        //
        // Variety ID = that specific variety
        // Example:
        // Ambrosia = receive Ambrosia availability updates.
        public int? FarmDefinitionId { get; set; }

        [Required]
        [MaxLength(320)]
        public string Email { get; set; } = "";

        // Email for now.
        // Leaves room for SMS/push later.
        [Required]
        [MaxLength(20)]
        public string Channel { get; set; } = "email";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        // Last time this subscription generated an alert.
        //
        // This DOES NOT deactivate the subscription.
        // A customer can receive future availability events.
        public DateTime? LastNotifiedAt { get; set; }
    }
}