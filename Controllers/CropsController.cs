using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CropsController : ControllerBase
    {
        private readonly AgrilocoContext _context;

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

        public CropsController(AgrilocoContext context)
        {
            _context = context;
        }
        [HttpGet("categories")]
        public ActionResult<List<string>> GetCategories()
        {
            var list = AllowedCategories
                .OrderBy(x => x)
                .ToList();

            return Ok(list);
        }
        // POST: /api/Crops
        [HttpPost]
        public async Task<ActionResult<CropSearchOut>> CreateCrop([FromBody] CropCreateIn input)
        {
            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == input.FarmId);
            if (farm == null || !farm.IsActive)
            {
                return BadRequest("Farm does not exist or is not active.");
            }

            if (string.IsNullOrWhiteSpace(input.Category))
            {
                return BadRequest("Category is required.");
            }

            var category = input.Category.Trim();
            if (!AllowedCategories.Contains(category))
            {
                return BadRequest("Category must be one of: Apple, Strawberry, Blueberry, Raspberry, Corn, Pumpkin.");
            }

            var variety = string.IsNullOrWhiteSpace(input.Variety) ? null : input.Variety.Trim();
            var rootstock = string.IsNullOrWhiteSpace(input.Rootstock) ? null : input.Rootstock.Trim();
            var notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

            string? availability = string.IsNullOrWhiteSpace(input.Availability) ? null : input.Availability.Trim();
            if (availability != null && !AllowedAvailabilityValues.Contains(availability))
            {
                return BadRequest("Availability must be 'Available', 'NotAvailable', or null.");
            }

            string? offeringType = string.IsNullOrWhiteSpace(input.OfferingType) ? null : input.OfferingType.Trim();
            if (offeringType != null && !AllowedOfferingTypes.Contains(offeringType))
            {
                return BadRequest("OfferingType must be 'PickYourOwn', 'ReadyPicked', 'Frozen', or null.");
            }

            if (input.YearPlanted.HasValue)
            {
                var year = input.YearPlanted.Value;
                if (year < 1800 || year > DateTime.UtcNow.Year + 1)
                {
                    return BadRequest("YearPlanted is out of a reasonable range.");
                }
            }

            var crop = new Crop
            {
                FarmId = input.FarmId,
                Category = category,
                Variety = variety,
                Availability = availability,
                OfferingType = offeringType,
                YearPlanted = input.YearPlanted,
                Rootstock = rootstock,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Crops.Add(crop);
            await _context.SaveChangesAsync();

            var result = ToPublicOut(crop);
            return CreatedAtAction(nameof(GetCropById), new { id = crop.Id }, result);
        }

        // GET: /api/Crops/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CropSearchOut>> GetCropById(int id)
        {
            var crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == id);
            if (crop == null)
            {
                return NotFound();
            }

            return Ok(ToPublicOut(crop));
        }

        // GET: /api/Crops/public?offeringType=...&availability=...
        [HttpGet("public")]
        public async Task<ActionResult<List<CropSearchOut>>> GetPublicCrops([FromQuery] string? offeringType, [FromQuery] string? availability)
        {
            string? ot = string.IsNullOrWhiteSpace(offeringType) ? null : offeringType.Trim();
            string? av = string.IsNullOrWhiteSpace(availability) ? null : availability.Trim();

            if (ot != null && !AllowedOfferingTypes.Contains(ot))
            {
                return BadRequest("offeringType must be PickYourOwn, ReadyPicked, Frozen, or empty.");
            }

            if (av != null && !AllowedAvailabilityValues.Contains(av))
            {
                return BadRequest("availability must be Available, NotAvailable, or empty.");
            }

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
                    OfferingType = c.OfferingType,
                    YearPlanted = c.YearPlanted,
                    Rootstock = c.Rootstock,
                    Notes = c.Notes
                }
            ).ToListAsync();

            return Ok(crops);
        }

        // GET: /api/Crops/byFarm/{farmId}
        [HttpGet("byFarm/{farmId}")]
        public async Task<ActionResult<List<CropSearchOut>>> GetCropsByFarm(int farmId)
        {
            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == farmId);
            if (farm == null || !farm.IsActive)
            {
                return BadRequest("Farm does not exist or is not active.");
            }

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
                    OfferingType = c.OfferingType,
                    YearPlanted = c.YearPlanted,
                    Rootstock = c.Rootstock,
                    Notes = c.Notes
                })
                .ToListAsync();

            return Ok(crops);
        }

        // PUT: /api/Crops/{id}/availability
        [HttpPut("{id}/availability")]
        public async Task<IActionResult> UpdateAvailability(int id, [FromBody] CropAvailabilityUpdateIn input)
        {
            var crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == id);
            if (crop == null)
            {
                return NotFound();
            }

            string? availability = string.IsNullOrWhiteSpace(input.Availability) ? null : input.Availability.Trim();
            if (availability != null && !AllowedAvailabilityValues.Contains(availability))
            {
                return BadRequest("Availability must be 'Available', 'NotAvailable', or null.");
            }

            crop.Availability = availability;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: /api/Crops/{id}/details
        [HttpPut("{id}/details")]
        public async Task<IActionResult> UpdateDetails(int id, [FromBody] CropDetailsUpdateIn input)
        {
            var crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == id);
            if (crop == null)
            {
                return NotFound();
            }

            if (input.OfferingType != null)
            {
                var offeringType = string.IsNullOrWhiteSpace(input.OfferingType) ? null : input.OfferingType.Trim();

                if (offeringType != null && !AllowedOfferingTypes.Contains(offeringType))
                {
                    return BadRequest("OfferingType must be 'PickYourOwn', 'ReadyPicked', 'Frozen', or null.");
                }

                crop.OfferingType = offeringType;
            }

            if (input.YearPlanted.HasValue)
            {
                var year = input.YearPlanted.Value;
                if (year < 1800 || year > DateTime.UtcNow.Year + 1)
                {
                    return BadRequest("YearPlanted is out of a reasonable range.");
                }
            }

            crop.YearPlanted = input.YearPlanted;
            crop.Rootstock = string.IsNullOrWhiteSpace(input.Rootstock) ? null : input.Rootstock.Trim();
            crop.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

            await _context.SaveChangesAsync();
            return NoContent();
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
                OfferingType = crop.OfferingType,
                YearPlanted = crop.YearPlanted,
                Rootstock = crop.Rootstock,
                Notes = crop.Notes
            };
        }
    }
}