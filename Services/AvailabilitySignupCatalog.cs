using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Services;

// The viewer's Variety selector uses direct children of a crop. Older farms may
// have typed those children as Product or Location; keep their real IDs intact.
public static class AvailabilitySignupCatalog
{
    public static bool IsSelection(FarmDefinition node, IEnumerable<FarmDefinition> nodes) =>
        node.DefinitionType.Equals("Product", StringComparison.OrdinalIgnoreCase) ||
        node.DefinitionType.Equals("Variety", StringComparison.OrdinalIgnoreCase) ||
        (!node.IsPathway && nodes.Any(parent => parent.Id == node.ParentFarmDefinitionId &&
            parent.FarmId == node.FarmId && parent.ParentFarmDefinitionId == null &&
            parent.DefinitionType.Equals("Product", StringComparison.OrdinalIgnoreCase)));

    public static async Task<List<PublicFoodListing>> LoadAsync(AgrilocoContext db, int farmId)
    {
        var listings = await PublicFoodCatalog.LoadAsync(db, farmId);
        if (!await db.Farms.AnyAsync(f => f.Id == farmId && f.IsActive)) return new();
        var nodes = await db.FarmDefinitions.AsNoTracking().Where(d => d.FarmId == farmId).ToListAsync();
        foreach (var node in nodes.Where(d => d.IsPublic && d.IsActive && !d.IsPathway &&
            !listings.Any(l => l.FarmDefinitionId == d.Id)))
        {
            var parent = nodes.FirstOrDefault(p => p.Id == node.ParentFarmDefinitionId && p.ParentFarmDefinitionId == null &&
                p.IsPublic && p.IsActive && p.DefinitionType.Equals("Product", StringComparison.OrdinalIgnoreCase));
            if (parent != null) listings.Add(new PublicFoodListing { FarmId = farmId, FarmDefinitionId = node.Id,
                Category = parent.DisplayName, Variety = node.DisplayName, Availability = node.Status });
        }
        return listings;
    }
}
