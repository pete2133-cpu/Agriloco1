using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FarmsController : ControllerBase
    {
        private readonly AgrilocoContext _context;
        private readonly IWebHostEnvironment _env;

        public FarmsController(AgrilocoContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // POST: api/Farms/register_minimal
        [HttpPost("register_minimal")]
        public async Task<ActionResult<FarmMinimalOut>> RegisterMinimal([FromBody] FarmMinimalIn input)
        {
            if (string.IsNullOrWhiteSpace(input.Name) ||
                string.IsNullOrWhiteSpace(input.Address) ||
                string.IsNullOrWhiteSpace(input.ContactMethod1) ||
                string.IsNullOrWhiteSpace(input.FruitCategory1))
            {
                return BadRequest("Name, address, contact method, and fruit category are required.");
            }

            // TODO: Replace this with real geocoding later
            var regionCode = "CA-ON";

            // Generate a simple Agriloco ID
            var agrilocoId = "AGR-" + Guid.NewGuid().ToString("N")[..8].ToUpper();

            var farm = new Farm
            {
                AgrilocoId = agrilocoId,
                Name = input.Name,
                Address = input.Address,
                RegionCode = regionCode,
                ContactMethod1 = input.ContactMethod1,
                FruitCategory1 = input.FruitCategory1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Farms.Add(farm);
            await _context.SaveChangesAsync();

            var result = new FarmMinimalOut
            {
                Id = farm.Id,
                AgrilocoId = farm.AgrilocoId,
                Name = farm.Name,
                RegionCode = farm.RegionCode,
                FruitCategory1 = farm.FruitCategory1,
                ContactMethod1 = farm.ContactMethod1
            };

            return CreatedAtAction(nameof(GetPublicFarm), new { id = farm.Id }, result);
        }

        // GET: api/Farms/public
        [HttpGet("public")]
        public async Task<ActionResult<PublicFarmOut[]>> GetPublicFarms()
        {
            var farms = await _context.Farms
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
              .Select(f => new PublicFarmOut
              {
                  Id = f.Id,
                  Name = f.Name,
                  RegionCode = f.RegionCode,
                  FruitCategory1 = f.FruitCategory1,
                  ContactMethod1 = f.ContactMethod1,
                  Address = f.Address,

                  Latitude = f.Latitude,
                  Longitude = f.Longitude,

                  // only if available
              

                  MapImageUrl = f.MapImageUrl
              })
                .ToArrayAsync();

            return farms;
        }

        // GET: api/Farms/{id}/public_single
        [HttpGet("{id}/public_single")]
        public async Task<ActionResult<PublicFarmOut>> GetPublicFarm(int id)
        {
            var farm = await _context.Farms.FindAsync(id);
            if (farm == null || !farm.IsActive)
                return NotFound();

            var result = new PublicFarmOut
            {
                Id = farm.Id,
                Name = farm.Name,
                RegionCode = farm.RegionCode,
                FruitCategory1 = farm.FruitCategory1,
                ContactMethod1 = farm.ContactMethod1,
                Address = farm.Address,

                Latitude = farm.Latitude,
                Longitude = farm.Longitude,

                // Only include these if Farm + DTO actually have them
            

                MapImageUrl = farm.MapImageUrl
            };

            return result;
        }

        // GET: /api/Farms/{id}/map-layout
        [HttpGet("{id}/map-layout")]
        public async Task<IActionResult> GetMapLayout(int id)
        {
            // NOTE: Avoid IsActive dependency here (prevents "no such column" or schema mismatch issues)
            var farmExists = await _context.Farms.AnyAsync(f => f.Id == id);
            if (!farmExists)
                return NotFound("Farm not found.");

            var layout = await _context.FarmMapLayouts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FarmId == id);

            if (layout == null)
                return NotFound("No map layout saved yet.");

            // Return the stored JSON as JSON (not a quoted string)
            return Content(layout.Json, "application/json");
        }

        // POST: /api/Farms/{id}/map-layout
        [HttpPost("{id}/map-layout")]
        [Consumes("application/json")]
        public async Task<IActionResult> SaveMapLayout(int id, [FromBody] JsonElement body)
        {
            // NOTE: Avoid IsActive dependency here too
            var farmExists = await _context.Farms.AnyAsync(f => f.Id == id);
            if (!farmExists)
                return NotFound("Farm not found.");

            // Preserve EXACT JSON from Unity
            var json = body.GetRawText();

            var existing = await _context.FarmMapLayouts
                .FirstOrDefaultAsync(x => x.FarmId == id);

            if (existing == null)
            {
                existing = new FarmMapLayout
                {
                    FarmId = id,
                    Json = json,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.FarmMapLayouts.Add(existing);
            }
            else
            {
                existing.Json = json;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { farmId = id, saved = true, updatedAt = existing.UpdatedAt });
        }

        // NEW: GET api/Farms/{id}/map-image
        // Unity can call this to fetch the farm's background image.
        [HttpGet("{id}/map-image")]
        public async Task<IActionResult> GetMapImage(int id)
        {
            var farm = await _context.Farms.FindAsync(id);
            if (farm == null)
                return NotFound("Farm not found.");

            if (string.IsNullOrWhiteSpace(farm.MapImageUrl))
                return NotFound("No map image uploaded for this farm.");

            // MapImageUrl is like "/uploads/farms/1/basemap.jpg"
            var relativePath = farm.MapImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

            if (!System.IO.File.Exists(physicalPath))
                return NotFound("Map image file not found on server.");

            // Basic content-type guess based on extension
            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(physicalPath, contentType);
        }
    }
}