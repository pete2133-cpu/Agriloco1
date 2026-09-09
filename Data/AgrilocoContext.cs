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

        // ============================================================
        // EXISTING FARM / SEARCH / UNITY DATA
        // ============================================================

        public DbSet<Farm> Farms { get; set; } = null!;
        public DbSet<Crop> Crops { get; set; } = null!;
        public DbSet<MapCell> MapCells { get; set; } = null!;
        public DbSet<CropColor> CropColors { get; set; } = null!;
        public DbSet<FarmMapLayout> FarmMapLayouts { get; set; } = null!;
   
        public DbSet<Member> Members => Set<Member>();
        public DbSet<SearchLog> SearchLogs => Set<SearchLog>();

        public DbSet<CropCatalogItem> CropCatalogItems
            => Set<CropCatalogItem>();

        public DbSet<CropCatalogAlias> CropCatalogAliases
            => Set<CropCatalogAlias>();

        public DbSet<CropAvailabilityAlertSubscription>
            CropAvailabilityAlertSubscriptions
            => Set<CropAvailabilityAlertSubscription>();

        public DbSet<FarmAvailabilityAlertSubscription>
            FarmAvailabilityAlertSubscriptions
            => Set<FarmAvailabilityAlertSubscription>();
        public DbSet<FarmDefinitionAvailabilitySubscription>
    FarmDefinitionAvailabilitySubscriptions
        => Set<FarmDefinitionAvailabilitySubscription>();

        // ============================================================
        // HARVEST / RECEIVING / INVENTORY
        // ============================================================

        public DbSet<HarvestLot> HarvestLots
            => Set<HarvestLot>();

        public DbSet<ReceivingLot> ReceivingLots
            => Set<ReceivingLot>();

        public DbSet<Supplier> Suppliers
            => Set<Supplier>();

        public DbSet<InventoryItem> InventoryItems
            => Set<InventoryItem>();

        public DbSet<InventoryItemPackage> InventoryItemPackages
            => Set<InventoryItemPackage>();

        public DbSet<ReceivingLotCustomField> ReceivingLotCustomFields
            => Set<ReceivingLotCustomField>();

        public DbSet<InventoryMovement> InventoryMovements
            => Set<InventoryMovement>();

        // ============================================================
        // RECIPES
        // ============================================================

        public DbSet<Recipe> Recipes
            => Set<Recipe>();

        public DbSet<RecipeIngredient> RecipeIngredients
            => Set<RecipeIngredient>();

        // ============================================================
        // PRODUCTION
        // ============================================================

        public DbSet<ProductionIngredientSource> ProductionIngredientSources => Set<ProductionIngredientSource>();

        public DbSet<ProductionRun> ProductionRuns
            => Set<ProductionRun>();

        public DbSet<ProductionRunIngredient> ProductionRunIngredients
            => Set<ProductionRunIngredient>();

        public DbSet<ProductionWorkEntry> ProductionWorkEntries
            => Set<ProductionWorkEntry>();

        public DbSet<ProductionWorkEntryWorker> ProductionWorkEntryWorkers
            => Set<ProductionWorkEntryWorker>();

        public DbSet<ProductionObservation> ProductionObservations
            => Set<ProductionObservation>();

        public DbSet<ProductionObservationValue> ProductionObservationValues
            => Set<ProductionObservationValue>();

        // ============================================================
        // NEW AGRILOCO DEFINITION / DASHBOARD SYSTEM
        // ============================================================

        // Master/common concepts such as:
        // Apple, Honeycrisp, Juice, Jam, Vinegar, etc.
        public DbSet<Definition> Definitions
            => Set<Definition>();

        // A farm/business-specific use of a definition.
        //
        // This also carries the member's own hierarchy:
        //
        // Apple
        //   Honeycrisp
        //      Row 12
        //
        // or
        //
        // Strawberry Jam
        //   500 mL Jar
        public DbSet<FarmDefinition> FarmDefinitions
            => Set<FarmDefinition>();

        // Shared distribution / offering methods such as:
        // Pick Your Own
        // Farm Store
        // Wholesale
        // Retail Partner
        // Farmers Market
        public DbSet<AvailabilityChannel> AvailabilityChannels
            => Set<AvailabilityChannel>();

        // Many-to-many connection allowing one FarmDefinition
        // to be available through several channels simultaneously.
        public DbSet<FarmDefinitionChannel> FarmDefinitionChannels
            => Set<FarmDefinitionChannel>();

        // Historical/event records against a definition:
        // harvest, spray, pruning, picking, observations, etc.
        public DbSet<DefinitionRecord> DefinitionRecords
            => Set<DefinitionRecord>();
        // ============================================================
        // FARM MAP EDITOR / PUBLISHED MAPS
        // ============================================================

        public DbSet<FarmMap> FarmMaps
            => Set<FarmMap>();

        public DbSet<FarmMapFeature> FarmMapFeatures
            => Set<FarmMapFeature>();

        public DbSet<FarmMapFeaturePoint> FarmMapFeaturePoints
            => Set<FarmMapFeaturePoint>();
    }
}
