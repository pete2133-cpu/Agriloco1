using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Agriloco.Api.Services;
using Agriloco1.Models.Inventory;
using Agriloco1.Pages.Farmer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

string database="AgrilocoPathwayRegression_"+Guid.NewGuid().ToString("N");
var options=new DbContextOptionsBuilder<SqlServerAgrilocoContext>().UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={database};Integrated Security=True;TrustServerCertificate=True").Options;
await using var db=new SqlServerAgrilocoContext(options);
void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
try
{
    await db.Database.MigrateAsync();
    var protectedFarm=new Farm{Name="Protected Farm 1",AgrilocoId="protected"};
    var farm=new Farm{Name="Pathway test farm",AgrilocoId="pathway-test"};
    db.Farms.AddRange(protectedFarm,farm);await db.SaveChangesAsync();
    var protectedItem=new FarmDefinition{FarmId=protectedFarm.Id,DisplayName="Existing crop",DefinitionType="Product",Status="Available"};
    db.FarmDefinitions.Add(protectedItem);await db.SaveChangesAsync();
    string baseline=JsonSerializer.Serialize(await db.FarmDefinitions.AsNoTracking().SingleAsync(d=>d.Id==protectedItem.Id));
    var member=new Member{FarmId=farm.Id,Username="isolated-pathway-test",PasswordHash=Array.Empty<byte>()};db.Members.Add(member);await db.SaveChangesAsync();
    var page=new AddModel(db){FarmId=farm.Id};
    Check(page.DefinitionTypes.Contains("Pathway"),"existing Add/Edit Type option list includes Pathway");
    int? parent=null;int pathwayId=0;
    foreach(string type in page.DefinitionTypes)
    {
        var create=new AddModel(db){FarmId=farm.Id,ParentId=parent,
            NewItem=new AddModel.NewDefinitionInput{DisplayName=type=="Pathway"?"Main Orchard Path":"Test "+type,DefinitionType=type,IsPublic=true,Notes="Fixture"}};
        Check(await create.OnPostCreateAsync() is RedirectToPageResult,type+" creates via existing Razor handler");
        var stored=await db.FarmDefinitions.AsNoTracking().SingleAsync(d=>d.FarmId==farm.Id&&d.DefinitionType==type);
        Check(stored.IsPublic&&stored.DefinitionType==type&&stored.ParentFarmDefinitionId==parent,type+" reloads with type, visibility and hierarchy intact");
        if(type=="Product")parent=stored.Id;
        if(type=="Pathway"){pathwayId=stored.Id;Check(stored.Status=="","Pathway has no crop availability status");}
    }
    var all=await db.FarmDefinitions.Where(d=>d.FarmId==farm.Id).ToListAsync();
    var pathway=all.Single(d=>d.Id==pathwayId);var product=all.Single(d=>d.Id==parent);
    FarmHierarchyChanges.ApplyStatus(all,product,"Coming Soon");
    Check(pathway.Status==""&&all.Where(d=>!d.IsPathway).All(d=>d.Status=="Coming Soon"),"crop branch propagation unchanged except Pathway exclusion");
    FarmHierarchyChanges.ApplyStatus(all,pathway,"Available");
    Check(pathway.Status==""&&product.Status=="Coming Soon","direct Pathway status update does not affect crop ancestors");
    await db.SaveChangesAsync();
    var edit=new AddModel(db){FarmId=farm.Id,EditItem=new AddModel.EditDefinitionInput{Id=pathwayId,DisplayName="Main Orchard Path",DefinitionType="Pathway",IsPublic=true,ParentFarmDefinitionId=parent,Notes="Edited"}};
    Check(await edit.OnPostEditAsync() is RedirectToPageResult,"Pathway saves through existing edit handler");
    var dashboard=new DashboardModel(db,null!,null!,Microsoft.Extensions.Logging.Abstractions.NullLogger<DashboardModel>.Instance);
    int channelId=await db.AvailabilityChannels.Select(c=>c.Id).FirstAsync();
    await dashboard.OnPostChannelAsync(farm.Id,product.Id,channelId,true);
    await dashboard.OnPostChannelAsync(farm.Id,pathwayId,channelId,true);
    Check(!await db.FarmDefinitionChannels.AnyAsync(c=>c.FarmDefinitionId==pathwayId)&&
        await db.FarmDefinitionChannels.AnyAsync(c=>c.FarmDefinitionId==product.Id&&c.IsEnabled),
        "Pathway cannot receive crop channels; existing product channel behaviour remains");
    var builder=WebApplication.CreateBuilder(new WebApplicationOptions{ContentRootPath=Directory.GetCurrentDirectory(),ApplicationName=typeof(AddModel).Assembly.FullName});
    builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Services.AddScoped<AgrilocoContext>(_=>new SqlServerAgrilocoContext(options));
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();builder.Services.AddAuthorization();
    builder.Services.AddRazorPages();builder.Services.AddControllers();
    await using var app=builder.Build();app.UseAuthentication();app.UseAuthorization();app.MapRazorPages();app.MapControllers();
    // Fixture-only login exists solely on this loopback test host, never in production code.
    app.MapGet("/fixture-login",async (HttpContext context)=>await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,member.Id.ToString())},CookieAuthenticationDefaults.AuthenticationScheme))));
    await app.StartAsync();
    try
    {
        string url=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client=new HttpClient(new HttpClientHandler{CookieContainer=new CookieContainer()}){BaseAddress=new Uri(url)};
        await client.GetAsync("/fixture-login");
        string html=await client.GetStringAsync($"/Farmer/Add?farmId={farm.Id}");
        var dropdown=Regex.Match(html,"<select[^>]*name=\"NewItem.DefinitionType\"[\\s\\S]*?</select>").Value;
        Check(dropdown.Contains("Pathway"),"rendered /Farmer/Add Type dropdown contains Pathway");
        using var publicClient=new HttpClient{BaseAddress=new Uri(url)};
        using var json=JsonDocument.Parse(await publicClient.GetStringAsync($"/api/UnityMap/farm?farmId={farm.Id}"));
        var item=json.RootElement.GetProperty("items").EnumerateArray().Single(i=>i.GetProperty("id").GetInt32()==pathwayId);
        Check(item.GetProperty("type").GetString()=="Pathway"&&item.GetProperty("isPublic").GetBoolean()&&item.GetProperty("parentId").GetInt32()==parent,
            "anonymous Unity-facing API exposes Pathway with existing IDs and hierarchy");
    }
    finally{await app.StopAsync();}
    db.ChangeTracker.Clear();
    Check(JsonSerializer.Serialize(await db.FarmDefinitions.SingleAsync(d=>d.Id==protectedItem.Id))==baseline,"isolated Farm 1 baseline unchanged");
    Check(!db.Database.HasPendingModelChanges(),"no SQL Server migration required");
    Console.WriteLine("ALL PATHWAY CHECKS PASSED");
}
finally{await db.Database.EnsureDeletedAsync();}



