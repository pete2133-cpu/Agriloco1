namespace Agriloco.Api.Services
{
    // Simple event payload: which farm + which crop flipped into Available
    public sealed class CropBecameAvailableEvent
    {
        public int FarmId { get; }
        public int CropId { get; }

        public CropBecameAvailableEvent(int farmId, int cropId)
        {
            FarmId = farmId;
            CropId = cropId;
        }
    }
}