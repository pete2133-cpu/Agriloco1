using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Agriloco.Api.Services
{
    public class FarmAvailabilityAlertWorker : BackgroundService
    {
        private readonly IFarmAvailabilityAlertQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FarmAvailabilityAlertWorker> _logger;

        public FarmAvailabilityAlertWorker(
            IFarmAvailabilityAlertQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<FarmAvailabilityAlertWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FarmAvailabilityAlertWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                CropBecameAvailableEvent ev;

                try
                {
                    ev = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _logger.LogInformation("Dequeued event: FarmId={FarmId}, CropId={CropId}", ev.FarmId, ev.CropId);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AgrilocoContext>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    var subs = await db.FarmAvailabilityAlertSubscriptions
                        .Where(s =>
                            s.FarmId == ev.FarmId &&
                            s.CropId == ev.CropId &&
                            !s.IsFulfilled &&
                            s.Channel == "email")
                        .ToListAsync(stoppingToken);

                    if (subs.Count == 0)
                    {
                        _logger.LogInformation("No pending subscriptions found for FarmId={FarmId}, CropId={CropId}.", ev.FarmId, ev.CropId);
                        continue;
                    }

                    var farm = await db.Farms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == ev.FarmId, stoppingToken);
                    var crop = await db.Crops.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ev.CropId, stoppingToken);

                    var farmName = farm?.Name ?? $"Farm #{ev.FarmId}";
                    var cropName = crop?.Category ?? $"Crop #{ev.CropId}";

                    var subject = $"{farmName}: {cropName} is now AVAILABLE";
                    var body =
                        $"Good news!\n\n" +
                        $"{farmName} just updated {cropName} to AVAILABLE.\n\n" +
                        $"View the farm page:\n" +
                        $"http://localhost:5227/public/farm?id={ev.FarmId}\n";

                    int sentCount = 0;

                    foreach (var s in subs)
                    {
                        await emailSender.SendAsync(s.Destination, subject, body);

                        s.SentAt = DateTime.UtcNow;
                        s.IsFulfilled = true;
                        s.FulfilledAt = DateTime.UtcNow;
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Fulfilled {Count} subscriptions for FarmId={FarmId}, CropId={CropId}.",
                        sentCount, ev.FarmId, ev.CropId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing alert event FarmId={FarmId}, CropId={CropId}.", ev.FarmId, ev.CropId);
                }
            }

            _logger.LogInformation("FarmAvailabilityAlertWorker stopped.");
        }
    }
}