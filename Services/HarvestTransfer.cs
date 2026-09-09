using System.Text;
using System.Text.Json;
using Agriloco.Api.Models;
using Agriloco1.Models.Inventory;

namespace Agriloco1.Services;

// Portable name snapshots: database IDs are deliberately not part of the interchange format.
public record HarvestTransfer(string Farm, string Address, string Lot, string Crop,
    string Variety, string Row, DateOnly Date, decimal Quantity, string Unit)
{
    public const string Prefix = "AGRILOCO-HARVEST:1:";
    public const string FieldName = "Agriloco harvest source v1";
    public string Encode() => Prefix + JsonSerializer.Serialize(this);
    public static HarvestTransfer From(HarvestLot lot, Farm farm) => new(farm.Name, farm.Address,
        lot.LotNumber, lot.CropName, lot.VarietyName, lot.Location,
        DateOnly.FromDateTime(lot.HarvestDate), lot.Quantity, lot.Unit);
    public static bool TryParse(string? value, out HarvestTransfer? harvest)
    {
        harvest = null;
        if (value == null || Encoding.UTF8.GetByteCount(value) > 2000 || !value.StartsWith(Prefix)) return false;
        try
        {
            var parsed = JsonSerializer.Deserialize<HarvestTransfer>(value[Prefix.Length..]);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Farm) || string.IsNullOrWhiteSpace(parsed.Lot)
                || string.IsNullOrWhiteSpace(parsed.Crop) || string.IsNullOrWhiteSpace(parsed.Unit)
                || parsed.Address == null || parsed.Variety == null || parsed.Row == null
                || parsed.Date == default || parsed.Quantity <= 0) return false;
            harvest = parsed;
            return true;
        }
        catch (JsonException) { return false; }
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public string Summary => $"{Farm} · {Lot} · {Crop} / {Variety} · {Row} · Harvested {Date:yyyy-MM-dd} · {Quantity} {Unit}";
}
