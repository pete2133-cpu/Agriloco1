public class PublicFarmOut
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RegionCode { get; set; } = string.Empty;

    public string FruitCategory1 { get; set; } = string.Empty;

    public string ContactMethod1 { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GeoSource { get; set; }
    public DateTime? GeoUpdatedAt { get; set; }
    public string? MapImageUrl { get; set; }
}