using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace agriloco.api.Pages.Public
{
    public class FarmModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public FarmModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int id { get; set; }

        public PublicFarmOut? Farm { get; set; }
        public List<CropSearchOut> Crops { get; set; } = new();

        public string LayoutJson { get; set; } = "{}";

        public async Task<IActionResult> OnGetAsync()
        {
            if (id <= 0) return BadRequest("Missing farm id.");

            var farm = await _db.Farms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
            if (farm == null) return NotFound("Farm not found.");

            Farm = new PublicFarmOut
            {
                Id = farm.Id,
                Name = farm.Name,
                RegionCode = farm.RegionCode ?? "",
                FruitCategory1 = farm.FruitCategory1 ?? "",
                ContactMethod1 = farm.ContactMethod1 ?? "",
                Address = farm.Address ?? "",
                MapImageUrl = farm.MapImageUrl
            };

            Crops = await _db.Crops
                .AsNoTracking()
                .Where(c => c.FarmId == id)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Variety)
                .Select(c => new CropSearchOut
                {
                    Id = c.Id,
                    FarmId = c.FarmId,
                    Category = c.Category,
                    Variety = c.Variety,
                    OfferingType = c.OfferingType,
                    OfferingTypes = c.OfferingTypes,
                    Availability = c.Availability,
                    PickingCondition = c.PickingCondition,
                    AvailabilityNote = c.AvailabilityNote,
                    InventorySource = c.InventorySource,
                    InventoryExternalId = c.InventoryExternalId,
                    InventoryQuantity = c.InventoryQuantity,
                    InventoryStatus = c.InventoryStatus,
                    InventoryLastSyncAt = c.InventoryLastSyncAt
                })
                .ToListAsync();

            var layout = await _db.FarmMapLayouts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FarmId == id);

            if (layout == null || string.IsNullOrWhiteSpace(layout.Json))
            {
                LayoutJson = JsonSerializer.Serialize(new MapLayoutPayload { farmId = id });
                return Page();
            }

            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            MapLayoutPayload? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<MapLayoutPayload>(layout.Json, opts);
            }
            catch
            {
                LayoutJson = JsonSerializer.Serialize(new MapLayoutPayload { farmId = id });
                return Page();
            }

            if (payload == null)
            {
                LayoutJson = JsonSerializer.Serialize(new MapLayoutPayload { farmId = id });
                return Page();
            }

            var cropById = Crops
                .Where(c => c.Id > 0)
                .ToDictionary(c => c.Id, c => c);

            if (payload.markers != null)
            {
                foreach (var m in payload.markers)
                {
                    if (m == null) continue;

                    if (m.cropId > 0 && cropById.TryGetValue(m.cropId, out var crop))
                    {
                        var catKey = (crop.Category ?? "").Trim().ToLowerInvariant();
                        var state = NormalizeAvailability(crop.Availability);

                        if (!string.IsNullOrWhiteSpace(catKey))
                            m.iconName = $"cat_{catKey}_{state}";

                        m.category = crop.Category ?? "";
                        m.variety = crop.Variety ?? "";
                        m.offeringType = crop.OfferingType ?? "";
                        m.availability = crop.Availability ?? "";
                        m.farmId = id;
                    }
                    else
                    {
                        var catKey = (m.category ?? "").Trim().ToLowerInvariant();
                        if (!string.IsNullOrWhiteSpace(catKey))
                            m.iconName = $"cat_{catKey}_unknown";
                        else
                            m.iconName = "cat_unknown_unknown";
                    }
                }
            }

            LayoutJson = JsonSerializer.Serialize(payload);
            return Page();
        }

        private static string NormalizeAvailability(string? availability)
        {
            if (string.IsNullOrWhiteSpace(availability))
                return "unknown";

            var a = availability.Trim().ToLowerInvariant();
            if (a == "available") return "available";
            if (a == "notavailable") return "unavailable";

            return "unknown";
        }

        public class MapLayoutPayload
        {
            public int farmId { get; set; }
            public string? savedAtIso { get; set; }
            public float surfaceWidth { get; set; }
            public float surfaceHeight { get; set; }
            public List<MarkerRecord>? markers { get; set; } = new();
            public List<LineRecord>? lines { get; set; } = new();
        }

        public class MarkerRecord
        {
            public float x { get; set; }
            public float y { get; set; }
            public float scale { get; set; }
            public string? iconName { get; set; }

            public int cropId { get; set; }
            public int farmId { get; set; }
            public string? category { get; set; }
            public string? variety { get; set; }
            public string? offeringType { get; set; }
            public string? availability { get; set; }
        }

        public class LineRecord
        {
            public float x1 { get; set; }
            public float y1 { get; set; }
            public float x2 { get; set; }
            public float y2 { get; set; }
            public float width { get; set; }
            public float r { get; set; }
            public float g { get; set; }
            public float b { get; set; }
            public float a { get; set; }
        }
    }
}