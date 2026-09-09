using Agriloco1.Models.Inventory;

namespace Agriloco.Api.Services;

// Select a single branch only. The caller supplies definitions from one farm.
public static class FarmHierarchyChanges
{
    public static List<FarmDefinition> Branch(IEnumerable<FarmDefinition> definitions, FarmDefinition selected, bool enabling)
    {
        var nodes = definitions.Where(d => d.FarmId == selected.FarmId).ToDictionary(d => d.Id);
        var result = new List<FarmDefinition>();
        var visited = new HashSet<int>();
        var pending = new Queue<int>();
        pending.Enqueue(selected.Id);
        var children = nodes.Values.Where(d => d.ParentFarmDefinitionId.HasValue)
            .ToLookup(d => d.ParentFarmDefinitionId!.Value);
        while (pending.TryDequeue(out var id))
        {
            if (!visited.Add(id) || !nodes.TryGetValue(id, out var node)) continue;
            result.Add(node);
            if (enabling)
            {
                if (node.ParentFarmDefinitionId.HasValue) pending.Enqueue(node.ParentFarmDefinitionId.Value);
            }
            else
            {
                foreach (var child in children[id]) pending.Enqueue(child.Id);
            }
        }
        return result;
    }

    public static void ApplyStatus(IEnumerable<FarmDefinition> definitions, FarmDefinition selected, string status)
    {
        var enabling = PublicFoodCatalog.IsAvailable(status);
        var now = DateTime.Now;
        foreach (var node in Branch(definitions, selected, enabling))
        {
            // Preserve an already-available ancestor's season/status detail.
            if (enabling && node.Id != selected.Id && PublicFoodCatalog.IsAvailable(node.Status)) continue;
            node.Status = node.Id == selected.Id || !enabling ? status : "Available";
            node.UpdatedAt = now;
        }
    }
}