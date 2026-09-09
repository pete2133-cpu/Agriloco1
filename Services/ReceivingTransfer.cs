using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agriloco.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Services;

public record ReceivingTransfer(string Receiver, string Address, string Lot, DateOnly Date,
    string Supplier, string Item, decimal Quantity, string Unit, decimal TotalQuantity,
    string BaseUnit, string Condition, HarvestTransfer? Harvest)
{
    public const string Prefix = "AGRILOCO-RECEIPT:1:";
    public string Encode() => Prefix + JsonSerializer.Serialize(this);
    [JsonIgnore] public string Summary => $"{Receiver} · {Lot} · Received {Date:yyyy-MM-dd} from {Supplier} · {Item} · {Quantity} {Unit} · Total {TotalQuantity} {BaseUnit}";

    public static bool TryParse(string? value, out ReceivingTransfer? receipt)
    {
        receipt = null;
        if (value == null || Encoding.UTF8.GetByteCount(value) > 2200 || !value.StartsWith(Prefix)) return false;
        try
        {
            var parsed = JsonSerializer.Deserialize<ReceivingTransfer>(value[Prefix.Length..]);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Receiver) || string.IsNullOrWhiteSpace(parsed.Lot)
                || string.IsNullOrWhiteSpace(parsed.Item) || string.IsNullOrWhiteSpace(parsed.Unit)
                || string.IsNullOrWhiteSpace(parsed.BaseUnit) || parsed.Address == null || parsed.Supplier == null
                || parsed.Condition == null || parsed.Date == default || parsed.Quantity <= 0 || parsed.TotalQuantity <= 0
                || (parsed.Harvest != null && !HarvestTransfer.TryParse(parsed.Harvest.Encode(), out _))) return false;
            receipt = parsed;
            return true;
        }
        catch (JsonException) { return false; }
    }

    public static async Task<ReceivingTransfer?> FromLotAsync(AgrilocoContext db, int farmId, int lotId)
    {
        var lot = await db.ReceivingLots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lotId && x.FarmId == farmId);
        var farm = await db.Farms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == farmId);
        if (lot == null || farm == null) return null;
        var field = await db.ReceivingLotCustomFields.AsNoTracking().FirstOrDefaultAsync(x => x.FarmId == farmId
            && x.ReceivingLotId == lotId && x.FieldName == HarvestTransfer.FieldName);
        HarvestTransfer? harvest = null;
        // Never silently drop a saved but unreadable harvest source from a receipt label.
        if (field != null && !HarvestTransfer.TryParse(field.FieldValue, out harvest)) return null;
        return new(farm.Name, farm.Address, lot.LotNumber, DateOnly.FromDateTime(lot.ReceivedDate),
            lot.SupplierName, lot.InventoryItemName, lot.UnitsReceived,
            lot.InventoryItemPackageId.HasValue ? lot.VariationName : lot.UsableUnit,
            lot.TotalUsableQuantity, lot.UsableUnit, lot.Condition, harvest);
    }
}
