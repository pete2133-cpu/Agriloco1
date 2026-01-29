using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaletteController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        // Same palette order used everywhere:
        // 1 Red, 2 Yellow, 3 Green, ...
        private static readonly string[] Palette = new[]
        {
            "#FF0000", // 1
            "#FFFF00", // 2
            "#00FF00", // 3
            "#00BFFF", // 4
            "#FF00FF", // 5
            "#FFA500", // 6
            "#8A2BE2", // 7
            "#00FFFF", // 8
            "#FF69B4", // 9
            "#A0522D"  // 10
        };

        public PaletteController(AgrilocoContext context)
        {
            _context = context;
        }

        // GET: /api/Palette?farmId=1
        [HttpGet]
        public async Task<ActionResult<List<PaletteEntryOut>>> GetPalette([FromQuery] int farmId)
        {
            if (farmId <= 0)
                return BadRequest("farmId is required.");

            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == farmId);
            if (farm == null || !farm.IsActive)
                return BadRequest("Farm does not exist or is not active.");

            // IMPORTANT: We must return each crop's REAL Id
            // and assign a consistent color index based on CreatedAt then Id.
            var crops = await _context.Crops
                .Where(c => c.FarmId == farmId)
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .ToListAsync();

            var result = new List<PaletteEntryOut>();

            for (int i = 0; i < crops.Count; i++)
            {
                var crop = crops[i];
                int colorIndex = i + 1;
                string colorCode = Palette[(colorIndex - 1) % Palette.Length];

                result.Add(new PaletteEntryOut
                {
                    CropId = crop.Id,
                    ColorIndex = colorIndex,
                    ColorCode = colorCode,
                    Category = crop.Category,
                    Variety = crop.Variety
                });
            }

            return Ok(result);
        }
    }
}