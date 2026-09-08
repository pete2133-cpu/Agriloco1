using System;
using System.Linq;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvailabilitySubscriptionsController : ControllerBase
    {
        private readonly AgrilocoContext _db;

        public AvailabilitySubscriptionsController(
            AgrilocoContext db)
        {
            _db = db;
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

                bool validSubscriptionType =
                    definitionType.Equals(
                        "Product",
                        StringComparison.OrdinalIgnoreCase) ||
                    definitionType.Equals(
                        "Variety",
                        StringComparison.OrdinalIgnoreCase);

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