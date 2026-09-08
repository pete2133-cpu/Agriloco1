using Agriloco.Api.Data;
using Agriloco.Api.Services;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer
{
    public class DashboardModel : PageModel
    {
        private readonly AgrilocoContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(
            AgrilocoContext db,
            IWebHostEnvironment environment,
            IEmailSender emailSender,
            ILogger<DashboardModel> logger)
        {
            _db = db;
            _environment = environment;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        public string FarmName { get; set; } = "Agriloco Farm";

        public string? MapImageUrl { get; set; }

        public DateTime? MapImageUploadedAt { get; set; }

        [BindProperty]
        public IFormFile? BasemapUpload { get; set; }

        public List<FarmDefinition> FarmDefinitions { get; set; } = new();

        public List<AvailabilityChannel> AvailabilityChannels { get; set; } = new();

        public Dictionary<int, HashSet<int>> EnabledChannels { get; set; } = new();

        public List<string> StatusOptions { get; } = new()
        {
            "Coming Soon",
            "Early Season",
            "Peak Season",
            "Late Season",
            "Available",
            "Limited Availability",
            "Out of Season",
            "Unavailable"
        };

        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        // ============================================================
        // STATUS
        // ============================================================

        public async Task<IActionResult> OnPostStatusAsync(
            int farmId,
            int farmDefinitionId,
            string status)
        {
            FarmId = farmId;

            var item = await _db.FarmDefinitions
                .FirstOrDefaultAsync(x =>
                    x.Id == farmDefinitionId &&
                    x.FarmId == FarmId);

            if (item != null &&
                StatusOptions.Contains(status))
            {
                // Save the OLD status before changing anything.
                //
                // This allows us to detect an actual transition:
                //
                // Coming Soon -> Available
                //
                // rather than simply seeing that the new value
                // happens to be Available.

                string oldStatus =
                    item.Status ?? "";

                string newStatus =
                    status;

                bool wasAvailable =
                    IsCustomerAvailableStatus(
                        oldStatus);

                bool isNowAvailable =
                    IsCustomerAvailableStatus(
                        newStatus);

                bool enteredAvailability =
                    !wasAvailable &&
                    isNowAvailable;

                bool canGenerateAvailabilityAlert =
                    IsAlertableDefinitionType(
                        item.DefinitionType);

                // Save the dashboard change first.
                //
                // The status change must succeed even if the
                // email server is temporarily unavailable.

                item.Status =
                    newStatus;

                item.UpdatedAt =
                    DateTime.Now;

                await _db.SaveChangesAsync();

                // Only Product and Variety definitions generate
                // customer availability notifications.
                //
                // Locations/Rows do not generate emails.

                if (enteredAvailability &&
                    canGenerateAvailabilityAlert)
                {
                    try
                    {
                        await SendAvailabilityNotificationsAsync(
                            item);
                    }
                    catch (Exception exception)
                    {
                        // Do not undo or block the farmer's
                        // status update because of an email problem.

                        _logger.LogError(
                            exception,
                            "Availability email notification failed. " +
                            "FarmId: {FarmId}, " +
                            "FarmDefinitionId: {FarmDefinitionId}, " +
                            "Name: {DisplayName}",
                            item.FarmId,
                            item.Id,
                            item.DisplayName);
                    }
                }
            }

            return RedirectToPage(
                new
                {
                    farmId = FarmId
                });
        }

        // ============================================================
        // CUSTOMER AVAILABLE STATUS
        // ============================================================

        private bool IsCustomerAvailableStatus(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(
                status))
            {
                return false;
            }

            return
                status.Equals(
                    "Available",
                    StringComparison.OrdinalIgnoreCase) ||

                status.Equals(
                    "Peak Season",
                    StringComparison.OrdinalIgnoreCase) ||

                status.Equals(
                    "Limited Availability",
                    StringComparison.OrdinalIgnoreCase) ||

                status.Equals(
                    "Late Season",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // ALERTABLE DEFINITION TYPE
        // ============================================================

        private bool IsAlertableDefinitionType(
            string? definitionType)
        {
            if (string.IsNullOrWhiteSpace(
                definitionType))
            {
                return false;
            }

            return
                definitionType.Equals(
                    "Product",
                    StringComparison.OrdinalIgnoreCase) ||

                definitionType.Equals(
                    "Variety",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // SEND AVAILABILITY NOTIFICATIONS
        // ============================================================

        private async Task SendAvailabilityNotificationsAsync(
            FarmDefinition availableItem)
        {
            // --------------------------------------------------------
            // Determine which subscriptions match this item.
            //
            // Example:
            //
            // Ambrosia becomes Available.
            //
            // Matching subscription scopes:
            //
            // NULL       = All Crops
            // Apple      = parent Product
            // Ambrosia   = exact Variety
            // --------------------------------------------------------

            var matchingDefinitionIds =
                await GetSubscriptionAncestorIdsAsync(
                    availableItem);

            var subscriptions =
                await _db
                    .FarmDefinitionAvailabilitySubscriptions
                    .Where(x =>
                        x.FarmId ==
                            availableItem.FarmId &&

                        x.IsActive &&

                        x.Channel ==
                            "email" &&

                        (
                            x.FarmDefinitionId == null ||

                            (
                                x.FarmDefinitionId.HasValue &&
                                matchingDefinitionIds.Contains(
                                    x.FarmDefinitionId.Value)
                            )
                        ))
                    .ToListAsync();

            if (subscriptions.Count == 0)
            {
                return;
            }

            // --------------------------------------------------------
            // DEDUPLICATE EMAIL ADDRESSES
            //
            // A customer could be subscribed to:
            //
            // All Crops
            // Apple
            // Ambrosia
            //
            // We only want ONE Ambrosia email for that event.
            // --------------------------------------------------------

            var emailGroups =
                subscriptions
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.Email))
                    .GroupBy(
                        x => x.Email
                            .Trim()
                            .ToLowerInvariant())
                    .ToList();

            if (emailGroups.Count == 0)
            {
                return;
            }

            string farmName =
                await _db.Farms
                    .AsNoTracking()
                    .Where(x =>
                        x.Id ==
                            availableItem.FarmId)
                    .Select(x =>
                        x.Name)
                    .FirstOrDefaultAsync()
                    ?? "Agriloco Farm";

            string subject =
                $"{availableItem.DisplayName} is now available at {farmName}";

            string body =
                BuildAvailabilityEmailBody(
                    farmName,
                    availableItem);

            DateTime notificationTime =
                DateTime.UtcNow;

            foreach (var emailGroup in emailGroups)
            {
                string email =
                    emailGroup.Key;

                try
                {
                    await _emailSender.SendAsync(
                        email,
                        subject,
                        body);

                    // Update every matching subscription belonging
                    // to this email address.
                    //
                    // The subscriptions remain active.

                    foreach (var subscription in emailGroup)
                    {
                        subscription.LastNotifiedAt =
                            notificationTime;
                    }

                    // Save after each successful recipient.
                    //
                    // If a later email fails, successful sends
                    // still have their notification time recorded.

                    await _db.SaveChangesAsync();

                    _logger.LogInformation(
                        "Availability email sent. " +
                        "FarmId: {FarmId}, " +
                        "FarmDefinitionId: {FarmDefinitionId}, " +
                        "Name: {DisplayName}, " +
                        "Email: {Email}",
                        availableItem.FarmId,
                        availableItem.Id,
                        availableItem.DisplayName,
                        email);
                }
                catch (Exception exception)
                {
                    // Continue to other subscribers if one address
                    // or SMTP send fails.

                    _logger.LogError(
                        exception,
                        "Could not send availability email. " +
                        "FarmId: {FarmId}, " +
                        "FarmDefinitionId: {FarmDefinitionId}, " +
                        "Email: {Email}",
                        availableItem.FarmId,
                        availableItem.Id,
                        email);
                }
            }
        }

        // ============================================================
        // SUBSCRIPTION ANCESTORS
        // ============================================================

        private async Task<List<int>>
            GetSubscriptionAncestorIdsAsync(
                FarmDefinition item)
        {
            var result =
                new List<int>();

            var visited =
                new HashSet<int>();

            // Exact item always matches.
            //
            // Example:
            // Ambrosia subscriber.

            result.Add(
                item.Id);

            visited.Add(
                item.Id);

            int? parentId =
                item.ParentFarmDefinitionId;

            int safety =
                0;

            while (
                parentId.HasValue &&
                safety < 50)
            {
                var parent =
                    await _db.FarmDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.Id ==
                                parentId.Value &&
                            x.FarmId ==
                                item.FarmId &&
                            x.IsActive);

                if (parent == null)
                {
                    break;
                }

                if (!visited.Add(
                    parent.Id))
                {
                    // Protect against malformed/circular
                    // hierarchy data.

                    break;
                }

                // Only Product/Variety ancestors are relevant
                // subscription scopes.
                //
                // Example:
                // Ambrosia -> Apple

                if (IsAlertableDefinitionType(
                    parent.DefinitionType))
                {
                    result.Add(
                        parent.Id);
                }

                parentId =
                    parent.ParentFarmDefinitionId;

                safety++;
            }

            return result;
        }

        // ============================================================
        // EMAIL BODY
        // ============================================================

        private string BuildAvailabilityEmailBody(
            string farmName,
            FarmDefinition availableItem)
        {
            return
                $"Good news!\n\n" +
                $"{availableItem.DisplayName} is now available " +
                $"at {farmName}.\n\n" +
                $"Current status: {availableItem.Status}\n\n" +
                $"Availability can change during the day, " +
                $"so please check the farm's current Agriloco map " +
                $"before visiting.\n\n" +
                $"{farmName}";
        }

        // ============================================================
        // AVAILABILITY CHANNEL
        // ============================================================

        public async Task<IActionResult> OnPostChannelAsync(
            int farmId,
            int farmDefinitionId,
            int availabilityChannelId,
            bool isEnabled)
        {
            FarmId = farmId;

            var itemExists = await _db.FarmDefinitions
                .AnyAsync(x =>
                    x.Id == farmDefinitionId &&
                    x.FarmId == FarmId);

            var channelExists = await _db.AvailabilityChannels
                .AnyAsync(x =>
                    x.Id == availabilityChannelId &&
                    x.IsActive);

            if (!itemExists || !channelExists)
            {
                return RedirectToPage(
                    new
                    {
                        farmId = FarmId
                    });
            }

            var link = await _db.FarmDefinitionChannels
                .FirstOrDefaultAsync(x =>
                    x.FarmDefinitionId == farmDefinitionId &&
                    x.AvailabilityChannelId == availabilityChannelId);

            if (link == null)
            {
                link = new FarmDefinitionChannel
                {
                    FarmDefinitionId = farmDefinitionId,
                    AvailabilityChannelId = availabilityChannelId,
                    IsEnabled = isEnabled,
                    UpdatedAt = DateTime.Now
                };

                _db.FarmDefinitionChannels.Add(link);
            }
            else
            {
                link.IsEnabled = isEnabled;
                link.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            return RedirectToPage(
                new
                {
                    farmId = FarmId
                });
        }

        // ============================================================
        // PUBLIC VISIBILITY
        // ============================================================

        public async Task<IActionResult> OnPostPublicAsync(
            int farmId,
            int farmDefinitionId,
            bool isPublic)
        {
            FarmId = farmId;

            var item = await _db.FarmDefinitions
                .FirstOrDefaultAsync(x =>
                    x.Id == farmDefinitionId &&
                    x.FarmId == FarmId);

            if (item != null)
            {
                item.IsPublic = isPublic;
                item.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();
            }

            return RedirectToPage(
                new
                {
                    farmId = FarmId
                });
        }

        // ============================================================
        // BASEMAP UPLOAD
        // ============================================================

        public async Task<IActionResult> OnPostBasemapAsync(
            int farmId)
        {
            FarmId = farmId;

            var farm = await _db.Farms
                .FirstOrDefaultAsync(x =>
                    x.Id == FarmId);

            if (farm == null)
            {
                return NotFound();
            }

            if (BasemapUpload == null ||
                BasemapUpload.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please choose an image to upload.");

                await LoadPageAsync();

                return Page();
            }

            var extension = Path
                .GetExtension(BasemapUpload.FileName)
                .ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "",
                    "Please upload a JPG, JPEG, PNG or WebP image.");

                await LoadPageAsync();

                return Page();
            }

            // Maximum basemap size: 20 MB
            const long maxFileSize =
                20 * 1024 * 1024;

            if (BasemapUpload.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "",
                    "The basemap image must be 20 MB or smaller.");

                await LoadPageAsync();

                return Page();
            }

            // ========================================================
            // CREATE FARM UPLOAD FOLDER
            //
            // wwwroot/uploads/farms/{FarmId}/
            // ========================================================

            var relativeFolder = Path.Combine(
                "uploads",
                "farms",
                FarmId.ToString());

            var physicalFolder = Path.Combine(
                _environment.WebRootPath,
                relativeFolder);

            Directory.CreateDirectory(
                physicalFolder);

            // ========================================================
            // USE PREDICTABLE BASEMAP NAME
            // ========================================================

            var fileName =
                "basemap" + extension;

            var physicalPath = Path.Combine(
                physicalFolder,
                fileName);

            // ========================================================
            // REMOVE OLD BASEMAP IF EXTENSION CHANGED
            // ========================================================

            if (!string.IsNullOrWhiteSpace(
                farm.MapImageUrl))
            {
                var oldRelativePath =
                    farm.MapImageUrl
                        .TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar);

                var oldPhysicalPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        oldRelativePath);

                if (System.IO.File.Exists(
                        oldPhysicalPath) &&
                    !string.Equals(
                        oldPhysicalPath,
                        physicalPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    System.IO.File.Delete(
                        oldPhysicalPath);
                }
            }

            // ========================================================
            // SAVE IMAGE
            // ========================================================

            await using (
                var stream = new FileStream(
                    physicalPath,
                    FileMode.Create))
            {
                await BasemapUpload
                    .CopyToAsync(stream);
            }

            // ========================================================
            // SAVE URL TO FARM
            // ========================================================

            farm.MapImageUrl =
                $"/uploads/farms/{FarmId}/{fileName}";

            farm.MapImageUploadedAt =
                DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return RedirectToPage(
                new
                {
                    farmId = FarmId
                });
        }

        // ============================================================
        // LOAD PAGE
        // ============================================================

        private async Task LoadPageAsync()
        {
            var farm = await _db.Farms
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == FarmId);

            if (farm != null)
            {
                FarmName = farm.Name;

                MapImageUrl =
                    farm.MapImageUrl;

                MapImageUploadedAt =
                    farm.MapImageUploadedAt;
            }

            // ========================================================
            // LOAD FARM DEFINITIONS
            // ========================================================

            var rawDefinitions =
                await _db.FarmDefinitions
                    .AsNoTracking()
                    .Where(x =>
                        x.FarmId == FarmId &&
                        x.IsActive)
                    .ToListAsync();

            FarmDefinitions =
                OrderHierarchy(
                    rawDefinitions);

            // ========================================================
            // LOAD AVAILABILITY CHANNELS
            // ========================================================

            AvailabilityChannels =
                await _db.AvailabilityChannels
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive)
                    .OrderBy(x =>
                        x.SortOrder)
                    .ThenBy(x =>
                        x.Name)
                    .ToListAsync();

            var definitionIds =
                FarmDefinitions
                    .Select(x =>
                        x.Id)
                    .ToList();

            var links =
                await _db.FarmDefinitionChannels
                    .AsNoTracking()
                    .Where(x =>
                        definitionIds.Contains(
                            x.FarmDefinitionId) &&
                        x.IsEnabled)
                    .ToListAsync();

            EnabledChannels =
                links
                    .GroupBy(x =>
                        x.FarmDefinitionId)
                    .ToDictionary(
                        x => x.Key,
                        x => x
                            .Select(y =>
                                y.AvailabilityChannelId)
                            .ToHashSet());
        }

        // ============================================================
        // HIERARCHY ORDERING
        // ============================================================

        private List<FarmDefinition> OrderHierarchy(
            List<FarmDefinition> source)
        {
            var result =
                new List<FarmDefinition>();

            var visited =
                new HashSet<int>();

            void AddChildren(
                int? parentId)
            {
                var children =
                    source
                        .Where(x =>
                            x.ParentFarmDefinitionId ==
                            parentId)
                        .OrderBy(x =>
                            x.SortOrder)
                        .ThenBy(x =>
                            x.DisplayName)
                        .ToList();

                foreach (
                    var child
                    in children)
                {
                    if (!visited.Add(
                        child.Id))
                    {
                        continue;
                    }

                    result.Add(
                        child);

                    AddChildren(
                        child.Id);
                }
            }

            AddChildren(
                null);

            foreach (
                var item
                in source
                    .OrderBy(x =>
                        x.SortOrder)
                    .ThenBy(x =>
                        x.DisplayName))
            {
                if (visited.Add(
                    item.Id))
                {
                    result.Add(
                        item);
                }
            }

            return result;
        }

        // ============================================================
        // DISPLAY HELPERS
        // ============================================================

        public int GetDepth(
            FarmDefinition item)
        {
            var depth =
                0;

            var parentId =
                item.ParentFarmDefinitionId;

            var safety =
                0;

            while (
                parentId.HasValue &&
                safety < 50)
            {
                var parent =
                    FarmDefinitions
                        .FirstOrDefault(x =>
                            x.Id ==
                            parentId.Value);

                if (parent == null)
                {
                    break;
                }

                depth++;

                parentId =
                    parent.ParentFarmDefinitionId;

                safety++;
            }

            return depth;
        }

        public bool IsChannelEnabled(
            int farmDefinitionId,
            int channelId)
        {
            return
                EnabledChannels.TryGetValue(
                    farmDefinitionId,
                    out var channels)
                &&
                channels.Contains(
                    channelId);
        }
    }
}