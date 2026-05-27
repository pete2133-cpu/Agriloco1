using Agriloco.Api.Data;
using Agriloco.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
// Controllers + Razor Pages
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// DbContext (SQLite)
builder.Services.AddDbContext<AgrilocoContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(cs))
        cs = "Data Source=agriloco.db";

    options.UseSqlite(cs);
});

// HttpClient for Razor Pages to call your own API
builder.Services.AddHttpClient("AgrilocoApiClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5227/");
});

// =======================================================
// ✅ ALERTS / EMAIL
// =======================================================
builder.Services.AddSingleton<IFarmAvailabilityAlertQueue, FarmAvailabilityAlertQueue>();

// ✅ IMPORTANT: REAL EMAIL SENDER (NOT DEBUG)
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();



builder.Services.AddHostedService<FarmAvailabilityAlertWorker>();
// Swagger/OpenAPI
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

// =======================================================
// DB BOOTSTRAP
// ONE-TIME RESET ENABLED
// =======================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgrilocoContext>();

    

    // ✅ Create tables from current Models (no migrations needed)
    db.Database.EnsureCreated();

    // ✅ One-time schema patch for FarmMapLayouts table (safe if already exists)
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
    catch
    {
        // ignore
    }

    // ✅ Legacy patch: OfferingType column (safe to keep for now)
    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Crops ADD COLUMN OfferingType TEXT;");
    }
    catch
    {
        // ignore
    }
}

app.MapControllers();
app.MapRazorPages();

app.Run();