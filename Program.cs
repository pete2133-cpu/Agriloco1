using Agriloco.Api.Data;
using Agriloco.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AgrilocoContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(cs))
        cs = "Data Source=agriloco.db";

    options.UseSqlite(cs);
});

builder.Services.AddHttpClient("AgrilocoApiClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5227/");
});

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

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgrilocoContext>();

    db.Database.EnsureCreated();

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

    try { db.Database.ExecuteSqlRaw("ALTER TABLE Crops ADD COLUMN OfferingType TEXT;"); } catch { }
}

app.MapControllers();
app.MapRazorPages();

app.Run();