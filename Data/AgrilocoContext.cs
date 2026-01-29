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

        public DbSet<Member> Members => Set<Member>();
        public DbSet<SearchLog> SearchLogs => Set<SearchLog>();

        public DbSet<Agriloco.Api.Models.CropCatalogItem> CropCatalogItems => Set<Agriloco.Api.Models.CropCatalogItem>();
        public DbSet<CategoryAlias> CategoryAliases => Set<CategoryAlias>();
    }
}