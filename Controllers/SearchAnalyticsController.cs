using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchAnalyticsController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public SearchAnalyticsController(AgrilocoContext context)
        {
            _context = context;
        }

        // Example:
        // GET /api/SearchAnalytics/term?term=apple&region=CA-ON&days=30
        [HttpGet("term")]
        public async Task<ActionResult<SearchTermCountOut>> GetTermCount(
            [FromQuery] string term,
            [FromQuery] string region,
            [FromQuery] int days = 30)
        {
            if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(region))
                return BadRequest("term and region are required.");

            if (days < 1) days = 1;
            if (days > 365) days = 365;

            var t = term.Trim().ToLowerInvariant();
            var r = region.Trim().ToUpperInvariant();
            var since = DateTime.UtcNow.AddDays(-days);

            var count = await _context.SearchLogs
                .Where(s => s.RegionCode == r && s.Term == t && s.CreatedAt >= since)
                .CountAsync();

            return Ok(new SearchTermCountOut
            {
                RegionCode = r,
                Term = t,
                Count = count
            });
        }

        // Example:
        // GET /api/SearchAnalytics/top?region=CA-ON&days=30&take=10
        [HttpGet("top")]
        public async Task<ActionResult<List<SearchTermCountOut>>> TopTerms(
            [FromQuery] string region,
            [FromQuery] int days = 30,
            [FromQuery] int take = 10)
        {
            if (string.IsNullOrWhiteSpace(region))
                return BadRequest("region is required.");

            if (days < 1) days = 1;
            if (days > 365) days = 365;

            if (take < 1) take = 1;
            if (take > 100) take = 100;

            var r = region.Trim().ToUpperInvariant();
            var since = DateTime.UtcNow.AddDays(-days);

            var result = await _context.SearchLogs
                .Where(s => s.RegionCode == r && s.CreatedAt >= since)
                .GroupBy(s => s.Term)
                .Select(g => new SearchTermCountOut
                {
                    RegionCode = r,
                    Term = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(take)
                .ToListAsync();

            return Ok(result);
        }
    }
}