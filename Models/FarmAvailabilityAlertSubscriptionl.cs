using System;
using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Models
{
    public class FarmAvailabilityAlertSubscription
    {
        [Key]
        public int Id { get; set; }

        public int FarmId { get; set; }

        // Which crop the user wants alerts for
        public int CropId { get; set; }

        // Optional: keep for readability/back-compat (email-only for now)
        [MaxLength(320)]
        public string Email { get; set; } = "";

        // Where to send the alert (email now; phone later)
        [MaxLength(320)]
        public string Destination { get; set; } = "";

        // How to send (standardize to lowercase to avoid mismatches)
        // Use "email" for now
        [MaxLength(20)]
        public string Channel { get; set; } = "email";

        // One-time alert: once fulfilled, do not send again
        public bool IsFulfilled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // When the alert was fulfilled (email attempted/sent successfully)
        public DateTime? FulfilledAt { get; set; }

        // Legacy/debug timestamp (keep if referenced elsewhere)
        public DateTime? SentAt { get; set; }
    }
}