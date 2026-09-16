using Agriloco.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Services;

// Metadata only: never copy image bytes into descendants or listing payloads.
public sealed class DefinitionImages(AgrilocoContext db)
{
    public async Task<Dictionary<int, int>> ResolveAsync(int farmId, bool publicOnly)
    {
        var rows = await db.FarmDefinitions.AsNoTracking().Where(d => d.FarmId == farmId)
            .Select(d => new { d.Id, d.ParentFarmDefinitionId, d.IsActive, d.IsPublic }).ToListAsync();
        var byId = rows.ToDictionary(d => d.Id);
        var images = (await db.MarketDetails.AsNoTracking()
            .Where(d => d.Image != null && d.FarmDefinition.FarmId == farmId)
            .Select(d => d.FarmDefinitionId).ToListAsync()).ToHashSet();
        var result = new Dictionary<int, int>();
        foreach (var item in rows)
        {
            var seen = new HashSet<int>();
            int? id = item.Id, source = null;
            bool valid = false;
            while (id.HasValue && byId.TryGetValue(id.Value, out var row) && seen.Add(row.Id))
            {
                if (!row.IsActive || (publicOnly && !row.IsPublic)) break;
                if (source == null && images.Contains(row.Id)) source = row.Id;
                if (!row.ParentFarmDefinitionId.HasValue) { valid = true; break; }
                id = row.ParentFarmDefinitionId;
            }
            // Fail closed for cycles, cross-farm/missing ancestors, and hidden public branches.
            if (valid && source.HasValue) result[item.Id] = source.Value;
        }
        return result;
    }

    public static string Url(int farmId, int sourceId, bool publicOnly) =>
        $"/api/DefinitionImages/{(publicOnly ? "public" : "farmer")}?farmId={farmId}&itemId={sourceId}";
}
