using Agriloco.Api.Controllers;
using Agriloco.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
static class GeoUnityHost
{
    public static async Task Run(SqlServerAgrilocoContext db,int farmId,string fixture)
    {
        var options=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var map=JsonSerializer.Deserialize<SaveMapRequest>(await File.ReadAllTextAsync(fixture),options)!;map.FarmId=farmId;
        var ids=map.Features.SelectMany(f=>new[]{f.FarmDefinitionId,f.LabelSourceFarmDefinitionId}).Where(i=>i>0).Distinct();
        var mapping=new Dictionary<int,int>();
        foreach(int id in ids){var definition=new Agriloco1.Models.Inventory.FarmDefinition{FarmId=farmId,DefinitionType=id==96?"Pathway":"Location",DisplayName="Test definition "+id,IsPublic=true};db.FarmDefinitions.Add(definition);await db.SaveChangesAsync();mapping[id]=definition.Id;}
        foreach(var f in map.Features){f.FarmId=farmId;if(f.FarmDefinitionId>0)f.FarmDefinitionId=mapping[f.FarmDefinitionId];if(f.LabelSourceFarmDefinitionId>0)f.LabelSourceFarmDefinitionId=mapping[f.LabelSourceFarmDefinitionId];}
        await new UnityMapController(db).SaveDraft(map);
        var farm=await db.Farms.SingleAsync(f=>f.Id==farmId);farm.MapImageUrl="/test-basemap.png";farm.MapImageUploadedAt=new DateTime(2026,1,1);await db.SaveChangesAsync();
        var folder=Environment.GetEnvironmentVariable("AGRILOCO_GEO_TEST_FOLDER")??throw new Exception("Missing isolated test folder");
        await File.WriteAllTextAsync(Path.Combine(folder,"ready"),"Isolated LocalDB HTTPS host ready");
        var timeout=DateTime.UtcNow.AddMinutes(10);
        while(!File.Exists(Path.Combine(folder,"done"))&&DateTime.UtcNow<timeout)await Task.Delay(500);
        Console.WriteLine("GEO HOST: stopped; disposable database will be removed.");
    }
}
