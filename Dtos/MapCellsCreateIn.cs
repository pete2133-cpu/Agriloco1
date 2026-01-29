using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Dtos
{
    public class MapCellCreateIn
    {
        [Required]
        public int FarmId { get; set; }

        // optional (null = non-crop feature later)
        public int? CropId { get; set; }

        [Required]
        public int GridX { get; set; }

        [Required]
        public int GridY { get; set; }

        // optional label
        public string? FeatureType { get; set; }
    }
}
