using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CropCatalogAliasesController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public CropCatalogAliasesController(AgrilocoContext context)
        {
            _context = context;
        }

        // GET: /api/CropCatalogAliases
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.CropCatalogAliases
                .AsNoTracking()
                .OrderBy(a => a.CanonicalCategory)
                .ThenBy(a => a.Alias)
                .ToListAsync();

            return Ok(items);
        }

        // POST: /api/CropCatalogAliases
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CropCatalogAliasCreateIn dto)
        {
            var canonical = (dto.CanonicalCategory ?? "").Trim();
            var alias = (dto.Alias ?? "").Trim();

            if (string.IsNullOrWhiteSpace(canonical))
                return BadRequest("CanonicalCategory is required.");

            if (string.IsNullOrWhiteSpace(alias))
                return BadRequest("Alias is required.");

            var exists = await _context.CropCatalogAliases
                .AnyAsync(a => a.Alias.ToLower() == alias.ToLower());

            if (exists)
                return Conflict("Alias already exists.");

            var entity = new CropCatalogAlias
            {
                CanonicalCategory = canonical,
                Alias = alias
            };

            _context.CropCatalogAliases.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }
    }
}