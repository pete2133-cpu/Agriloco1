using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco.Api.Models;
using Agriloco.Api.Services; // IFarmAvailabilityAlertQueue + CropBecameAvailableEvent
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CropsController : ControllerBase
    {
        private readonly AgrilocoContext _context;
        private readonly IFarmAvailabilityAlertQueue _alertQueue;

        private static readonly HashSet<string> AllowedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Apple",
            "Strawberry",
            "Blueberry",
            "Raspberry",
            "Corn",
            "Pumpkin",
            "Grapes",
            "Road",
            "Washroom",
            "Market",
            "ParkingLot",
            "Pathway",
        };

        private static readonly HashSet<string> AllowedOfferingTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PickYourOwn",
            "ReadyPicked",
            "Frozen"
        };

        private static readonly HashSet<string> AllowedAvailabilityValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Available",
            "NotAvailable"
        };

        private static readonly HashSet<string> AllowedPickingConditions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Excellent",
            "Good",
            "Fair"
        };

        public CropsController(AgrilocoContext context, IFarmAvailabilityAlertQueue alertQueue)
        {
            _context = context;
            _alertQueue = alertQueue;
        }

        // ============================================================
        // GET: /api/Crops/categories
        // Includes built-ins + canonical categories from alias table
        // ============================================================
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            // If your DB doesn't have CropCatalogAliases, remove this block.
            var aliasCanonicals = await _context.CropCatalogAliases
                .AsNoTracking()
                .Select(a => a.CanonicalCategory)
                .ToListAsync();

            var list = AllowedCategories
                .Concat(aliasCanonicals.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            return Ok(list);
        }

        // ============================================================
        // NEW: GET /api/Crops/varieties?category=Apple
        // Used for cascading dropdowns (category -> variety list)
        // ============================================================
        [HttpGet("varieties")]
        public async Task<ActionResult<List<string>>> GetVarieties([FromQuery] string? category)
        {
            var raw = (category ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return Ok(new List<string>());

            // Normalize category via alias table (same logic as create)
            var canonical = await ResolveCanonicalCategoryAsync(raw);

            // Optional: only allow known categories
            // If you want "unknown" categories to still work, remove this check.
            if (!AllowedCategories.Contains(canonical))
                return Ok(new List<string>());

            // Pull distinct varieties for that category
            var varieties = await _context.Crops
                .AsNoTracking()
                .Where(c => c.Category == canonical)
                .Select(c => c.Variety)
                .Where(v => v != null && v.Trim() != "")
                .Select(v => v!.Trim())
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            return Ok(varieties);
        }

        // ============================================================
        // POST: /api/Crops
        // ============================================================
        [HttpPost]
        public async Task<ActionResult<CropSearchOut>> CreateCrop([FromBody] CropCreateIn input)
        {
            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == input.FarmId);
            if (farm == null || !farm.IsActive)
                return BadRequest("Farm does not exist or is not active.");

            if (string.IsNullOrWhiteSpace(input.Category))
                return BadRequest("Category is required.");

            // Normalize category via alias table
            var category = await ResolveCanonicalCategoryAsync(input.Category);

            if (!AllowedCategories.Contains(category))
                return BadRequest("Category must be one of the allowed categories.");

            var variety = string.IsNullOrWhiteSpace(input.Variety) ? null : input.Variety.Trim();
            var rootstock = string.IsNullOrWhiteSpace(input.Rootstock) ? null : input.Rootstock.Trim();
            var notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

            string? availability = string.IsNullOrWhiteSpace(input.Availability) ? null : input.Availability.Trim();
            if (availability != null && !AllowedAvailabilityValues.Contains(availability))
                return BadRequest("Availability must be 'Available', 'NotAvailable', or null.");

            // PickingCondition: only valid if Available
            string? pickingCondition = string.IsNullOrWhiteSpace(input.PickingCondition) ? null : input.PickingCondition.Trim();
            if (pickingCondition != null && !AllowedPickingConditions.Contains(pickingCondition))
                return BadRequest("PickingCondition must be 'Excellent', 'Good', 'Fair', or null.");

            if (!string.Equals(availability, "Available", StringComparison.OrdinalIgnoreCase))
                pickingCondition = null;

            // Legacy single offering type
            string? offeringType = string.IsNullOrWhiteSpace(input.OfferingType) ? null : input.OfferingType.Trim();
            if (offeringType != null && !AllowedOfferingTypes.Contains(offeringType))
                return BadRequest("OfferingType must be 'PickYourOwn', 'ReadyPicked', 'Frozen', or null.");

            // New multi offering types (CSV)
            string? offeringTypes = null;
            try
            {
                offeringTypes = NormalizeOfferingTypesCsv(input.OfferingTypes);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            if (input.YearPlanted.HasValue)
            {
                var year = input.YearPlanted.Value;
                if (year < 1800 || year > DateTime.UtcNow.Year + 1)
                    return BadRequest("YearPlanted is out of a reasonable range.");
            }

            var crop = new Crop
            {
                FarmId = input.FarmId,
                Category = category,
                Variety = variety,
                Availability = availability,
                PickingCondition = pickingCondition,

                OfferingType = offeringType,
                OfferingTypes = offeringTypes,

                YearPlanted = input.YearPlanted,
                Rootstock = rootstock,
                Notes = notes,

                AvailabilityNote = string.IsNullOrWhiteSpace(input.AvailabilityNote) ? null : input.AvailabilityNote.Trim(),

                InventorySource = string.IsNullOrWhiteSpace(input.InventorySource) ? null : input.InventorySource.Trim(),
                InventoryExternalId = string.IsNullOrWhiteSpace(input.InventoryExternalId) ? null : input.InventoryExternalId.Trim(),
                InventoryQuantity = input.InventoryQuantity,
                InventoryStatus = string.IsNullOrWhiteSpace(input.InventoryStatus) ? null : input.InventoryStatus.Trim(),
                InventoryLastSyncAt = null,

                CreatedAt = DateTime.UtcNow
            };

            // Backward compat: if OfferingType empty but OfferingTypes has values -> set first
            if (string.IsNullOrWhiteSpace(crop.OfferingType) && !string.IsNullOrWhiteSpace(crop.OfferingTypes))
                crop.OfferingType = crop.OfferingTypes.Split(',').Select(s => s.Trim()).FirstOrDefault();

            _context.Crops.Add(crop);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCropById), new { id = crop.Id }, ToPublicOut(crop));
        }

        // ============================================================
        // GET: /api/Crops/{id}
        // ============================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<CropSearchOut>> GetCropById(int id)
        {
            var crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == id);
            if (crop == null)
                return NotFound();

            return Ok(ToPublicOut(crop));
        }

        // ============================================================
        // GET: /api/Crops/public?offeringType=...&availability=...
        // Legacy search filters (single offeringType + availability)
        // ============================================================
        [HttpGet("public")]
        public async Task<ActionResult<List<CropSearchOut>>> GetPublicCrops(
            [FromQuery] string? offeringType,
            [FromQuery] string? availability)
        {
            string? ot = string.IsNullOrWhiteSpace(offeringType) ? null : offeringType.Trim();
            string? av = string.IsNullOrWhiteSpace(availability) ? null : availability.Trim();

            if (ot != null && !AllowedOfferingTypes.Contains(ot))
                return BadRequest("offeringType must be PickYourOwn, ReadyPicked, Frozen, or empty.");

            if (av != null && !AllowedAvailabilityValues.Contains(av))
                return BadRequest("availability must be Available, NotAvailable, or empty.");

            var crops = await (
                from c in _context.Crops
                join f in _context.Farms on c.FarmId equals f.Id
                where f.IsActive
                      && (ot == null || c.OfferingType == ot)
                      && (av == null || c.Availability == av)
                orderby c.Category, c.FarmId, c.Id
                select new CropSearchOut
                {
                    Id = c.Id,
                    FarmId = c.FarmId,
                    Category = c.Category,
                    Variety = c.Variety,
                    Availability = c.Availability,
                    PickingCondition = c.PickingCondition,
                    OfferingType = c.OfferingType,
                    OfferingTypes = c.OfferingTypes,
                    YearPlanted = c.YearPlanted,
                    Rootstock = c.Rootstock,
                    Notes = c.Notes,
                    AvailabilityNote = c.AvailabilityNote,
                    InventorySource = c.InventorySource,
                    InventoryExternalId = c.InventoryExternalId,
                    InventoryQuantity = c.InventoryQuantity,
                    InventoryStatus = c.InventoryStatus,
                    InventoryLastSyncAt = c.InventoryLastSyncAt
                }
            ).ToListAsync();

            return Ok(crops);
        }

        // ============================================================
        // GET: /api/Crops/byFarm/{farmId}
        // ✅ This is the endpoint your portal relies on.
        // ============================================================
        [HttpGet("byFarm/{farmId}")]
        public async Task<ActionResult<List<CropSearchOut>>> GetCropsByFarm(int farmId)
        {
            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == farmId);
            if (farm == null || !farm.IsActive)
                return BadRequest("Farm does not exist or is not active.");

            var crops = await _context.Crops
                .Where(c => c.FarmId == farmId)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Id)
                .Select(c => new CropSearchOut
                {
                    Id = c.Id,
                    FarmId = c.FarmId,
                    Category = c.Category,
                    Variety = c.Variety,
                    Availability = c.Availability,
                    PickingCondition = c.PickingCondition,
                    OfferingType = c.OfferingType,
                    OfferingTypes = c.OfferingTypes,
                    YearPlanted = c.YearPlanted,
                    Rootstock = c.Rootstock,
                    Notes = c.Notes,
                    AvailabilityNote = c.AvailabilityNote,
                    InventorySource = c.InventorySource,
                    InventoryExternalId = c.InventoryExternalId,
                    InventoryQuantity = c.InventoryQuantity,
                    InventoryStatus = c.InventoryStatus,
                    InventoryLastSyncAt = c.InventoryLastSyncAt
                })
                .ToListAsync();

            return Ok(crops);
        }

        // ============================================================
        // PUT: /api/Crops/{id}/availability
        // Triggers email queue when flipping into Available
        // ============================================================
        [HttpPut("{id}/availability")]
        public async Task<IActionResult> UpdateAvailability(int id, [FromBody] CropAvailabilityUpdateIn input)
        {
            var crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == id);
            if (crop == null)
                return NotFound();

            var oldAvailability = crop.Availability;

            string? availability = string.IsNullOrWhiteSpace(input.Availability) ? null : input.Availability.Trim();
            if (availability != null && !AllowedAvailabilityValues.Contains(availability))
                return BadRequest("Availability must be 'Available', 'NotAvailable', or null.");

            crop.Availability = availability;

            // optional: allow picking condition update here too
            string? pickingCondition = string.IsNullOrWhiteSpace(input.PickingCondition) ? null : input.PickingCondition.Trim();
            if (pickingCondition != null && !AllowedPickingConditions.Contains(pickingCondition))
                return BadRequest("PickingCondition must be 'Excellent', 'Good', 'Fair', or null.");

            if (!string.Equals(availability, "Available", StringComparison.OrdinalIgnoreCase))
                pickingCondition = null;

            crop.PickingCondition = pickingCondition;

            await _context.SaveChangesAsync();

            // Trigger: only when flipping into Available (NotAvailable/null -> Available)
            var oldWasAvailable = string.Equals(oldAvailability, "Available", StringComparison.OrdinalIgnoreCase);
            var newIsAvailable = string.Equals(availability, "Available", StringComparison.OrdinalIgnoreCase);

            if (!oldWasAvailable && newIsAvailable)
            {
                _alertQueue.Enqueue(new CropBecameAvailableEvent(crop.FarmId, crop.Id));
            }

            return NoContent();
        }

        // ============================================================
        // PUT: /api/Crops/{id}/details
        // ============================================================
        [HttpPut("{id}/details")]
        public async Task<IActionResult> UpdateDetails(int id, [FromBody] CropDetailsUpdateIn input)
        {
            var crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == id);
            if (crop == null)
                return NotFound();

            // OfferingType (legacy)
            if (input.OfferingType != null)
            {
                var offeringType = string.IsNullOrWhiteSpace(input.OfferingType) ? null : input.OfferingType.Trim();
                if (offeringType != null && !AllowedOfferingTypes.Contains(offeringType))
                    return BadRequest("OfferingType must be 'PickYourOwn', 'ReadyPicked', 'Frozen', or null.");

                crop.OfferingType = offeringType;
            }

            // OfferingTypes CSV
            if (input.OfferingTypes != null)
            {
                try
                {
                    crop.OfferingTypes = NormalizeOfferingTypesCsv(input.OfferingTypes);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }

                if (string.IsNullOrWhiteSpace(crop.OfferingType) && !string.IsNullOrWhiteSpace(crop.OfferingTypes))
                    crop.OfferingType = crop.OfferingTypes.Split(',').Select(s => s.Trim()).FirstOrDefault();
            }

            if (input.YearPlanted.HasValue)
            {
                var year = input.YearPlanted.Value;
                if (year < 1800 || year > DateTime.UtcNow.Year + 1)
                    return BadRequest("YearPlanted is out of a reasonable range.");
            }

            crop.YearPlanted = input.YearPlanted;
            crop.Rootstock = string.IsNullOrWhiteSpace(input.Rootstock) ? null : input.Rootstock.Trim();
            crop.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

            if (input.AvailabilityNote != null)
                crop.AvailabilityNote = string.IsNullOrWhiteSpace(input.AvailabilityNote) ? null : input.AvailabilityNote.Trim();

            if (input.InventorySource != null)
                crop.InventorySource = string.IsNullOrWhiteSpace(input.InventorySource) ? null : input.InventorySource.Trim();

            if (input.InventoryExternalId != null)
                crop.InventoryExternalId = string.IsNullOrWhiteSpace(input.InventoryExternalId) ? null : input.InventoryExternalId.Trim();

            if (input.InventoryQuantity.HasValue)
                crop.InventoryQuantity = input.InventoryQuantity;

            if (input.InventoryStatus != null)
                crop.InventoryStatus = string.IsNullOrWhiteSpace(input.InventoryStatus) ? null : input.InventoryStatus.Trim();

            if (input.PickingCondition != null)
            {
                var pc = string.IsNullOrWhiteSpace(input.PickingCondition) ? null : input.PickingCondition.Trim();
                if (pc != null && !AllowedPickingConditions.Contains(pc))
                    return BadRequest("PickingCondition must be 'Excellent', 'Good', 'Fair', or null.");

                // only apply if Available
                if (string.Equals(crop.Availability, "Available", StringComparison.OrdinalIgnoreCase))
                    crop.PickingCondition = pc;
                else
                    crop.PickingCondition = null;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ============================================================
        // Helpers
        // ============================================================
        private async Task<string> ResolveCanonicalCategoryAsync(string inputCategory)
        {
            var raw = (inputCategory ?? "").Trim();
            if (raw.Length == 0) return raw;

            // If your DB doesn't have CropCatalogAliases, return raw.
            var alias = await _context.CropCatalogAliases
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Alias.ToLower() == raw.ToLower());

            if (alias != null && !string.IsNullOrWhiteSpace(alias.CanonicalCategory))
                return alias.CanonicalCategory.Trim();

            return raw;
        }

        private static string? NormalizeOfferingTypesCsv(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return null;

            var parts = csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var p in parts)
            {
                if (!AllowedOfferingTypes.Contains(p))
                    throw new ArgumentException("OfferingTypes contains invalid value: " + p);
            }

            return parts.Count == 0 ? null : string.Join(",", parts);
        }

        private static CropSearchOut ToPublicOut(Crop crop)
        {
            return new CropSearchOut
            {
                Id = crop.Id,
                FarmId = crop.FarmId,
                Category = crop.Category,
                Variety = crop.Variety,
                Availability = crop.Availability,
                PickingCondition = crop.PickingCondition,
                OfferingType = crop.OfferingType,
                OfferingTypes = crop.OfferingTypes,
                YearPlanted = crop.YearPlanted,
                Rootstock = crop.Rootstock,
                Notes = crop.Notes,
                AvailabilityNote = crop.AvailabilityNote,
                InventorySource = crop.InventorySource,
                InventoryExternalId = crop.InventoryExternalId,
                InventoryQuantity = crop.InventoryQuantity,
                InventoryStatus = crop.InventoryStatus,
                InventoryLastSyncAt = crop.InventoryLastSyncAt
            };
        }
    }
}