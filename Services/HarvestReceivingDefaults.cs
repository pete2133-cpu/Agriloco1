using Agriloco1.Models.Inventory;

namespace Agriloco1.Services;

public record HarvestReceivingDefaults(string Supplier, string Item, string Package, string Variety, string Row, string Message)
{
    // Match whole names, never substrings (Apple must not select Apple Cider Vinegar).
    private static string Key(string value)
    {
        var name = string.Join(" ", value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (name.EndsWith("ies")) return name[..^3] + "y";
        return name.EndsWith('s') && !name.EndsWith("ss") ? name[..^1] : name;
    }

    public static HarvestReceivingDefaults For(HarvestLot harvest, string farmName,
        IEnumerable<Supplier> suppliers, IEnumerable<InventoryItem> items, IEnumerable<InventoryItemPackage> packages)
    {
        var supplierMatches = suppliers.Where(x => x.IsActive && x.FarmId == harvest.FarmId && x.LinkedFarmId == harvest.FarmId).ToList();
        if (supplierMatches.Count == 0)
            supplierMatches = suppliers.Where(x => x.IsActive && x.FarmId == harvest.FarmId && string.Equals(x.SupplierName.Trim(), farmName.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        var supplier = supplierMatches.Count == 1 ? $"{supplierMatches[0].Id} | {supplierMatches[0].SupplierName}" : farmName;
        var active = items.Where(x => x.IsActive && x.FarmId == harvest.FarmId).ToList();
        var matches = active.Where(x => !string.IsNullOrWhiteSpace(harvest.VarietyName) &&
            (Key(x.ItemName) == Key(harvest.CropName + " " + harvest.VarietyName)
             || Key(x.ItemName) == Key(harvest.VarietyName + " " + harvest.CropName))).ToList();
        if (matches.Count == 0) matches = active.Where(x => Key(x.ItemName) == Key(harvest.CropName)).ToList();
        if (matches.Count != 1)
            return new(supplier, "", "", harvest.VarietyName, harvest.Location,
                "Supplier and harvest details filled. Choose the inventory item: there is no single matching saved item for this crop.");
        var item = matches[0];
        return new(supplier, $"{item.Id} | {item.ItemName} ({item.BaseUnit})", "",
            harvest.VarietyName, harvest.Location,
            "Supplier, item and crop variety filled. Choose the receiving unit and enter the amount received; it can differ from the harvest unit.");
    }
}
