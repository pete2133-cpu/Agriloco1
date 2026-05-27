using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Dtos
{
    public class AvailabilityAlertSubscribeIn
    {
        [Required]
        public int FarmId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}