using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CropCatalogController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public CropCatalogController(AgrilocoContext context)
        {
            _context = context;
        }

        // GET: /api/CropCatalog/categories
        [HttpGet("categories")]
        public async Task<ActionResult<string[]>> GetCategories()
        {
            var categories = await _context.CropCatalogItems
                .Where(x => x.IsActive && x.Variety == null)
                .Select(x => x.Category)
                .Distinct()
                .OrderBy(x => x)
                .ToArrayAsync();

            return categories;
        }

        // GET: /api/CropCatalog/varieties?category=Apple
        [HttpGet("varieties")]
        public async Task<ActionResult<string[]>> GetVarieties([FromQuery] string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest("category is required.");

            // Find canonical category row by normalized key in memory
            var catKey = NormalizeKey(category);

            var categoryRows = await _context.CropCatalogItems
                .Where(x => x.IsActive && x.Variety == null)
                .ToListAsync();

            var canonicalCategory = categoryRows
                .FirstOrDefault(x => NormalizeKey(x.Category) == catKey)
                ?.Category;

            if (canonicalCategory == null)
                return Array.Empty<string>();

            var varieties = await _context.CropCatalogItems
                .Where(x => x.IsActive && x.Category == canonicalCategory && x.Variety != null)
                .Select(x => x.Variety!)
                .Distinct()
                .OrderBy(x => x)
                .ToArrayAsync();

            return varieties;
        }

        // POST: /api/CropCatalog/category
        [HttpPost("category")]
        public async Task<ActionResult> AddCategory([FromBody] CropCatalogCategoryCreateIn input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Category))
                return BadRequest("Category is required.");

            var display = NormalizeDisplay(input.Category);
            var catKey = NormalizeKey(display);

            // Load existing categories into memory, compare using NormalizeKey
            var existing = await _context.CropCatalogItems
                .Where(x => x.IsActive && x.Variety == null)
                .Select(x => x.Category)
                .ToListAsync();

            var exists = existing.Any(c => NormalizeKey(c) == catKey);

            if (exists)
                return Conflict($"Category already exists (case-insensitive): {display}");

            _context.CropCatalogItems.Add(new CropCatalogItem
            {
                Category = display,
                Variety = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { category = display });
        }

        // POST: /api/CropCatalog/variety
        [HttpPost("variety")]
        public async Task<ActionResult> AddVariety([FromBody] CropCatalogVarietyCreateIn input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Category) || string.IsNullOrWhiteSpace(input.Variety))
                return BadRequest("Category and Variety are required.");

            var catDisplay = NormalizeDisplay(input.Category);
            var varDisplay = NormalizeDisplay(input.Variety);

            var catKey = NormalizeKey(catDisplay);
            var varKey = NormalizeKey(varDisplay);

            // Find canonical category (case-insensitive) in memory
            var categories = await _context.CropCatalogItems
                .Where(x => x.IsActive && x.Variety == null)
                .ToListAsync();

            var canonicalCategory = categories
                .FirstOrDefault(x => NormalizeKey(x.Category) == catKey)
                ?.Category;

            // If category doesn't exist yet, create it
            if (canonicalCategory == null)
            {
                canonicalCategory = catDisplay;

                _context.CropCatalogItems.Add(new CropCatalogItem
                {
                    Category = canonicalCategory,
                    Variety = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            // Check for existing variety under canonical category (case-insensitive) in memory
            var existingVarieties = await _context.CropCatalogItems
                .Where(x => x.IsActive && x.Category == canonicalCategory && x.Variety != null)
                .Select(x => x.Variety!)
                .ToListAsync();

            var exists = existingVarieties.Any(v => NormalizeKey(v) == varKey);

            if (exists)
                return Conflict($"Variety already exists (case-insensitive): {canonicalCategory} / {varDisplay}");

            _context.CropCatalogItems.Add(new CropCatalogItem
            {
                Category = canonicalCategory,
                Variety = varDisplay,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { category = canonicalCategory, variety = varDisplay });
        }

        // -------- Helpers (NOT used inside EF queries) --------

        private static string NormalizeDisplay(string input)
        {
            var s = input.Trim();
            s = string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return s;
        }

        private static string NormalizeKey(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim();
            s = string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return s.ToLowerInvariant();
        }
    }
}