using Agriloco.Api.Data;
using Agriloco.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();

var provider = builder.Configuration["Database:Provider"]
    ?? (builder.Environment.IsDevelopment() ? "Sqlite" : "SqlServer");
var connection = builder.Configuration.GetConnectionString("Default");
if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) && builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AgrilocoContext>(options =>
        options.UseSqlite(string.IsNullOrWhiteSpace(connection) ? "Data Source=agriloco.db" : connection));
}
else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Set ConnectionStrings__Default for SQL Server/Azure SQL.");
    builder.Services.AddDbContext<SqlServerAgrilocoContext>(options =>
        options.UseSqlServer(connection, sql => sql.EnableRetryOnFailure()));
    builder.Services.AddScoped<AgrilocoContext>(services => services.GetRequiredService<SqlServerAgrilocoContext>());
}
else
{
    throw new InvalidOperationException("Database:Provider must be SqlServer, or Sqlite in Development only.");
}

var publicUrl = builder.Configuration["Application:PublicBaseUrl"];
if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var publicBaseUri) ||
    publicBaseUri.AbsolutePath != "/" || publicBaseUri.Query.Length != 0 ||
    publicBaseUri.Fragment.Length != 0 || publicBaseUri.UserInfo.Length != 0 ||
    (publicBaseUri.Scheme != "https" && (!builder.Environment.IsDevelopment() || publicBaseUri.Scheme != "http")))
    throw new InvalidOperationException("Set Application__PublicBaseUrl to the public HTTPS origin (HTTP is allowed in Development).");
builder.Services.AddHttpClient("AgrilocoApiClient", client =>
{
    client.BaseAddress = new Uri(publicBaseUri.GetLeftPart(UriPartial.Authority) + "/");
});
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);

builder.Services.AddSingleton<IFarmAvailabilityAlertQueue, FarmAvailabilityAlertQueue>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<FarmAvailabilityAlertWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Agriloco API",
        Version = "v1"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agriloco API v1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles(UnityWebGlStaticFiles.CreateOptions());
