using Agriloco.Api.Data;
using Agriloco.Api.Security;
using Agriloco.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers;

[ApiController]
[Route("api/DefinitionImages")]
public sealed class DefinitionImagesController(AgrilocoContext db, DefinitionImages images, DefinitionThumbnails thumbnails) : ControllerBase
{
    [HttpGet("public")]
    public Task<IActionResult> Public([FromQuery] int farmId, [FromQuery] int itemId) => Read(farmId, itemId, true);

    [HttpGet("farmer")]
    [FarmMapWrite(IncludeReads = true)]
    public Task<IActionResult> Farmer([FromQuery] int farmId, [FromQuery] int itemId) => Read(farmId, itemId, false);

    private async Task<IActionResult> Read(int farmId, int itemId, bool publicOnly)
    {
        // Check current visibility/ownership before using any cache, including conditional requests.
        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (!await db.Farms.AsNoTracking().AnyAsync(f => f.Id == farmId && f.IsActive)) return NotFound();
        var sources = await images.ResolveAsync(farmId, publicOnly);
        if (!sources.TryGetValue(itemId, out var sourceId)) return NotFound();
        var original = await db.MarketDetails.AsNoTracking().Where(d => d.FarmDefinitionId == sourceId)
            .Select(d => d.Image).FirstOrDefaultAsync();
        if (original == null) return NotFound();
        var thumbnail = await thumbnails.GetAsync(original);
        if (thumbnail == null) return NotFound();
        Response.Headers.ETag = thumbnail.ETag;
        if (Request.GetTypedHeaders().IfNoneMatch?.Any(tag => tag.Tag.Value == thumbnail.ETag || tag.Tag.Value == "*") == true)
            return StatusCode(StatusCodes.Status304NotModified);
        return File(thumbnail.Bytes, "image/webp");
    }
}
