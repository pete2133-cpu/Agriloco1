namespace Agriloco.Api.Dtos
{
    public class UnityTileOut
    {
        public int FarmId { get; set; }

        public int GridX { get; set; }
        public int GridY { get; set; }

        public string FeatureType { get; set; } = "Crop";

        public int? CropId { get; set; }
        public string? Category { get; set; }
        public string? Variety { get; set; }
        public string? Availability { get; set; }

        // Unity can tint tiles with this or map to sprites
        public string ColorCode { get; set; } = "#FFFFFF";
    }
}