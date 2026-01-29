using System;
using System.Linq;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnityMapController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public UnityMapController(AgrilocoContext context)
        {
            _context = context;
        }

        // GET: /api/UnityMap/cells?farmId=1
        [HttpGet("cells")]
        public async Task<IActionResult> GetUnityCells([FromQuery] int farmId)
        {
            // Pull cells + crop info
            var cells = await _context.MapCells
                .AsNoTracking()
                .Where(m => m.FarmId == farmId)
                .Include(m => m.Crop)
                .Select(m => new
                {
                    id = m.Id,
                    farmId = m.FarmId,
                    gridX = m.GridX,
                    gridY = m.GridY,
                    cropId = m.CropId,
                    featureType = m.FeatureType,

                    // Crop details (null-safe)
                    category = m.Crop != null ? m.Crop.Category : null,
                    variety = m.Crop != null ? m.Crop.Variety : null,
                    availability = m.Crop != null ? m.Crop.Availability : null,

                    // Unity needs a color string
                    colorCode = m.CropId.HasValue ? ColorForCropId(m.CropId.Value) : "#000000"
                })
                .ToListAsync();

            return Ok(cells);
        }

        private static string ColorForCropId(int cropId)
        {
            string[] palette =
            {
                "#FF0000", // 1
                "#FFFF00", // 2
                "#00BFFF", // 3
                "#00FF00", // 4
                "#FF00FF", // 5
                "#FFA500", // 6
                "#8A2BE2", // 7
                "#00FFFF", // 8
                "#A52A2A", // 9
                "#808080"  // 10
            };

            var idx = (cropId - 1) % palette.Length;
            if (idx < 0) idx = 0;
            return palette[idx];
        }
    }
}