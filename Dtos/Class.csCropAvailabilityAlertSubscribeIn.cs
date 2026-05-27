using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Dtos
{
    public class CropAvailabilityAlertSubscribeIn
    {
        [Required]
        public int FarmId { get; set; }

        [Required]
        public int CropId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}