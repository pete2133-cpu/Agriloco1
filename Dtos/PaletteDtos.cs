namespace Agriloco.Api.Dtos
{
    public class PaletteEntryOut
    {
        public int FarmId { get; set; }
        public int CropId { get; set; }

        public string Category { get; set; } = string.Empty;
        public string? Variety { get; set; }
        public string? Availability { get; set; }

        // 1-based index: 1,2,3...
        public int ColorIndex { get; set; }

        // Hex color for UI/Unity (derived from ColorIndex)
        public string ColorCode { get; set; } = "#000000";
    }
}