app.UseRouting();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgrilocoContext>();

    // Production schema is applied separately using reviewed SQL Server migrations.
    if (db.Database.IsSqlite() && app.Environment.IsDevelopment())
    {
    db.Database.EnsureCreated();
    Agriloco1.Services.ProductionSourceSchema.EnsureCreated(db);

    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS FarmMapLayouts (
            Id INTEGER NOT NULL CONSTRAINT PK_FarmMapLayouts PRIMARY KEY AUTOINCREMENT,
            FarmId INTEGER NOT NULL,
            Json TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            CONSTRAINT UQ_FarmMapLayouts_FarmId UNIQUE (FarmId),
            CONSTRAINT FK_FarmMapLayouts_Farms_FarmId FOREIGN KEY (FarmId) REFERENCES Farms (Id) ON DELETE CASCADE
        );
    ");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS HarvestLots (
            Id INTEGER NOT NULL CONSTRAINT PK_HarvestLots PRIMARY KEY AUTOINCREMENT,
            FarmId INTEGER NULL,
            LotNumber TEXT NOT NULL,
            CropName TEXT NOT NULL,
            VarietyName TEXT NOT NULL,
            Quantity REAL NOT NULL,
            Unit TEXT NOT NULL,
            HarvestDate TEXT NOT NULL,
            Status TEXT NOT NULL,
            Notes TEXT NOT NULL,
            Location TEXT NOT NULL,
            Workers TEXT NOT NULL,
            Condition TEXT NOT NULL,
            PhotoCount INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL
        );
    ");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS Suppliers (
            Id INTEGER NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY AUTOINCREMENT,
            FarmId INTEGER NULL,
            SupplierName TEXT NOT NULL,
            SupplierType TEXT NOT NULL,
            LinkedFarmId INTEGER NULL,
            ContactName TEXT NOT NULL,
            Phone TEXT NOT NULL,
            Email TEXT NOT NULL,
            Address TEXT NOT NULL,
            Notes TEXT NOT NULL,
            IsActive INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL
        );
    ");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS InventoryItems (
            Id INTEGER NOT NULL CONSTRAINT PK_InventoryItems PRIMARY KEY AUTOINCREMENT,
            FarmId INTEGER NULL,
            ItemName TEXT NOT NULL,
            ItemCategory TEXT NOT NULL,
            BaseUnit TEXT NOT NULL,
            DefaultStorageLocation TEXT NOT NULL,
            ShelfLife TEXT NOT NULL,
            Notes TEXT NOT NULL,
            BarcodeValue TEXT NOT NULL,
            IsActive INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL
        );
    ");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS InventoryItemPackages (
            Id INTEGER NOT NULL CONSTRAINT PK_InventoryItemPackages PRIMARY KEY AUTOINCREMENT,
            FarmId INTEGER NULL,
            InventoryItemId INTEGER NOT NULL,
            PackageName TEXT NOT NULL,
            PackageType TEXT NOT NULL DEFAULT '',
            PackageQuantity REAL NOT NULL,
            PackageUnit TEXT NOT NULL,
            StorageLocation TEXT NOT NULL DEFAULT '',
            BarcodeValue TEXT NOT NULL DEFAULT '',
            ShelfLife TEXT NOT NULL DEFAULT '',
            Notes TEXT NOT NULL,
            IsActive INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL
        );
    ");
    }
    catch { }

    try { db.Database.ExecuteSqlRaw("ALTER TABLE InventoryItemPackages ADD COLUMN PackageType TEXT NOT NULL DEFAULT '';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE InventoryItemPackages ADD COLUMN StorageLocation TEXT NOT NULL DEFAULT '';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE InventoryItemPackages ADD COLUMN BarcodeValue TEXT NOT NULL DEFAULT '';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE InventoryItemPackages ADD COLUMN ShelfLife TEXT NOT NULL DEFAULT '';"); } catch { }
    // ✅ Recipes table for Inventory and Production
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS Recipes (
        Id INTEGER NOT NULL CONSTRAINT PK_Recipes PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        RecipeName TEXT NOT NULL,
        RecipeCategory TEXT NOT NULL,
        ExpectedYieldQuantity REAL NULL,
        ExpectedYieldUnit TEXT NOT NULL,
        Status TEXT NOT NULL,
        Notes TEXT NOT NULL,
        IsActive INTEGER NOT NULL,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }
    // ✅ RecipeIngredients table for recipe building
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS RecipeIngredients (
        Id INTEGER NOT NULL CONSTRAINT PK_RecipeIngredients PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        RecipeId INTEGER NOT NULL,
        InventoryItemId INTEGER NULL,
        InventoryItemPackageId INTEGER NULL,
        IngredientName TEXT NOT NULL,
        VariationName TEXT NOT NULL,
        Quantity REAL NOT NULL,
        Unit TEXT NOT NULL,
        EstimatedUnitCost REAL NULL,
        EstimatedTotalCost REAL NULL,
        Notes TEXT NOT NULL,
        SortOrder INTEGER NOT NULL,
        CreatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }
    // ✅ Inventory movement ledger
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS InventoryMovements (
        Id INTEGER NOT NULL CONSTRAINT PK_InventoryMovements PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        InventoryItemId INTEGER NULL,
        InventoryItemPackageId INTEGER NULL,
        ReceivingLotId INTEGER NULL,
        ProductionRunId INTEGER NULL,
        ProductionRunIngredientId INTEGER NULL,
        MovementType TEXT NOT NULL,
        Quantity REAL NOT NULL,
        Unit TEXT NOT NULL,
        ReferenceNumber TEXT NOT NULL,
        Notes TEXT NOT NULL,
        OccurredAt TEXT NOT NULL,
        CreatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }
    // ✅ Production runs / batches
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ProductionRuns (
        Id INTEGER NOT NULL CONSTRAINT PK_ProductionRuns PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        RecipeId INTEGER NOT NULL,
        RecipeName TEXT NOT NULL,
        BatchNumber TEXT NOT NULL,
        ProcessType TEXT NOT NULL,
        Status TEXT NOT NULL,
        ProductionDate TEXT NOT NULL,
        StartedAt TEXT NULL,
        EndedAt TEXT NULL,
        ScaleFactor REAL NULL,
        ScaleAnchorIngredientId INTEGER NULL,
        ExpectedYieldQuantity REAL NULL,
        ExpectedYieldUnit TEXT NOT NULL,
        ActualYieldQuantity REAL NULL,
        ActualYieldUnit TEXT NOT NULL,
        OutputInventoryItemId INTEGER NULL,
        OutputInventoryItemPackageId INTEGER NULL,
        IngredientsCommitted INTEGER NOT NULL,
        OutputPosted INTEGER NOT NULL,
        InventoryReturned INTEGER NOT NULL,
        Notes TEXT NOT NULL,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }
    // ✅ Production run ingredient snapshots
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ProductionRunIngredients (
        Id INTEGER NOT NULL CONSTRAINT PK_ProductionRunIngredients PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        ProductionRunId INTEGER NOT NULL,
        RecipeIngredientId INTEGER NULL,
        InventoryItemId INTEGER NULL,
        InventoryItemPackageId INTEGER NULL,
        IngredientName TEXT NOT NULL,
        VariationName TEXT NOT NULL,
        RecipeQuantity REAL NOT NULL,
        Unit TEXT NOT NULL,
        SuggestedQuantity REAL NULL,
        ActualQuantity REAL NULL,
        IsScaleAnchor INTEGER NOT NULL,
        CommittedQuantity REAL NOT NULL,
        Notes TEXT NOT NULL,
        SortOrder INTEGER NOT NULL,
        CreatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }
    // ✅ Production labour/work events
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ProductionWorkEntries (
        Id INTEGER NOT NULL CONSTRAINT PK_ProductionWorkEntries PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        ProductionRunId INTEGER NOT NULL,
        WorkType TEXT NOT NULL,
        WorkDate TEXT NOT NULL,
        StartedAt TEXT NULL,
        EndedAt TEXT NULL,
        DefaultHours REAL NOT NULL,
        Notes TEXT NOT NULL,
        CreatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }
    // ✅ Workers attached to production labour events
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ProductionWorkEntryWorkers (
        Id INTEGER NOT NULL CONSTRAINT PK_ProductionWorkEntryWorkers PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        ProductionRunId INTEGER NOT NULL,
        ProductionWorkEntryId INTEGER NOT NULL,
        WorkerName TEXT NOT NULL,
        HoursWorked REAL NOT NULL,
        HourlyRate REAL NULL,
        LabourCost REAL NULL,
        CreatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }
    // ✅ Production observations and batch checks
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ProductionObservations (
        Id INTEGER NOT NULL CONSTRAINT PK_ProductionObservations PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        ProductionRunId INTEGER NOT NULL,
        ProductionWorkEntryId INTEGER NULL,
        RecordedAt TEXT NOT NULL,
        ObservationType TEXT NOT NULL,
        Notes TEXT NOT NULL,
        CreatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }
    // ✅ Multiple measured values within one production observation
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ProductionObservationValues (
        Id INTEGER NOT NULL CONSTRAINT PK_ProductionObservationValues PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        ProductionRunId INTEGER NOT NULL,
        ProductionObservationId INTEGER NOT NULL,
        MeasurementName TEXT NOT NULL,
        ValueText TEXT NOT NULL,
        NumericValue REAL NULL,
        Unit TEXT NOT NULL,
        Notes TEXT NOT NULL,
        SortOrder INTEGER NOT NULL,
        CreatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }


    // ✅ ReceivingLotCustomFields table for flexible receiving details
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ReceivingLotCustomFields (
        Id INTEGER NOT NULL CONSTRAINT PK_ReceivingLotCustomFields PRIMARY KEY AUTOINCREMENT,
        FarmId INTEGER NULL,
        ReceivingLotId INTEGER NOT NULL,
        FieldName TEXT NOT NULL,
        FieldValue TEXT NOT NULL,
        CreatedAt TEXT NOT NULL
    );
