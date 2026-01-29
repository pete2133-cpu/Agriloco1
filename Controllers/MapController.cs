using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public MapController(AgrilocoContext context)
        {
            _context = context;
        }

        // GET: /api/Map/cells?farmId=1
        [HttpGet("cells")]
        public async Task<ActionResult<List<MapCellOut>>> GetCells([FromQuery] int farmId)
        {
            if (farmId <= 0)
                return BadRequest("farmId is required.");

            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == farmId);
            if (farm == null || !farm.IsActive)
                return BadRequest("Farm does not exist or is not active.");

            var cells = await _context.MapCells
                .Include(m => m.Crop)
                .Where(m => m.FarmId == farmId)
                .OrderBy(m => m.GridY)
                .ThenBy(m => m.GridX)
                .Select(m => new MapCellOut
                {
                    Id = m.Id,
                    FarmId = m.FarmId,
                    CropId = m.CropId,
                    Category = m.Crop != null ? m.Crop.Category : null,
                    Variety = m.Crop != null ? m.Crop.Variety : null,
                    Availability = m.Crop != null ? m.Crop.Availability : null,
                    GridX = m.GridX,
                    GridY = m.GridY,
                    FeatureType = m.FeatureType
                })
                .ToListAsync();

            return Ok(cells);
        }

        // POST: /api/Map/cells
        [HttpPost("cells")]
        public async Task<ActionResult<MapCellOut>> CreateCell([FromBody] MapCellCreateIn input)
        {
            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == input.FarmId);
            if (farm == null || !farm.IsActive)
                return BadRequest("Farm does not exist or is not active.");

            Crop? crop = null;
            if (input.CropId.HasValue)
            {
                crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == input.CropId.Value);
                if (crop == null)
                    return BadRequest("CropId does not exist.");

                if (crop.FarmId != input.FarmId)
                    return BadRequest("CropId does not belong to this farm.");
            }

            // Prevent duplicate tile coordinates for same farm
            var existing = await _context.MapCells.FirstOrDefaultAsync(m =>
                m.FarmId == input.FarmId &&
                m.GridX == input.GridX &&
                m.GridY == input.GridY);

            if (existing != null)
            {
                // Update existing instead of failing (nice for painting)
                existing.CropId = input.CropId;
                existing.FeatureType = string.IsNullOrWhiteSpace(input.FeatureType) ? "Crop" : input.FeatureType.Trim();
                await _context.SaveChangesAsync();

                var updatedOut = new MapCellOut
                {
                    Id = existing.Id,
                    FarmId = existing.FarmId,
                    CropId = existing.CropId,
                    Category = crop?.Category,
                    Variety = crop?.Variety,
                    Availability = crop?.Availability,
                    GridX = existing.GridX,
                    GridY = existing.GridY,
                    FeatureType = existing.FeatureType
                };

                return Ok(updatedOut);
            }

            var cell = new MapCell
            {
                FarmId = input.FarmId,
                CropId = input.CropId,
                GridX = input.GridX,
                GridY = input.GridY,
                FeatureType = string.IsNullOrWhiteSpace(input.FeatureType) ? "Crop" : input.FeatureType.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.MapCells.Add(cell);
            await _context.SaveChangesAsync();

            var result = new MapCellOut
            {
                Id = cell.Id,
                FarmId = cell.FarmId,
                CropId = cell.CropId,
                Category = crop?.Category,
                Variety = crop?.Variety,
                Availability = crop?.Availability,
                GridX = cell.GridX,
                GridY = cell.GridY,
                FeatureType = cell.FeatureType
            };

            return Ok(result);
        }

        // DELETE: /api/Map/cells?farmId=1&gridX=10&gridY=20
        [HttpDelete("cells")]
        public async Task<IActionResult> DeleteCell([FromQuery] int farmId, [FromQuery] int gridX, [FromQuery] int gridY)
        {
            if (farmId <= 0)
                return BadRequest("farmId is required.");

            var cell = await _context.MapCells.FirstOrDefaultAsync(m =>
                m.FarmId == farmId && m.GridX == gridX && m.GridY == gridY);

            if (cell == null)
                return NotFound();

            _context.MapCells.Remove(cell);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}