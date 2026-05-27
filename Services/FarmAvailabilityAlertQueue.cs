using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Agriloco.Api.Services
{
    public sealed class FarmAvailabilityAlertQueue : IFarmAvailabilityAlertQueue
    {
        private readonly Channel<CropBecameAvailableEvent> _channel;

        public FarmAvailabilityAlertQueue()
        {
            _channel = Channel.CreateUnbounded<CropBecameAvailableEvent>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

        public void Enqueue(CropBecameAvailableEvent ev)
        {
            // Best-effort enqueue (should not fail for unbounded)
            _channel.Writer.TryWrite(ev);
        }

        public async Task<CropBecameAvailableEvent> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}