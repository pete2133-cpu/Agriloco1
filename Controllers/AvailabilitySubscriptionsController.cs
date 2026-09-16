using System;
using System.Linq;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Agriloco.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Encodings.Web;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvailabilitySubscriptionsController : ControllerBase
    {
        private readonly AgrilocoContext _db;
        private readonly AvailabilityUnsubscribeLinks _links;
        private readonly ILogger<AvailabilitySubscriptionsController> _logger;

        public AvailabilitySubscriptionsController(
            AgrilocoContext db, AvailabilityUnsubscribeLinks links,
            ILogger<AvailabilitySubscriptionsController> logger)
        {
            _db = db;
            _links = links;
            _logger = logger;
        }

        public sealed class BatchRequest
        {
            public int FarmId { get; set; }
            public int[] FarmDefinitionIds { get; set; } = Array.Empty<int>();
            public string Email { get; set; } = "";
        }

        [HttpPost("batch")]
        [EnableRateLimiting("availability-signup")]
        public async Task<IActionResult> SubscribeBatch([FromBody] BatchRequest request)
        {
            var email = (request.Email ?? "").Trim().ToLowerInvariant();
            var ids = (request.FarmDefinitionIds ?? Array.Empty<int>()).Distinct().ToArray();
            if (email.Length > 320 || !IsReasonableEmail(email))
                return BadRequest(new { code = "invalid_email", message = "Please enter a valid email address." });
            if (request.FarmId <= 0 || ids.Length == 0 || ids.Length > 500)
                return BadRequest(new { code = "no_products", message = "No products are available for notifications with this selection." });
            try
            {
                var catalog = (await AvailabilitySignupCatalog.LoadAsync(_db, request.FarmId))
                    .Where(x => x.FarmDefinitionId.HasValue && ids.Contains(x.FarmDefinitionId.Value)).ToList();
                if (catalog.Count != ids.Length)
                    return BadRequest(new { code = "no_products", message = "No products are available for notifications with this selection." });

                // Success means the subscriptions are committed, including existing subscriptions.
                // Future availability emails are sent by DashboardAvailabilityNotifications.
                await _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    _db.ChangeTracker.Clear();
                    await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                    foreach (var id in ids.OrderBy(x => x))
                    {
                        var result = await Subscribe(new CreateAvailabilitySubscriptionRequest { FarmId = request.FarmId, FarmDefinitionId = id, Email = email });
                        if (result is not OkObjectResult) throw new InvalidOperationException("Selected subscription became unavailable.");
                    }
                    await transaction.CommitAsync();
                });
                return Ok(new { success = true });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Availability signup persistence failed for farm {FarmId}, {SelectionCount} selections.", request.FarmId, ids.Length);
                return StatusCode(500, new { code = "signup_failed", message = "We couldn't complete your signup. Please try again." });
            }
        }

        // GET only presents a confirmation, so mail-link scanners do not unsubscribe people.
        [HttpGet("unsubscribe")]
        public IActionResult UnsubscribePage([FromQuery] string token)
        {
            if (_links.Read(token) == null) return BadRequest("This unsubscribe link is invalid.");
            Response.Headers.CacheControl = "no-store";
            Response.Headers["Referrer-Policy"] = "no-referrer";
            return Content("<!doctype html><html><meta name=viewport content='width=device-width,initial-scale=1'><title>Unsubscribe</title>" +
                "<body><h1>Unsubscribe from farm availability emails</h1><form method=post>" +
                "<input type=hidden name=token value='" + HtmlEncoder.Default.Encode(token) +
                "'><button>Unsubscribe</button></form></body></html>", "text/html");
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromForm] string token)
        {
            var recipient = _links.Read(token);
            if (recipient == null) return BadRequest("This unsubscribe link is invalid.");
            var subscriptions = await _db.FarmDefinitionAvailabilitySubscriptions
                .Where(x => x.FarmId == recipient.FarmId && x.Email == recipient.Email && x.Channel == "email" && x.IsActive).ToListAsync();
            foreach (var subscription in subscriptions) subscription.IsActive = false;
            var legacy = await _db.FarmAvailabilityAlertSubscriptions.Where(x => x.FarmId == recipient.FarmId &&
                x.Destination == recipient.Email && x.Channel == "email" && !x.IsFulfilled).ToListAsync();
            foreach (var subscription in legacy) { subscription.IsFulfilled = true; subscription.FulfilledAt = DateTime.UtcNow; }
            await _db.SaveChangesAsync();
            return Content("You have been unsubscribed from this farm's availability emails.", "text/plain");
        }

        // ============================================================
        // POST: api/AvailabilitySubscriptions
        //
        // Creates a public availability email subscription.
        //
        // FarmDefinitionId:
        //     null = All Crops
        //     Product ID = product branch, e.g. Apple
        //     Variety ID = specific variety, e.g. Ambrosia
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Subscribe(
            [FromBody] CreateAvailabilitySubscriptionRequest request)
        {
            if (request.FarmId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "A valid farm is required."
                });
            }

            string email =
                (request.Email ?? "")
                .Trim()
                .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required."
                });
            }

            if (email.Length > 320 ||
                !IsReasonableEmail(email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please enter a valid email address."
                });
            }

            // --------------------------------------------------------
            // Make sure the farm exists.
            // --------------------------------------------------------

            bool farmExists =
                await _db.Farms
                    .AnyAsync(x => x.Id == request.FarmId);

            if (!farmExists)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Farm was not found."
                });
            }

            FarmDefinition? selectedDefinition = null;

            // --------------------------------------------------------
            // If a Product or Variety was selected, make sure it
            // actually belongs to this farm and is public/active.
            //
            // null is valid and means All Crops.
            // --------------------------------------------------------

            if (request.FarmDefinitionId.HasValue)
            {
                selectedDefinition =
                    await _db.FarmDefinitions
                        .FirstOrDefaultAsync(x =>
                            x.Id == request.FarmDefinitionId.Value &&
                            x.FarmId == request.FarmId &&
                            x.IsActive);

                if (selectedDefinition == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "The selected crop or variety was not found."
                    });
                }

                if (!selectedDefinition.IsPublic)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "The selected crop or variety is not public."
                    });
                }

                // The public viewer should subscribe at Product
                // or Variety level, not directly to a Location/Row.
                string definitionType =
                    selectedDefinition.DefinitionType ?? "";

                bool validSubscriptionType = AvailabilitySignupCatalog.IsSelection(selectedDefinition,
                    await _db.FarmDefinitions.AsNoTracking().Where(x => x.FarmId == request.FarmId).ToListAsync());

                if (!validSubscriptionType)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Availability subscriptions must be for a crop/product or variety."
                    });
                }
            }

            // --------------------------------------------------------
            // Check for an existing ACTIVE subscription.
            //
            // This makes repeated clicks safe and gives the customer
            // a friendly response instead of a database error.
            // --------------------------------------------------------

            FarmDefinitionAvailabilitySubscription?
                existingSubscription =
                    await _db
                        .FarmDefinitionAvailabilitySubscriptions
                        .FirstOrDefaultAsync(x =>
                            x.FarmId == request.FarmId &&
                            x.FarmDefinitionId ==
                                request.FarmDefinitionId &&
                            x.Email == email &&
                            x.Channel == "email" &&
                            x.IsActive);

            if (existingSubscription != null)
            {
                return Ok(new
                {
                    success = true,
                    alreadySubscribed = true,
                    subscriptionId =
                        existingSubscription.Id,
                    farmId =
                        existingSubscription.FarmId,
                    farmDefinitionId =
                        existingSubscription.FarmDefinitionId,
                    email =
                        existingSubscription.Email,
                    message =
                        BuildSuccessMessage(
                            selectedDefinition,
                            true)
                });
            }

            // --------------------------------------------------------
            // Create the subscription.
            // --------------------------------------------------------

            var subscription =
                new FarmDefinitionAvailabilitySubscription
                {
                    FarmId = request.FarmId,
                    FarmDefinitionId =
                        request.FarmDefinitionId,
                    Email = email,
                    Channel = "email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastNotifiedAt = null
                };

            _db.FarmDefinitionAvailabilitySubscriptions
                .Add(subscription);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                alreadySubscribed = false,
                subscriptionId = subscription.Id,
                farmId = subscription.FarmId,
                farmDefinitionId =
                    subscription.FarmDefinitionId,
                email = subscription.Email,
                message =
                    BuildSuccessMessage(
                        selectedDefinition,
                        false)
            });
        }

        // ============================================================
        // Basic email validation.
        //
        // This is intentionally lightweight. It catches obvious
        // mistakes without pretending to verify that the mailbox
        // actually exists.
        // ============================================================

        private static bool IsReasonableEmail(string email)
        {
            try
            {
                var address =
                    new System.Net.Mail.MailAddress(email);

                return address.Address.Equals(
                    email,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // Customer-facing response message.
        // ============================================================

        private static string BuildSuccessMessage(
            FarmDefinition? definition,
            bool alreadySubscribed)
        {
            string prefix =
                alreadySubscribed
                    ? "You are already signed up"
                    : "You are signed up";

            if (definition == null)
            {
                return
                    prefix +
                    " for availability updates from this farm.";
            }

            return
                prefix +
                " for " +
                definition.DisplayName +
                " availability updates.";
        }
    }

    // ================================================================
    // Request sent by Unity/public website.
    // ================================================================

    public class CreateAvailabilitySubscriptionRequest
    {
        public int FarmId { get; set; }

        // null = All Crops
        // Product ID = product branch
        // Variety ID = specific variety
        public int? FarmDefinitionId { get; set; }

        public string Email { get; set; } = "";
    }
}
