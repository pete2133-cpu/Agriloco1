namespace Agriloco.Api.Models
{
    public class CropColor
    {
        public int Id { get; set; }

        // Crop type, e.g. "Apple", "Strawberry"
        public string Category { get; set; } = string.Empty;

        // Variety, e.g. "Honeycrisp", "Maya"
        // Can be null/empty meaning "default for this crop"
        public string? Variety { get; set; }

        // Final color code, e.g. "#FF0000"
        public string ColorCode { get; set; } = "#000000";
    }
}