");
    }
    catch
    {
        // ignore
    }

    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ReceivingLots (
            Id INTEGER NOT NULL CONSTRAINT PK_ReceivingLots PRIMARY KEY AUTOINCREMENT,
            FarmId INTEGER NULL,
            LotNumber TEXT NOT NULL,
            ReceivedDate TEXT NOT NULL,

            SupplierId INTEGER NULL,
            SupplierName TEXT NOT NULL,

            InventoryItemId INTEGER NULL,
            InventoryItemName TEXT NOT NULL,

            InventoryItemPackageId INTEGER NULL,
            VariationName TEXT NOT NULL,
            PackageType TEXT NOT NULL,

            UnitsReceived REAL NOT NULL,
            UsableQuantityPerUnit REAL NOT NULL,
            UsableUnit TEXT NOT NULL,
            TotalUsableQuantity REAL NOT NULL,

            ItemName TEXT NOT NULL,
            Quantity REAL NOT NULL,
            Unit TEXT NOT NULL,

            Status TEXT NOT NULL,
            Condition TEXT NOT NULL,

            StorageLocation TEXT NOT NULL,
            BarcodeValue TEXT NOT NULL,

            InvoiceNumber TEXT NOT NULL,
            PriceTotal REAL NULL,

            PhotoCount INTEGER NOT NULL,
            DocumentCount INTEGER NOT NULL,

            SourceHarvestLotId INTEGER NULL,

            Notes TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
    ");
    }
    catch { }
    // ============================================================
    // AGRILOCO DEFINITION / DASHBOARD SYSTEM
    // ============================================================

    // Master definitions
    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS Definitions (
            Id INTEGER NOT NULL CONSTRAINT PK_Definitions PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            DefinitionType TEXT NOT NULL,
            ParentDefinitionId INTEGER NULL,
            CanonicalKey TEXT NOT NULL,
            Description TEXT NOT NULL,
            IsActive INTEGER NOT NULL,
            SortOrder INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        ");
    }
    catch { }

    // Farm-specific definition hierarchy
    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS FarmDefinitions (
            Id INTEGER NOT NULL CONSTRAINT PK_FarmDefinitions PRIMARY KEY AUTOINCREMENT,
            FarmId INTEGER NOT NULL,
            DefinitionId INTEGER NULL,
            ParentFarmDefinitionId INTEGER NULL,
            DisplayName TEXT NOT NULL,
            DefinitionType TEXT NOT NULL,
            Status TEXT NOT NULL,
            PresentationMode TEXT NOT NULL,
            IsPublic INTEGER NOT NULL,
            IsActive INTEGER NOT NULL,
            SortOrder INTEGER NOT NULL,
            Notes TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        ");
    }
    catch { }

    // Master availability / sales channels
    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS AvailabilityChannels (
            Id INTEGER NOT NULL CONSTRAINT PK_AvailabilityChannels PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Code TEXT NOT NULL,
            Description TEXT NOT NULL,
            IsActive INTEGER NOT NULL,
            SortOrder INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            CONSTRAINT UQ_AvailabilityChannels_Code UNIQUE (Code)
        );
        ");
    }
    catch { }

    // Allows one item to have multiple channels
    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS FarmDefinitionChannels (
            Id INTEGER NOT NULL CONSTRAINT PK_FarmDefinitionChannels PRIMARY KEY AUTOINCREMENT,
            FarmDefinitionId INTEGER NOT NULL,
            AvailabilityChannelId INTEGER NOT NULL,
            IsEnabled INTEGER NOT NULL,
            UpdatedAt TEXT NOT NULL,
            CONSTRAINT UQ_FarmDefinitionChannels UNIQUE (
                FarmDefinitionId,
                AvailabilityChannelId
            )
        );
        ");
    }
    catch { }

    // Activity/history records
    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS DefinitionRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_DefinitionRecords PRIMARY KEY AUTOINCREMENT,
            FarmId INTEGER NOT NULL,
            FarmDefinitionId INTEGER NOT NULL,
            RecordType TEXT NOT NULL,
            RecordDate TEXT NOT NULL,
            Quantity REAL NULL,
            Unit TEXT NOT NULL,
            Notes TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        ");
    }
    catch { }
    // ============================================================
    // FARM DEFINITION AVAILABILITY SUBSCRIPTIONS
    // ============================================================

    // Customer email subscriptions from the public viewer.
    //
    // FarmDefinitionId:
    //     NULL = subscribe to availability updates for the entire farm
    //     Product ID = subscribe to that product branch
    //     Variety ID = subscribe to that specific variety
    //
    // Unlike the legacy Crop/Farm alert system, these subscriptions
    // remain active after an email is sent.
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS FarmDefinitionAvailabilitySubscriptions (
        Id INTEGER NOT NULL
            CONSTRAINT PK_FarmDefinitionAvailabilitySubscriptions
            PRIMARY KEY AUTOINCREMENT,

        FarmId INTEGER NOT NULL,

        FarmDefinitionId INTEGER NULL,

        Email TEXT NOT NULL,

        Channel TEXT NOT NULL DEFAULT 'email',

        IsActive INTEGER NOT NULL DEFAULT 1,

        CreatedAt TEXT NOT NULL,

        LastNotifiedAt TEXT NULL
    );
    ");
    }
    catch { }

    // Helps find active subscriptions for a farm quickly.
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE INDEX IF NOT EXISTS
        IX_FarmDefinitionAvailabilitySubscriptions_FarmId
    ON FarmDefinitionAvailabilitySubscriptions (FarmId);
    ");
    }
    catch { }

    // Helps find subscriptions attached to a specific
    // product or variety.
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE INDEX IF NOT EXISTS
        IX_FarmDefinitionAvailabilitySubscriptions_FarmDefinitionId
    ON FarmDefinitionAvailabilitySubscriptions (FarmDefinitionId);
    ");
    }
    catch { }

    // Prevent the same email address from accidentally creating
    // the exact same active subscription repeatedly.
    //
    // We use a partial SQLite index so an old inactive subscription
    // does not prevent the customer from subscribing again later.
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE UNIQUE INDEX IF NOT EXISTS
        UX_FarmDefinitionAvailabilitySubscriptions_ActiveSubscription
    ON FarmDefinitionAvailabilitySubscriptions
       (FarmId, FarmDefinitionId, Email, Channel)
    WHERE IsActive = 1
      AND FarmDefinitionId IS NOT NULL;
    ");
    }
    catch { }

    // SQLite treats NULL values as distinct inside a unique index,
    // so farm-wide subscriptions need their own uniqueness rule.
    try
    {
        db.Database.ExecuteSqlRaw(@"
    CREATE UNIQUE INDEX IF NOT EXISTS
        UX_FarmDefinitionAvailabilitySubscriptions_ActiveFarmSubscription
    ON FarmDefinitionAvailabilitySubscriptions
       (FarmId, Email, Channel)
    WHERE IsActive = 1
      AND FarmDefinitionId IS NULL;
    ");
    }
    catch { }
    // ============================================================
    // FARM MAP EDITOR / PUBLISHED MAPS
    // ============================================================

    // One farm can have a Draft map and a Published map.
    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS FarmMaps (
            Id INTEGER NOT NULL
                CONSTRAINT PK_FarmMaps
                PRIMARY KEY AUTOINCREMENT,

            FarmId INTEGER NOT NULL,

            Status TEXT NOT NULL,

            CreatedAt TEXT NOT NULL,

            UpdatedAt TEXT NOT NULL,

            PublishedAt TEXT NULL,

            CONSTRAINT UQ_FarmMaps_FarmId_Status
                UNIQUE (FarmId, Status)
        );
        ");
    }
    catch { }

    // Individual map features:
    // lines, polygons, points, pathways, POIs, etc.
    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS FarmMapFeatures (
            Id INTEGER NOT NULL
                CONSTRAINT PK_FarmMapFeatures
                PRIMARY KEY AUTOINCREMENT,

            FarmMapId INTEGER NOT NULL,

            FarmDefinitionId INTEGER NOT NULL,

            Purpose TEXT NOT NULL,

            GeometryType TEXT NOT NULL,

            Subtype TEXT NOT NULL,

            DisplayPath TEXT NOT NULL,

            LineWidth REAL NOT NULL,

            Opacity REAL NOT NULL,

            ShowLabel INTEGER NOT NULL,

            LabelSourceFarmDefinitionId INTEGER NOT NULL,

            CustomLabel TEXT NOT NULL,

            LabelPosition TEXT NOT NULL,

            LabelFontSize REAL NOT NULL,

            LabelTextColour TEXT NOT NULL,

            CONSTRAINT FK_FarmMapFeatures_FarmMaps
                FOREIGN KEY (FarmMapId)
                REFERENCES FarmMaps (Id)
                ON DELETE CASCADE
        );
        ");
    }
    catch { }

    // Ordered normalized points belonging to each feature.
    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS FarmMapFeaturePoints (
            Id INTEGER NOT NULL
                CONSTRAINT PK_FarmMapFeaturePoints
                PRIMARY KEY AUTOINCREMENT,

            FarmMapFeatureId INTEGER NOT NULL,

            PointOrder INTEGER NOT NULL,

            X REAL NOT NULL,

            Y REAL NOT NULL,

            CONSTRAINT FK_FarmMapFeaturePoints_FarmMapFeatures
                FOREIGN KEY (FarmMapFeatureId)
                REFERENCES FarmMapFeatures (Id)
                ON DELETE CASCADE
        );
        ");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE INDEX IF NOT EXISTS
            IX_FarmMapFeatures_FarmMapId
        ON FarmMapFeatures (FarmMapId);
        ");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
        CREATE INDEX IF NOT EXISTS
            IX_FarmMapFeaturePoints_FarmMapFeatureId
        ON FarmMapFeaturePoints (FarmMapFeatureId);
        ");
    }
    catch { }

    // ============================================================
    // DEFAULT AVAILABILITY CHANNELS
    // ============================================================

    try
    {
        db.Database.ExecuteSqlRaw(@"
        INSERT OR IGNORE INTO AvailabilityChannels
        (Name, Code, Description, IsActive, SortOrder, CreatedAt)
        VALUES
        ('Pick Your Own', 'PickYourOwn',
         'Available for customers to harvest directly at the farm.',
         1, 10, CURRENT_TIMESTAMP);

        INSERT OR IGNORE INTO AvailabilityChannels
        (Name, Code, Description, IsActive, SortOrder, CreatedAt)
        VALUES
        ('Farm Store', 'FarmStore',
         'Available through the farm store or farm gate.',
         1, 20, CURRENT_TIMESTAMP);

        INSERT OR IGNORE INTO AvailabilityChannels
        (Name, Code, Description, IsActive, SortOrder, CreatedAt)
        VALUES
        ('Wholesale', 'Wholesale',
         'Available for wholesale purchase.',
         1, 30, CURRENT_TIMESTAMP);

        INSERT OR IGNORE INTO AvailabilityChannels
        (Name, Code, Description, IsActive, SortOrder, CreatedAt)
        VALUES
        ('Retail Partner', 'RetailPartner',
         'Available through third-party retail partners.',
         1, 40, CURRENT_TIMESTAMP);

        INSERT OR IGNORE INTO AvailabilityChannels
        (Name, Code, Description, IsActive, SortOrder, CreatedAt)
        VALUES
        ('Farmers Market', 'FarmersMarket',
         'Available through a farmers market.',
         1, 50, CURRENT_TIMESTAMP);
        ");
    }
    catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Crops ADD COLUMN OfferingType TEXT;"); } catch { }
    }
}

app.MapControllers();
app.MapRazorPages();

app.Run();
