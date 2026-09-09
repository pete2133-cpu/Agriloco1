using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Services;

public record AvailabilityDeliveryResult(int Sent, int Failed)
{
    public string Message => Failed > 0
        ? $"Availability saved. {Sent} notification(s) sent; {Failed} failed. Check the email settings and application log."
        : Sent > 0 ? $"Availability saved. {Sent} subscriber notification(s) sent."
        : "Availability saved. No matching subscribers to notify.";
}

public sealed class DashboardAvailabilityNotifications(AgrilocoContext db, IEmailSender sender, ILogger logger)
{
    public async Task<AvailabilityDeliveryResult> SendAsync(int farmId, IReadOnlyCollection<int> changedIds)
    {
        var listings = (await PublicFoodCatalog.LoadAsync(db, farmId))
            .Where(x => x.FarmDefinitionId.HasValue && changedIds.Contains(x.FarmDefinitionId.Value)
                && PublicFoodCatalog.IsAvailable(x.Availability)).ToList();
        if (listings.Count == 0) return new(0, 0);
        var nodes = await db.FarmDefinitions.AsNoTracking().Where(x => x.FarmId == farmId).ToDictionaryAsync(x => x.Id);
        HashSet<int> Ancestors(int id)
        {
            var path = new HashSet<int>();
            while (nodes.TryGetValue(id, out var node) && path.Add(id))
            {
                if (!node.ParentFarmDefinitionId.HasValue) break;
                id = node.ParentFarmDefinitionId.Value;
            }
            return path;
        }
        var paths = listings.ToDictionary(x => x.FarmDefinitionId!.Value, x => Ancestors(x.FarmDefinitionId!.Value));
        // A newly activated variety covers its product's subscriptions too. Prefer its precise name.
        var events = listings.Where(x => !listings.Any(other => other != x && paths[other.FarmDefinitionId!.Value].Contains(x.FarmDefinitionId!.Value))).ToList();
        var subscriptions = await db.FarmDefinitionAvailabilitySubscriptions.Where(x => x.FarmId == farmId && x.IsActive && x.Channel == "email").ToListAsync();
        var legacySubscriptions = await db.FarmAvailabilityAlertSubscriptions.Where(x => x.FarmId == farmId && !x.IsFulfilled && x.Channel == "email").ToListAsync();
        var legacyCrops = await db.Crops.AsNoTracking().Where(x => x.FarmId == farmId).ToListAsync();
        static bool Same(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
        var matches = events.Select(item => new {
            Item = item,
            Definitions = subscriptions.Where(x => x.FarmDefinitionId == null || paths[item.FarmDefinitionId!.Value].Contains(x.FarmDefinitionId.Value)).ToList(),
            // Match legacy category/variety names within this farm, never interchangeable numeric IDs.
            Legacy = legacySubscriptions.Where(x => legacyCrops.Any(c => c.Id == x.CropId
                && Same(c.Category, item.Category) && (Same(c.Variety, item.Variety) || string.IsNullOrWhiteSpace(c.Variety)))).ToList()
        }).ToList();
        var recipients = matches.SelectMany(x => x.Definitions.Select(s => s.Email).Concat(x.Legacy.Select(s => s.Destination)))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var farmName = await db.Farms.Where(x => x.Id == farmId).Select(x => x.Name).FirstAsync();
        int sent = 0, failed = 0;
        foreach (var recipient in recipients)
        {
            var relevant = matches.Where(x => x.Definitions.Any(s => Same(s.Email, recipient)) || x.Legacy.Any(s => Same(s.Destination, recipient))).ToList();
            var names = relevant.Select(x => string.IsNullOrWhiteSpace(x.Item.Variety) ? x.Item.Category : $"{x.Item.Category} — {x.Item.Variety}").Distinct().ToList();
            var body = $"Good news!\n\nNow available at {farmName}:\n" + string.Join("\n", names.Select(x => "• " + x))
                + "\n\nAvailability can change during the day. Check the farm's current Agriloco listing before visiting.";
            try
            {
                await sender.SendAsync(recipient, $"{farmName}: {string.Join(", ", names)} now available", body);
                var now = DateTime.UtcNow;
                foreach (var sub in relevant.SelectMany(x => x.Definitions).Where(x => Same(x.Email, recipient)).Distinct()) sub.LastNotifiedAt = now;
                foreach (var sub in relevant.SelectMany(x => x.Legacy).Where(x => Same(x.Destination, recipient)).Distinct())
                {
                    sub.IsFulfilled = true; sub.SentAt = now; sub.FulfilledAt = now;
                }
                await db.SaveChangesAsync();
                sent++;
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogError(ex, "Availability email failed for farm {FarmId}. Status was saved.", farmId);
            }
        }
        return new(sent, failed);
    }
}
