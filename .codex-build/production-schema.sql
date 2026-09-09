IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [AvailabilityChannels] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AvailabilityChannels] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [CropAvailabilityAlertSubscriptions] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [CropId] int NOT NULL,
        [Email] nvarchar(320) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [NotifiedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_CropAvailabilityAlertSubscriptions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [CropCatalogAliases] (
        [Id] int NOT NULL IDENTITY,
        [CanonicalCategory] nvarchar(max) NOT NULL,
        [Alias] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CropCatalogAliases] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [CropCatalogItems] (
        [Id] int NOT NULL IDENTITY,
        [Category] nvarchar(max) NOT NULL,
        [Variety] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CropCatalogItems] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [CropColors] (
        [Id] int NOT NULL IDENTITY,
        [Category] nvarchar(max) NOT NULL,
        [Variety] nvarchar(max) NULL,
        [ColorCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CropColors] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [DefinitionRecords] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [FarmDefinitionId] int NOT NULL,
        [RecordType] nvarchar(max) NOT NULL,
        [RecordDate] datetime2 NOT NULL,
        [Quantity] decimal(18,6) NULL,
        [Unit] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DefinitionRecords] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Definitions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [DefinitionType] nvarchar(max) NOT NULL,
        [ParentDefinitionId] int NULL,
        [CanonicalKey] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Definitions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FarmAvailabilityAlertSubscriptions] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [CropId] int NOT NULL,
        [Email] nvarchar(320) NOT NULL,
        [Destination] nvarchar(320) NOT NULL,
        [Channel] nvarchar(20) NOT NULL,
        [IsFulfilled] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [FulfilledAt] datetime2 NULL,
        [SentAt] datetime2 NULL,
        CONSTRAINT [PK_FarmAvailabilityAlertSubscriptions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FarmDefinitionAvailabilitySubscriptions] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [FarmDefinitionId] int NULL,
        [Email] nvarchar(320) NOT NULL,
        [Channel] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastNotifiedAt] datetime2 NULL,
        CONSTRAINT [PK_FarmDefinitionAvailabilitySubscriptions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FarmDefinitionChannels] (
        [Id] int NOT NULL IDENTITY,
        [FarmDefinitionId] int NOT NULL,
        [AvailabilityChannelId] int NOT NULL,
        [IsEnabled] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FarmDefinitionChannels] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FarmDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [DefinitionId] int NULL,
        [ParentFarmDefinitionId] int NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [DefinitionType] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PresentationMode] nvarchar(max) NOT NULL,
        [IsPublic] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FarmDefinitions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FarmMaps] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [PublishedAt] datetime2 NULL,
        CONSTRAINT [PK_FarmMaps] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Farms] (
        [Id] int NOT NULL IDENTITY,
        [AgrilocoId] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [RegionCode] nvarchar(max) NULL,
        [ContactMethod1] nvarchar(max) NOT NULL,
        [FruitCategory1] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [Hours] nvarchar(max) NULL,
        [ParkingInfo] nvarchar(max) NULL,
        [EntranceInfo] nvarchar(max) NULL,
        [PaymentInfo] nvarchar(max) NULL,
        [ReservationUrl] nvarchar(max) NULL,
        [ProfileLastUpdatedAt] datetime2 NULL,
        [ProfileUpdateCount] int NOT NULL,
        [MapImageUrl] nvarchar(max) NULL,
        [MapImageUploadedAt] datetime2 NULL,
        CONSTRAINT [PK_Farms] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [HarvestLots] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [LotNumber] nvarchar(max) NOT NULL,
        [CropName] nvarchar(max) NOT NULL,
        [VarietyName] nvarchar(max) NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [HarvestDate] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [Workers] nvarchar(max) NOT NULL,
        [Condition] nvarchar(max) NOT NULL,
        [PhotoCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_HarvestLots] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [InventoryItemPackages] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [InventoryItemId] int NOT NULL,
        [PackageName] nvarchar(max) NOT NULL,
        [PackageType] nvarchar(max) NOT NULL,
        [PackageQuantity] decimal(18,6) NOT NULL,
        [PackageUnit] nvarchar(max) NOT NULL,
        [StorageLocation] nvarchar(max) NOT NULL,
        [BarcodeValue] nvarchar(max) NOT NULL,
        [ShelfLife] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InventoryItemPackages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [InventoryItems] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [ItemName] nvarchar(max) NOT NULL,
        [ItemCategory] nvarchar(max) NOT NULL,
        [BaseUnit] nvarchar(max) NOT NULL,
        [DefaultStorageLocation] nvarchar(max) NOT NULL,
        [ShelfLife] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [BarcodeValue] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [InventoryMovements] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [InventoryItemId] int NULL,
        [InventoryItemPackageId] int NULL,
        [ReceivingLotId] int NULL,
        [ProductionRunId] int NULL,
        [ProductionRunIngredientId] int NULL,
        [MovementType] nvarchar(max) NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [ReferenceNumber] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ProductionIngredientSources] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [ProductionRunId] int NOT NULL,
        [ProductionRunIngredientId] int NOT NULL,
        [ReceivingLotId] int NULL,
        [ReceiptPayload] nvarchar(max) NOT NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductionIngredientSources] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ProductionObservations] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [ProductionRunId] int NOT NULL,
        [ProductionWorkEntryId] int NULL,
        [RecordedAt] datetime2 NOT NULL,
        [ObservationType] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductionObservations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ProductionObservationValues] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [ProductionRunId] int NOT NULL,
        [ProductionObservationId] int NOT NULL,
        [MeasurementName] nvarchar(max) NOT NULL,
        [ValueText] nvarchar(max) NOT NULL,
        [NumericValue] decimal(18,6) NULL,
        [Unit] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductionObservationValues] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ProductionRunIngredients] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [ProductionRunId] int NOT NULL,
        [RecipeIngredientId] int NULL,
        [InventoryItemId] int NULL,
        [InventoryItemPackageId] int NULL,
        [IngredientName] nvarchar(max) NOT NULL,
        [VariationName] nvarchar(max) NOT NULL,
        [RecipeQuantity] decimal(18,6) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [SuggestedQuantity] decimal(18,6) NULL,
        [ActualQuantity] decimal(18,6) NULL,
        [IsScaleAnchor] bit NOT NULL,
        [CommittedQuantity] decimal(18,6) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductionRunIngredients] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ProductionRuns] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [RecipeId] int NOT NULL,
        [RecipeName] nvarchar(max) NOT NULL,
        [BatchNumber] nvarchar(max) NOT NULL,
        [ProcessType] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ProductionDate] datetime2 NOT NULL,
        [StartedAt] datetime2 NULL,
        [EndedAt] datetime2 NULL,
        [ScaleFactor] decimal(18,6) NULL,
        [ScaleAnchorIngredientId] int NULL,
        [ExpectedYieldQuantity] decimal(18,6) NULL,
        [ExpectedYieldUnit] nvarchar(max) NOT NULL,
        [ActualYieldQuantity] decimal(18,6) NULL,
        [ActualYieldUnit] nvarchar(max) NOT NULL,
        [OutputInventoryItemId] int NULL,
        [OutputInventoryItemPackageId] int NULL,
        [IngredientsCommitted] bit NOT NULL,
        [OutputPosted] bit NOT NULL,
        [InventoryReturned] bit NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductionRuns] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ProductionWorkEntries] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [ProductionRunId] int NOT NULL,
        [WorkType] nvarchar(max) NOT NULL,
        [WorkDate] datetime2 NOT NULL,
        [StartedAt] datetime2 NULL,
        [EndedAt] datetime2 NULL,
        [DefaultHours] decimal(18,6) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductionWorkEntries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ProductionWorkEntryWorkers] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [ProductionRunId] int NOT NULL,
        [ProductionWorkEntryId] int NOT NULL,
        [WorkerName] nvarchar(max) NOT NULL,
        [HoursWorked] decimal(18,6) NOT NULL,
        [HourlyRate] decimal(18,6) NULL,
        [LabourCost] decimal(18,6) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductionWorkEntryWorkers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ReceivingLotCustomFields] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [ReceivingLotId] int NOT NULL,
        [FieldName] nvarchar(max) NOT NULL,
        [FieldValue] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ReceivingLotCustomFields] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [ReceivingLots] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [LotNumber] nvarchar(max) NOT NULL,
        [ReceivedDate] datetime2 NOT NULL,
        [SupplierId] int NULL,
        [SupplierName] nvarchar(max) NOT NULL,
        [InventoryItemId] int NULL,
        [InventoryItemName] nvarchar(max) NOT NULL,
        [InventoryItemPackageId] int NULL,
        [VariationName] nvarchar(max) NOT NULL,
        [PackageType] nvarchar(max) NOT NULL,
        [UnitsReceived] decimal(18,6) NOT NULL,
        [UsableQuantityPerUnit] decimal(18,6) NOT NULL,
        [UsableUnit] nvarchar(max) NOT NULL,
        [TotalUsableQuantity] decimal(18,6) NOT NULL,
        [ItemName] nvarchar(max) NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Condition] nvarchar(max) NOT NULL,
        [StorageLocation] nvarchar(max) NOT NULL,
        [BarcodeValue] nvarchar(max) NOT NULL,
        [InvoiceNumber] nvarchar(max) NOT NULL,
        [PriceTotal] decimal(18,6) NULL,
        [PhotoCount] int NOT NULL,
        [DocumentCount] int NOT NULL,
        [SourceHarvestLotId] int NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ReceivingLots] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [RecipeIngredients] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [RecipeId] int NOT NULL,
        [InventoryItemId] int NULL,
        [InventoryItemPackageId] int NULL,
        [IngredientName] nvarchar(max) NOT NULL,
        [VariationName] nvarchar(max) NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [EstimatedUnitCost] decimal(18,6) NULL,
        [EstimatedTotalCost] decimal(18,6) NULL,
        [Notes] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RecipeIngredients] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Recipes] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [RecipeName] nvarchar(max) NOT NULL,
        [RecipeCategory] nvarchar(max) NOT NULL,
        [ExpectedYieldQuantity] decimal(18,6) NULL,
        [ExpectedYieldUnit] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Recipes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [SearchLogs] (
        [Id] int NOT NULL IDENTITY,
        [RegionCode] nvarchar(max) NOT NULL,
        [Term] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NULL,
        [Availability] nvarchar(max) NULL,
        [FarmId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SearchLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NULL,
        [SupplierName] nvarchar(max) NOT NULL,
        [SupplierType] nvarchar(max) NOT NULL,
        [LinkedFarmId] int NULL,
        [ContactName] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FarmMapFeatures] (
        [Id] int NOT NULL IDENTITY,
        [FarmMapId] int NOT NULL,
        [FarmDefinitionId] int NOT NULL,
        [Purpose] nvarchar(max) NOT NULL,
        [GeometryType] nvarchar(max) NOT NULL,
        [Subtype] nvarchar(max) NOT NULL,
        [DisplayPath] nvarchar(max) NOT NULL,
        [LineWidth] float NOT NULL,
        [Opacity] float NOT NULL,
        [ShowLabel] bit NOT NULL,
        [LabelSourceFarmDefinitionId] int NOT NULL,
        [CustomLabel] nvarchar(max) NOT NULL,
        [LabelPosition] nvarchar(max) NOT NULL,
        [LabelFontSize] float NOT NULL,
        [LabelTextColour] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_FarmMapFeatures] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmMapFeatures_FarmMaps_FarmMapId] FOREIGN KEY ([FarmMapId]) REFERENCES [FarmMaps] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Crops] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Variety] nvarchar(100) NULL,
        [Availability] nvarchar(20) NULL,
        [PickingCondition] nvarchar(20) NULL,
        [OfferingType] nvarchar(20) NULL,
        [OfferingTypes] nvarchar(200) NULL,
        [YearPlanted] int NULL,
        [Rootstock] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [AvailabilityNote] nvarchar(200) NULL,
        [InventorySource] nvarchar(30) NULL,
        [InventoryExternalId] nvarchar(100) NULL,
        [InventoryQuantity] int NULL,
        [InventoryStatus] nvarchar(50) NULL,
        [InventoryLastSyncAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Crops] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Crops_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FarmMapLayouts] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [Json] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FarmMapLayouts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmMapLayouts_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Members] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [Username] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NULL,
        [AltEmail] nvarchar(max) NULL,
        [PasswordHash] varbinary(max) NOT NULL,
        [PasswordSalt] varbinary(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Members] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Members_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FarmMapFeaturePoints] (
        [Id] int NOT NULL IDENTITY,
        [FarmMapFeatureId] int NOT NULL,
        [PointOrder] int NOT NULL,
        [X] float NOT NULL,
        [Y] float NOT NULL,
        CONSTRAINT [PK_FarmMapFeaturePoints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FarmMapFeaturePoints_FarmMapFeatures_FarmMapFeatureId] FOREIGN KEY ([FarmMapFeatureId]) REFERENCES [FarmMapFeatures] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE TABLE [MapCells] (
        [Id] int NOT NULL IDENTITY,
        [FarmId] int NOT NULL,
        [CropId] int NULL,
        [GridX] int NOT NULL,
        [GridY] int NOT NULL,
        [FeatureType] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MapCells] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MapCells_Crops_CropId] FOREIGN KEY ([CropId]) REFERENCES [Crops] ([Id]),
        CONSTRAINT [FK_MapCells_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'Description', N'IsActive', N'Name', N'SortOrder') AND [object_id] = OBJECT_ID(N'[AvailabilityChannels]'))
        SET IDENTITY_INSERT [AvailabilityChannels] ON;
    EXEC(N'INSERT INTO [AvailabilityChannels] ([Id], [Code], [CreatedAt], [Description], [IsActive], [Name], [SortOrder])
    VALUES (1, N''PickYourOwn'', ''2026-01-01T00:00:00.0000000Z'', N''Available for customers to harvest directly at the farm.'', CAST(1 AS bit), N''Pick Your Own'', 10),
    (2, N''FarmStore'', ''2026-01-01T00:00:00.0000000Z'', N''Available through the farm store or farm gate.'', CAST(1 AS bit), N''Farm Store'', 20),
    (3, N''Wholesale'', ''2026-01-01T00:00:00.0000000Z'', N''Available for wholesale purchase.'', CAST(1 AS bit), N''Wholesale'', 30),
    (4, N''RetailPartner'', ''2026-01-01T00:00:00.0000000Z'', N''Available through third-party retail partners.'', CAST(1 AS bit), N''Retail Partner'', 40),
    (5, N''FarmersMarket'', ''2026-01-01T00:00:00.0000000Z'', N''Available through farmers markets.'', CAST(1 AS bit), N''Farmers Market'', 50)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'Description', N'IsActive', N'Name', N'SortOrder') AND [object_id] = OBJECT_ID(N'[AvailabilityChannels]'))
        SET IDENTITY_INSERT [AvailabilityChannels] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AvailabilityChannels_Code] ON [AvailabilityChannels] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Crops_FarmId] ON [Crops] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FarmDefinitionChannels_FarmDefinitionId_AvailabilityChannelId] ON [FarmDefinitionChannels] ([FarmDefinitionId], [AvailabilityChannelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_FarmMapFeaturePoints_FarmMapFeatureId] ON [FarmMapFeaturePoints] ([FarmMapFeatureId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_FarmMapFeatures_FarmMapId] ON [FarmMapFeatures] ([FarmMapId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FarmMapLayouts_FarmId] ON [FarmMapLayouts] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_MapCells_CropId] ON [MapCells] ([CropId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_MapCells_FarmId] ON [MapCells] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Members_FarmId] ON [Members] ([FarmId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionIngredientSources_FarmId_ProductionRunIngredientId] ON [ProductionIngredientSources] ([FarmId], [ProductionRunIngredientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909151613_InitialSqlServer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909151613_InitialSqlServer', N'8.0.23');
END;
GO

COMMIT;
GO

