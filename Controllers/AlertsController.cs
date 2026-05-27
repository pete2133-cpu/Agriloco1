using System;
using System.Linq;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Agriloco.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly AgrilocoContext _db;
        private readonly IFarmAvailabilityAlertQueue _queue;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(AgrilocoContext db, IFarmAvailabilityAlertQueue queue, ILogger<AlertsController> logger)
        {
            _db = db;
            _queue = queue;
            _logger = logger;
        }

        public class SubscribeCropIn
        {
            public int FarmId { get; set; }
            public int CropId { get; set; }
            public string Email { get; set; } = "";
        }

        // POST: /api/Alerts/subscribe-crop
        [HttpPost("subscribe-crop")]
        public async Task<IActionResult> SubscribeCrop([FromBody] SubscribeCropIn input)
        {
            if (input == null) return BadRequest("Missing body.");
            if (input.FarmId <= 0) return BadRequest("FarmId is required.");
            if (input.CropId <= 0) return BadRequest("CropId is required.");
            if (string.IsNullOrWhiteSpace(input.Email)) return BadRequest("Email is required.");

            var email = input.Email.Trim().ToLowerInvariant();

            var farmExists = await _db.Farms.AnyAsync(f => f.Id == input.FarmId && f.IsActive);
            if (!farmExists) return BadRequest("Farm not found or inactive.");

            var cropExists = await _db.Crops.AnyAsync(c => c.Id == input.CropId && c.FarmId == input.FarmId);
            if (!cropExists) return BadRequest("Crop not found for that farm.");

            var already = await _db.FarmAvailabilityAlertSubscriptions
                .AnyAsync(s =>
                    s.FarmId == input.FarmId &&
                    s.CropId == input.CropId &&
                    s.Channel == "email" &&
                    s.Destination == email &&
                    !s.IsFulfilled);

            if (already)
                return Ok(new { status = "already_subscribed" });

            var row = new FarmAvailabilityAlertSubscription
            {
                FarmId = input.FarmId,
                CropId = input.CropId,
                Channel = "email",
                Destination = email,
                IsFulfilled = false,
                CreatedAt = DateTime.UtcNow,
                FulfilledAt = null
            };

            _db.FarmAvailabilityAlertSubscriptions.Add(row);
            await _db.SaveChangesAsync();

            return Ok(new { status = "subscribed" });
        }

        // GET: /api/Alerts/debug/list?farmId=1&cropId=1
        [HttpGet("debug/list")]
        public async Task<IActionResult> DebugList([FromQuery] int farmId, [FromQuery] int cropId)
        {
            var rows = await _db.FarmAvailabilityAlertSubscriptions
                .AsNoTracking()
                .Where(s => s.FarmId == farmId && s.CropId == cropId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(rows);
        }

        // ✅ NEW: POST /api/Alerts/debug/enqueue?farmId=1&cropId=1
        // This forces an event into the queue without touching crop availability.
        [HttpPost("debug/enqueue")]
        public IActionResult DebugEnqueue([FromQuery] int farmId, [FromQuery] int cropId)
        {
            if (farmId <= 0 || cropId <= 0) return BadRequest("farmId and cropId required.");

            _queue.Enqueue(new CropBecameAvailableEvent(farmId, cropId));
            _logger.LogInformation("DEBUG enqueue: FarmId={FarmId}, CropId={CropId}", farmId, cropId);

            return Ok(new { status = "enqueued", farmId, cropId });
        }
    }
}