using Agriloco.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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

// ✅ Add HttpClient for Razor Pages to call your own API
builder.Services.AddHttpClient("AgrilocoApiClient", client =>
{
    // This should match your running site URL (launchSettings.json)
    client.BaseAddress = new Uri("http://localhost:5227/");
});

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

    // Swagger
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

// DB create + schema patch (for OfferingType)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgrilocoContext>();

    // ONE-TIME RESET (uncomment, run once, then comment again):
    // db.Database.EnsureDeleted();

    db.Database.EnsureCreated();

    // ✅ One-time schema patch for existing SQLite DBs (safe if already exists)
    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Crops ADD COLUMN OfferingType TEXT;");
    }
    catch
    {
        // ignore (likely already exists)
    }
}

app.MapControllers();
app.MapRazorPages();

app.Run();