using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco1.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Services;

// Website read model only. Definition IDs must never be used as legacy Crop IDs.
public sealed class PublicFoodListing : CropSearchOut
{
    public int? FarmDefinitionId { get; init; }
    public List<string> SalesMethodCodes { get; init; } = new();
    public string SalesMethods { get; init; } = "";
}

public static class PublicFoodCatalog
{
    public static bool IsAvailable(string? status) =>
        new[] { "Available", "Peak Season", "Limited Availability", "Late Season" }
            .Contains(status ?? "", StringComparer.OrdinalIgnoreCase);

    public static bool Matches(string? value, string query)
    {
        query = query.Trim();
        return (value ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (query.Length > 3 && query.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
             (value ?? "").Contains(query[..^1], StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<List<PublicFoodListing>> LoadAsync(AgrilocoContext db, int? farmId = null)
    {
        var farmIds = await db.Farms.AsNoTracking()
            .Where(f => f.IsActive && (!farmId.HasValue || f.Id == farmId.Value))
            .Select(f => f.Id).ToListAsync();
        // Include hidden ancestors when checking visibility, but never publish them.
        var definitions = await db.FarmDefinitions.AsNoTracking()
            .Where(d => farmIds.Contains(d.FarmId)).ToListAsync();
        var byId = definitions.ToDictionary(d => d.Id);
        var channels = await db.AvailabilityChannels.AsNoTracking().Where(c => c.IsActive).ToDictionaryAsync(c => c.Id);
        var ids = definitions.Select(d => d.Id).ToList();
        var links = await db.FarmDefinitionChannels.AsNoTracking()
            .Where(c => ids.Contains(c.FarmDefinitionId) && c.IsEnabled).ToListAsync();
        var legacy = await db.Crops.AsNoTracking().Where(c => farmIds.Contains(c.FarmId)).ToListAsync();

        bool PublicPath(FarmDefinition d)
        {
            var seen = new HashSet<int>();
            while (true)
            {
                if (!seen.Add(d.Id) || !d.IsActive || !d.IsPublic) return false;
                if (!d.ParentFarmDefinitionId.HasValue) return true;
                if (!byId.TryGetValue(d.ParentFarmDefinitionId.Value, out var parent) || parent.FarmId != d.FarmId) return false;
                d = parent;
            }
        }
        string ProductName(FarmDefinition d)
        {
            var seen = new HashSet<int>();
            while (seen.Add(d.Id))
            {
                if (d.DefinitionType.Equals("Product", StringComparison.OrdinalIgnoreCase)) return d.DisplayName;
                if (!d.ParentFarmDefinitionId.HasValue || !byId.TryGetValue(d.ParentFarmDefinitionId.Value, out var parent) || parent.FarmId != d.FarmId) break;
                d = parent;
            }
            return "";
        }
        static string Key(int farm, string? category, string? variety) =>
            $"{farm}|{category?.Trim().ToUpperInvariant()}|{variety?.Trim().ToUpperInvariant()}";
        var result = new List<PublicFoodListing>();
        var definitionKeys = new HashSet<string>();
        foreach (var d in definitions.Where(d => d.DefinitionType.Equals("Product", StringComparison.OrdinalIgnoreCase) || d.DefinitionType.Equals("Variety", StringComparison.OrdinalIgnoreCase)))
        {
            var isVariety = d.DefinitionType.Equals("Variety", StringComparison.OrdinalIgnoreCase);
            var category = isVariety ? ProductName(d) : d.DisplayName;
            if (string.IsNullOrWhiteSpace(category)) category = d.DisplayName;
            var variety = isVariety ? d.DisplayName : "";
            var key = Key(d.FarmId, category, variety);
            definitionKeys.Add(key); // Prevent stale legacy duplicates overriding visibility or status.
            if (!PublicPath(d)) continue;
            var sales = links.Where(l => l.FarmDefinitionId == d.Id && channels.ContainsKey(l.AvailabilityChannelId))
                .Select(l => channels[l.AvailabilityChannelId]).DistinctBy(c => c.Id).OrderBy(c => c.SortOrder).ToList();
            var duplicate = legacy.FirstOrDefault(c => Key(c.FarmId, c.Category, c.Variety) == key);
            result.Add(new PublicFoodListing {
                Id = duplicate?.Id ?? 0, FarmDefinitionId = d.Id, FarmId = d.FarmId,
                Category = category, Variety = variety, Availability = d.Status,
                SalesMethodCodes = sales.Select(c => c.Code).ToList(),
                SalesMethods = string.Join(", ", sales.Select(c => c.Name))
            });
        }
        foreach (var c in legacy.Where(c => !definitionKeys.Contains(Key(c.FarmId, c.Category, c.Variety))))
        {
            var codes = new List<string>();
            if (!string.IsNullOrWhiteSpace(c.OfferingType)) codes.Add(c.OfferingType);
            // Legacy multi-method values have been stored as JSON as well as delimited text.
            if (!string.IsNullOrWhiteSpace(c.OfferingTypes))
            {
                try { codes.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<string>>(c.OfferingTypes) ?? new()); }
                catch (System.Text.Json.JsonException) { codes.AddRange(c.OfferingTypes.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); }
            }
            codes = codes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            string Label(string code) => channels.Values.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Name ??
                (code == "ReadyPicked" ? "Ready picked" : code == "PickYourOwn" ? "Pick your own" : code);
            result.Add(new PublicFoodListing { Id = c.Id, FarmId = c.FarmId, Category = c.Category, Variety = c.Variety,
                Availability = c.Availability, SalesMethodCodes = codes, SalesMethods = string.Join(", ", codes.Select(Label)) });
        }
        return result.OrderBy(c => c.Category).ThenBy(c => c.Variety).ThenBy(c => c.FarmId).ToList();
    }
}