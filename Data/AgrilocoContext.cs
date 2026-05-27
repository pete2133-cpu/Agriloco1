using Agriloco.Api.Models;
using Microsoft.EntityFrameworkCore;

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

        // ✅ aliases (you already use this in CropsController)
        public DbSet<CropCatalogAlias> CropCatalogAliases => Set<CropCatalogAlias>();
        public DbSet<Agriloco.Api.Models.CropAvailabilityAlertSubscription> CropAvailabilityAlertSubscriptions => Set<Agriloco.Api.Models.CropAvailabilityAlertSubscription>();

        // ✅ NEW: one-time availability alert subscriptions
        public DbSet<FarmAvailabilityAlertSubscription> FarmAvailabilityAlertSubscriptions
            => Set<FarmAvailabilityAlertSubscription>();
    }
}