using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agriloco.Api.Models
{
    public class MapCell
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FarmId { get; set; }

        [ForeignKey(nameof(FarmId))]
        public Farm Farm { get; set; } = null!;

        // If null -> non-crop feature later (buildings, driveway, etc.)
        public int? CropId { get; set; }

        [ForeignKey(nameof(CropId))]
        public Crop? Crop { get; set; }

        // Grid coordinates
        [Required]
        public int GridX { get; set; }

        [Required]
        public int GridY { get; set; }

        // Optional future feature classification
        [MaxLength(50)]
        public string FeatureType { get; set; } = "Crop";

        public DateTime CreatedAt { get; set; }
    }
}