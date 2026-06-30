using Agriloco.Api.Models;
using Microsoft.EntityFrameworkCore;
using Agriloco1.Models.Inventory;

namespace Agriloco.Api.Data
{
    public class AgrilocoContext : DbContext
    {
        public AgrilocoContext(DbContextOptions<AgrilocoContext> options)
            : base(options)
        {
        }

        public DbSet<Farm> Farms { get; set; } = null!;
        public DbSet<Crop> Crops { get; set; } = null!;
        public DbSet<MapCell> MapCells { get; set; } = null!;
        public DbSet<CropColor> CropColors { get; set; } = null!;
        public DbSet<FarmMapLayout> FarmMapLayouts { get; set; } = null!;

        public DbSet<Member> Members => Set<Member>();
        public DbSet<SearchLog> SearchLogs => Set<SearchLog>();

        public DbSet<CropCatalogItem> CropCatalogItems => Set<CropCatalogItem>();

        public DbSet<CropCatalogAlias> CropCatalogAliases => Set<CropCatalogAlias>();

        public DbSet<CropAvailabilityAlertSubscription> CropAvailabilityAlertSubscriptions
            => Set<CropAvailabilityAlertSubscription>();

        public DbSet<FarmAvailabilityAlertSubscription> FarmAvailabilityAlertSubscriptions
            => Set<FarmAvailabilityAlertSubscription>();

        public DbSet<HarvestLot> HarvestLots => Set<HarvestLot>();
        public DbSet<ReceivingLot> ReceivingLots => Set<ReceivingLot>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
        public DbSet<InventoryItemPackage> InventoryItemPackages => Set<InventoryItemPackage>();
       
    }
}