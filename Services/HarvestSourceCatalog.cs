using Agriloco1.Models.Inventory;

namespace Agriloco.Api.Services;

public sealed record HarvestSourceOption(int Id, string Name, int CropId, int? VarietyId = null);
public sealed class HarvestSourceCatalog
{
    public List<HarvestSourceOption> Crops { get; } = new();
    public List<HarvestSourceOption> Varieties { get; } = new();
    public List<HarvestSourceOption> Locations { get; } = new();

    public static HarvestSourceCatalog FromDefinitions(IEnumerable<FarmDefinition> definitions, int farmId)
    {
        var catalog = new HarvestSourceCatalog();
        var nodes = definitions.Where(d => d.FarmId == farmId && d.IsActive).ToDictionary(d => d.Id);
        foreach (var node in nodes.Values.OrderBy(d => d.SortOrder).ThenBy(d => d.DisplayName))
        {
            var path = new List<FarmDefinition>();
            var current = node;
            var seen = new HashSet<int>();
            bool valid = true;
            while (true)
            {
                if (!seen.Add(current.Id)) { valid = false; break; }
                path.Add(current);
                if (!current.ParentFarmDefinitionId.HasValue) break;
                if (!nodes.TryGetValue(current.ParentFarmDefinitionId.Value, out current!)) { valid = false; break; }
            }
            if (!valid) continue;
            var crop = path.FirstOrDefault(d => d.DefinitionType.Equals("Product", StringComparison.OrdinalIgnoreCase));
            if (crop == null) continue;
            var variety = path.FirstOrDefault(d => d.DefinitionType.Equals("Variety", StringComparison.OrdinalIgnoreCase));
            if (node.DefinitionType.Equals("Product", StringComparison.OrdinalIgnoreCase))
                catalog.Crops.Add(new(node.Id, node.DisplayName, node.Id));
            else if (node.DefinitionType.Equals("Variety", StringComparison.OrdinalIgnoreCase))
                catalog.Varieties.Add(new(node.Id, node.DisplayName, crop.Id, node.Id));
            else if (node.DefinitionType.Equals("Location", StringComparison.OrdinalIgnoreCase))
                catalog.Locations.Add(new(node.Id, node.DisplayName, crop.Id, variety?.Id));
        }
        return catalog;
    }
}