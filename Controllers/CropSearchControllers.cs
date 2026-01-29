using System.Linq;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CropSearchController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public CropSearchController(AgrilocoContext context)
        {
            _context = context;
        }

        // GET: api/CropSearch?category=Apple&region=CA-ON
        [HttpGet]
        public async Task<ActionResult<CropSearchOut[]>> Search(
            [FromQuery] string? category = null,
            [FromQuery] string? region = null)
        {
            var query = _context.Crops
                .Include(c => c.Farm)
                .Where(c => c.Farm != null && c.Farm.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                var catNormalized = category.Trim().ToUpper();
                query = query.Where(c => c.Category.ToUpper() == catNormalized);
            }

            if (!string.IsNullOrWhiteSpace(region))
            {
                var regionNormalized = region.Trim().ToUpper();
                query = query.Where(c => (c.Farm!.RegionCode ?? string.Empty).ToUpper() == regionNormalized);
            }

            query = query
                .OrderBy(c =>
                    c.Availability == "Available" ? 1 :
                    c.Availability == "NotAvailable" ? 2 : 3)
                .ThenBy(c => c.Farm!.Name);

            var results = await query
                .Select(c => new CropSearchOut
                {
                    FarmId = c.FarmId,
                    FarmName = c.Farm!.Name,
                    RegionCode = c.Farm.RegionCode,
                    CropId = c.Id,
                    Category = c.Category,
                    Variety = c.Variety,
                    Availability = c.Availability,
                    ContactMethod1 = c.Farm.ContactMethod1
                })
                .ToArrayAsync();

            return results;
        }
    }
}