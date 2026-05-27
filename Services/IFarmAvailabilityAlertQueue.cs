using System.Threading;
using System.Threading.Tasks;

namespace Agriloco.Api.Services
{
    public interface IFarmAvailabilityAlertQueue
    {
        void Enqueue(CropBecameAvailableEvent ev);
        Task<CropBecameAvailableEvent> DequeueAsync(CancellationToken cancellationToken);
    }
}