namespace Agriloco.Api.Dtos
{
    public class RowLabelCreateIn
    {
        public int FarmId { get; set; }
        public int RowY { get; set; }
        public int CropId { get; set; }
        public string Availability { get; set; } = "Available"; // Available | NotAvailable
    }

    public class RowLabelOut
    {
        public int Id { get; set; }
        public int FarmId { get; set; }
        public int RowY { get; set; }
        public int CropId { get; set; }
        public string? Category { get; set; }
        public string? Variety { get; set; }
        public string Availability { get; set; } = "Available";
    }
}