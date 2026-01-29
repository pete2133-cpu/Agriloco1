namespace Agriloco.Api.Dtos
{
    public class MapCellOut
    {
        public int Id { get; set; }
        public int FarmId { get; set; }

        public int? CropId { get; set; }
        public string? Category { get; set; }
        public string? Variety { get; set; }
        public string? Availability { get; set; }

        public int GridX { get; set; }
        public int GridY { get; set; }

        public string FeatureType { get; set; } = "Crop";
    }
}