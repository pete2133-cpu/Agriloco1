using System.ComponentModel.DataAnnotations;
using Agriloco1.Models.Inventory;

namespace Agriloco.Api.Models;

// Optional enrichment of an existing farm item, never a separate catalog/inclusion rule.
public class MarketDetails
{
    [Key] public int FarmDefinitionId { get; set; }
    public FarmDefinition FarmDefinition { get; set; } = null!;
    [MaxLength(160)] public string? DisplayName { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(100)] public string? Category { get; set; }
    public byte[]? Image { get; set; }
    [MaxLength(30)] public string? ImageContentType { get; set; }
    public List<MarketSellingOption> SellingOptions { get; set; } = new();
}

public class MarketSellingOption
{
    public int Id { get; set; }
    public int FarmDefinitionId { get; set; }
    public MarketDetails MarketDetails { get; set; } = null!;
    [MaxLength(100)] public string? Name { get; set; }
    public decimal? Price { get; set; }
    [MaxLength(3)] public string Currency { get; set; } = "CAD";
    public decimal? SellQuantity { get; set; }
    [MaxLength(10)] public string? SellUnit { get; set; }
    [MaxLength(20)] public string? PackageType { get; set; }
    [MaxLength(100)] public string? Sku { get; set; }
    [MaxLength(100)] public string? Barcode { get; set; }
    public bool TrackInventory { get; set; }
    public bool IsPublic { get; set; } = true;
    public int SortOrder { get; set; }
    // Reserved for explicit stock conversion later; never inferred or deducted today.
    [MaxLength(10)] public string? StockUnit { get; set; }
    public decimal? StockQuantityPerSale { get; set; }
}

public static class MarketUnits
{
    public static readonly string[] Units = { "each", "g", "kg", "oz", "lb", "mL", "L", "pint", "quart" };
    public static readonly string[] Packages = { "loose", "bag", "box", "basket", "bunch", "bundle", "jar", "bottle", "jug", "tray" };
}
