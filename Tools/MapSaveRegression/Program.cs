using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Agriloco.Api.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Agriloco.Api.Controllers;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var database = "AgrilocoMapRegression_" + Guid.NewGuid().ToString("N");
var fault = new SaveFault();
var options = new DbContextOptionsBuilder<SqlServerAgrilocoContext>()
    .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={database};Integrated Security=True;TrustServerCertificate=True",
        sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null))
    .AddInterceptors(fault).Options;
await using var db = new SqlServerAgrilocoContext(options);
void Check(bool ok, string message)
{
    if (!ok) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}
try
{
    await db.Database.MigrateAsync();
    db.Farms.Add(new Farm { Name = "Isolated map regression", AgrilocoId = database });
    await db.SaveChangesAsync();
    int farmId = await db.Farms.Select(f => f.Id).SingleAsync();
    var controller = new UnityMapController(db);
    SaveMapRequest Request(double x) => new() { FarmId = farmId, Features = new() {
        new SaveMapFeatureRequest { GeometryType = "Line", Purpose = "FarmDefinition", LineWidth = 6,
            Opacity = 0.8, ShowLabel = true, CustomLabel = "Regression row", LabelPosition = "Center",
            LabelFontSize = 9, LabelTextColour = "White",
            LabelLayoutJson = "{\"version\":1,\"manual\":true,\"placed\":true,\"offsetX\":-0.015,\"offsetY\":0.025,\"paddingX\":6,\"paddingY\":2,\"backgroundColour\":\"Dark Slate\"}",
            Points = new() { new() { X = x, Y = 0.2 }, new() { X = 0.3, Y = 0.4 } } } } };
    async Task<double> FirstX(string status) => await db.FarmMapFeaturePoints.AsNoTracking()
        .Where(p => p.FarmMapFeature!.FarmMap!.FarmId == farmId && p.FarmMapFeature.FarmMap.Status == status)
        .OrderBy(p => p.PointOrder).Select(p => p.X).FirstAsync();

    if (args.Contains("--expect-failure"))
    {
        try { await controller.SaveDraft(Request(0.1)); throw new Exception("Old save unexpectedly succeeded"); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("does not support user-initiated transactions"))
        { Console.WriteLine("REPRODUCED SQL SERVER FAILURE: " + ex.Message); }
        return;
    }
    Check(await controller.PublishMap(farmId) is BadRequestObjectResult, "No draft retains existing 400");
    fault.Transient = true;
    Check(await controller.SaveDraft(Request(0.1)) is OkObjectResult, "First draft saves with a transient failure after identity insert");
    Check(fault.Thrown == 1, "Transient SQL-unit replay exercised");
    Check(await db.FarmMaps.CountAsync() == 1 && await db.FarmMapFeatures.CountAsync() == 1 &&
        await db.FarmMapFeaturePoints.CountAsync() == 2, "Retry creates no duplicate maps/features/points");
    Check(await controller.PublishMap(farmId) is OkObjectResult, "First publish succeeds with SQL retry strategy enabled");
    Check(await FirstX("Published") == 0.1, "Published geometry round-trips");
    fault.Transient = true;
    Check(await controller.SaveDraft(Request(0.6)) is OkObjectResult, "Replacing draft retries after feature deletion");
    Check(await FirstX("Draft") == 0.6 && await FirstX("Published") == 0.1, "Draft edits do not alter published snapshot");
    fault.Fatal = true;
    try { await controller.SaveDraft(Request(0.9)); throw new Exception("Expected failed save"); }
    catch (InvalidOperationException ex) when (ex.Message == "Regression permanent failure") { }
    db.ChangeTracker.Clear();
    Check(await FirstX("Draft") == 0.6 && await FirstX("Published") == 0.1, "Failed replacement rolls back existing draft and published map");
    fault.Transient = true;
    Check(await controller.PublishMap(farmId) is OkObjectResult, "Republish retries after published feature deletion");
    Check(await FirstX("Published") == 0.6, "Republish updates snapshot");
    Check(await db.FarmMaps.CountAsync() == 2 && await db.FarmMapFeatures.CountAsync() == 2 &&
        await db.FarmMapFeaturePoints.CountAsync() == 4, "Repeated operations leave no duplicate or orphan geometry");
    Check(!db.Database.HasPendingModelChanges(), "SQL Server migrations including label metadata match model");
    var member = new Member { FarmId = farmId, Username = "map-regression-member" };
    string password = args.Contains("--geo-unity-host") ? Environment.GetEnvironmentVariable("AGRILOCO_GEO_TEST_PASSWORD") ?? throw new Exception("Missing isolated test secret") : Guid.NewGuid().ToString("N");
    member.PasswordHash = MemberPasswords.Hash(member, password);
    db.Members.Add(member);
    await db.SaveChangesAsync();

    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    builder.WebHost.UseUrls(args.Contains("--geo-unity-host") ? "https://localhost:65449" : "https://127.0.0.1:0");
    builder.Services.AddControllers().AddApplicationPart(typeof(UnityMapController).Assembly);
    builder.Services.AddScoped<AgrilocoContext>(_ => new SqlServerAgrilocoContext(options));
    builder.Services.AddScoped<UnityEditorTokens>();
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
    builder.Services.AddAuthorization();
    builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("member-login", l =>
    { l.PermitLimit = 10; l.Window = TimeSpan.FromMinutes(1); }));
    await using var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();
    await app.StartAsync();
    try
    {
        if(args.Length==2 && args[0]=="--geo-unity-host"){await GeoUnityHost.Run(db,farmId,args[1]);return;}
        string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        // Fixture-only trust for the local development certificate. Never contacts Azure.
        using var client = new HttpClient(new HttpClientHandler {
            ServerCertificateCustomValidationCallback = (request, _, _, _) => request.RequestUri!.IsLoopback
        }) { BaseAddress = new Uri(address) };
        async Task Expect(HttpMethod method, string route, int expected, object? body = null, string? token = null)
        {
            using var request = new HttpRequestMessage(method, route);
            if (body != null) request.Content = JsonContent.Create(body);
            if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request);
            Check((int)response.StatusCode == expected, $"{method} {route}: {expected}");
        }
        await Expect(HttpMethod.Get, $"/api/UnityMap/farm?farmId={farmId}", 200);
        await Expect(HttpMethod.Get, $"/api/UnityMap/published?farmId={farmId}", 200);
        await Expect(HttpMethod.Get, $"/api/UnityMap/draft?farmId={farmId}", 401);
        await Expect(HttpMethod.Post, "/api/UnityMap/draft", 401, Request(0.7));
        await Expect(HttpMethod.Post, $"/api/UnityMap/publish?farmId={farmId}", 401);
        using var login = await client.PostAsJsonAsync("/api/UnityEditorAuth/login", new { username = member.Username, password });
        Check(login.StatusCode == HttpStatusCode.OK, "Existing member credentials authenticate");
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        string token = loginJson.RootElement.GetProperty("accessToken").GetString()!;
        await Expect(HttpMethod.Post, "/api/UnityMap/draft", 401, Request(0.7), "invalid");
        var foreign = Request(0.7); foreign.FarmId = farmId + 999;
        await Expect(HttpMethod.Post, "/api/UnityMap/draft", 403, foreign, token);
        await Expect(HttpMethod.Post, $"/api/UnityMap/publish?farmId={farmId + 999}", 403, token: token);
        var foreignDefinition = Request(0.7); foreignDefinition.Features[0].FarmDefinitionId = 999999;
        await Expect(HttpMethod.Post, "/api/UnityMap/draft", 403, foreignDefinition, token);
        if (args.Length==2 && args[0]=="--geo-regression") await GeoRegression.Run(client,db,token,farmId,args[1]);
        if (args.Contains("--draft-schema-regression"))
        {
            // Only this uniquely named disposable LocalDB fixture is changed. Never application settings/Azure.
            Check(database.StartsWith("AgrilocoMapRegression_") && db.Database.GetDbConnection().DataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase),
                "Schema reproduction is restricted to disposable LocalDB");
            db.FarmDefinitions.Add(new Agriloco1.Models.Inventory.FarmDefinition {
                FarmId = farmId, DisplayName = "Temporary Pumpkin Path", DefinitionType = "Pathway", Status = "", IsPublic = true });
            await db.SaveChangesAsync();
            var before = await db.FarmMapFeaturePoints.AsNoTracking().OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.FarmMapFeatureId, p.PointOrder, p.X, p.Y }).ToListAsync();
            int featureCount = await db.FarmMapFeatures.CountAsync();
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [FarmMapFeatures] DROP COLUMN [LabelLayoutJson]");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260913153926_AddMapLabelLayout'");
            await Expect(HttpMethod.Get, $"/api/UnityMap/farm?farmId={farmId}", 200);
            await Expect(HttpMethod.Get, $"/api/UnityMap/draft?farmId={farmId}", 500, token: token);
            await Expect(HttpMethod.Get, $"/api/UnityMap/published?farmId={farmId}", 500);
            try { await controller.GetDraft(farmId); throw new Exception("Expected missing-column SQL error"); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 207 && ex.Message.Contains("LabelLayoutJson"))
            { Console.WriteLine("REPRODUCED LOCAL SQL EXCEPTION: " + ex.Message); }
            await db.Database.MigrateAsync();
            await Expect(HttpMethod.Get, $"/api/UnityMap/draft?farmId={farmId}", 200, token: token);
            await Expect(HttpMethod.Get, $"/api/UnityMap/published?farmId={farmId}", 200);
            await Expect(HttpMethod.Get, $"/api/UnityMap/draft?farmId={farmId}", 401);
            var after = await db.FarmMapFeaturePoints.AsNoTracking().OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.FarmMapFeatureId, p.PointOrder, p.X, p.Y }).ToListAsync();
            Check(before.SequenceEqual(after) && await db.FarmMapFeatures.CountAsync() == featureCount,
                "Existing additive migration preserves every feature and exact ordered coordinate");
            Check(await db.FarmDefinitions.AnyAsync(d => d.DefinitionType == "Pathway"), "Pathway remains saved while authenticated draft load succeeds");
        }
        await Expect(HttpMethod.Get, $"/api/UnityMap/draft?farmId={farmId}", 200, token: token);
        await Expect(HttpMethod.Post, "/api/UnityMap/draft", 200, Request(0.7), token);
        await Expect(HttpMethod.Post, $"/api/UnityMap/publish?farmId={farmId}", 200, token: token);
        // Draft needs the bearer header; verify via a scoped request.
        using var draftRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/UnityMap/draft?farmId={farmId}");
        draftRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var draftResponse = await client.SendAsync(draftRequest);
        using var draftJson = JsonDocument.Parse(await draftResponse.Content.ReadAsStringAsync());
        var expectedLayout = Request(0.7).Features[0].LabelLayoutJson;
        Check(draftJson.RootElement.GetProperty("features")[0].GetProperty("labelLayoutJson").GetString() == expectedLayout,
            "Authenticated draft reload returns exact label metadata");
        Check(await db.FarmMapFeatures.AsNoTracking().Where(f => f.FarmMap!.Status == "Published")
            .Select(f => f.LabelLayoutJson).SingleAsync() == expectedLayout, "Publish snapshot retains exact label metadata");
        Check(await FirstX("Published") == 0.7, "Authenticated HTTP Save Draft -> Publish Map persists geometry in SQL Server");
        if (args.Length == 2 && (args[0] == "--point-fixture" || args[0] == "--polygon-fixture" || args[0] == "--pathway-fixture"))
        {
            var poi = JsonSerializer.Deserialize<SaveMapFeatureRequest>(await File.ReadAllTextAsync(args[1]),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var location = new Agriloco1.Models.Inventory.FarmDefinition { FarmId = farmId,
                DisplayName = "Temporary Information", DefinitionType = args[0] == "--pathway-fixture" ? "Pathway" : "Location", IsPublic = true, IsActive = true };
            db.FarmDefinitions.Add(location); await db.SaveChangesAsync();
            poi.FarmId = farmId; poi.FarmDefinitionId = location.Id; poi.LabelSourceFarmDefinitionId = location.Id;
            var pointRequest = new SaveMapRequest { FarmId = farmId, Features = new() { poi } };
            await Expect(HttpMethod.Post, "/api/UnityMap/draft", 200, pointRequest, token);
            await Expect(HttpMethod.Post, $"/api/UnityMap/publish?farmId={farmId}", 200, token: token);
            using var response = await client.GetAsync($"/api/UnityMap/published?farmId={farmId}");
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var point = document.RootElement.GetProperty("features").EnumerateArray().Single();
            Check(point.GetProperty("geometryType").GetString() == poi.GeometryType && point.GetProperty("points").GetArrayLength() == poi.Points.Count,
                "Public published endpoint exposes Unity-authored geometry type and vertex count");
            Check(point.GetProperty("points").EnumerateArray().Select((p,i) => p.GetProperty("x").GetDouble() == poi.Points[i].X && p.GetProperty("y").GetDouble() == poi.Points[i].Y).All(x => x) &&
                point.GetProperty("labelLayoutJson").GetString() == poi.LabelLayoutJson &&
                point.GetProperty("farmDefinitionId").GetInt32() == location.Id,
                "Every ordered vertex, label metadata and hierarchy association survives SQL save/publish exactly");
            if (args[0] == "--pathway-fixture")
            {
                using var farmResponse = await client.GetAsync($"/api/UnityMap/farm?farmId={farmId}");
                using var farmJson = JsonDocument.Parse(await farmResponse.Content.ReadAsStringAsync());
                Check(farmJson.RootElement.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetInt32() == location.Id)
                    .GetProperty("type").GetString() == "Pathway", "Published pathway is linked to the existing public Pathway definition");
            }
            else
            {
            var trackedLocation = await db.FarmDefinitions.FindAsync(location.Id);
            trackedLocation!.Status = "Available"; await db.SaveChangesAsync();
            using var updated = await client.GetAsync($"/api/UnityMap/published?farmId={farmId}");
            using var updatedJson = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
            Check(updatedJson.RootElement.GetProperty("features")[0].GetProperty("status").GetString() == "Available",
                "Live availability changes reach the published polygon without republishing geometry");
            }

        }

    }
    finally { await app.StopAsync(); }
    Console.WriteLine("ALL SQL SERVER MAP SAVE REGRESSIONS PASSED");
}
finally
{
    // Only the unique LocalDB database created above is removed. Never uses application connection settings.
    await db.Database.EnsureDeletedAsync();
}

sealed class SaveFault : SaveChangesInterceptor
{
    public bool Transient;
    public bool Fatal;
    public int Thrown;
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        if (Transient) { Transient = false; Thrown++; throw new TimeoutException("Regression transient failure"); }
        if (Fatal) { Fatal = false; throw new InvalidOperationException("Regression permanent failure"); }
        return new(result);
    }
}

