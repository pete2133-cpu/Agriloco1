using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Agriloco.Api.Controllers;
using Agriloco.Api.Data;
using Agriloco.Georeferencing;
using Microsoft.EntityFrameworkCore;

static class GeoRegression
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("GEO PASS: "+text);}
    public static async Task Run(HttpClient client,SqlServerAgrilocoContext db,string token,int farmId,string fixture)
    {
        var json=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var map=JsonSerializer.Deserialize<SaveMapRequest>(await File.ReadAllTextAsync(fixture),json)!;map.FarmId=farmId;
        var ids=map.Features.SelectMany(f=>new[]{f.FarmDefinitionId,f.LabelSourceFarmDefinitionId}).Where(id=>id>0).Distinct().ToArray();
        var mapping=new Dictionary<int,int>();
        foreach(int id in ids){var d=new Agriloco1.Models.Inventory.FarmDefinition {FarmId=farmId,DefinitionType=id==96?"Pathway":"Location",DisplayName="Isolated definition "+id,IsPublic=true};db.FarmDefinitions.Add(d);await db.SaveChangesAsync();mapping[id]=d.Id;}
        foreach(var f in map.Features){f.FarmId=farmId;if(f.FarmDefinitionId>0)f.FarmDefinitionId=mapping[f.FarmDefinitionId];if(f.LabelSourceFarmDefinitionId>0)f.LabelSourceFarmDefinitionId=mapping[f.LabelSourceFarmDefinitionId];}
        async Task<string> Send(HttpMethod method,string route,int code,object? body=null,bool authenticated=true)
        {using var req=new HttpRequestMessage(method,route);if(authenticated)req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);if(body!=null)req.Content=JsonContent.Create(body);using var res=await client.SendAsync(req);var text=await res.Content.ReadAsStringAsync();Check((int)res.StatusCode==code,$"{method} {route}: {code}");return text;}
        await Send(HttpMethod.Post,"/api/UnityMap/draft",200,map);
        string draftRoute=$"/api/UnityMap/draft?farmId={farmId}",geo=$"/api/UnityEditorGeoreference?farmId={farmId}",pub=$"/api/UnityMap/georeference?farmId={farmId}";
        string before=await Send(HttpMethod.Get,draftRoute,200);
        Check(JsonDocument.Parse(before).RootElement.GetProperty("features").GetArrayLength()==31,"actual 31-feature Farm 1 fixture loads in isolated farm");
        var farm=await db.Farms.SingleAsync(f=>f.Id==farmId);farm.MapImageUrl="/uploads/isolated-calibration.png";farm.MapImageUploadedAt=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);await db.SaveChangesAsync();
        var request=new GeoreferenceRequest {MapImageUrl=farm.MapImageUrl,MapImageUploadedAt=farm.MapImageUploadedAt};
        await Send(HttpMethod.Get,geo,401,authenticated:false);await Send(HttpMethod.Put,geo,401,request,false);
        await Send(HttpMethod.Get,$"/api/UnityEditorGeoreference?farmId={farmId+999}",403);
        await Send(HttpMethod.Put,$"/api/UnityEditorGeoreference?farmId={farmId+999}",403,request);
        await Send(HttpMethod.Get,geo,200);
        const double latitude=45,longitude=10,radius=6378137;
        foreach(var (e,n) in new[]{(-100d,-100d),(100d,-100d),(100d,100d),(-100d,100d),(0d,0d)})
            request.Points.Add(new GeoReferencePoint {Id=Guid.NewGuid().ToString("N"),Name="Temporary reference",Latitude=latitude+n/radius*180/Math.PI,Longitude=longitude+e/(radius*Math.Cos(latitude*Math.PI/180))*180/Math.PI,NormalizedMapX=.5+.002*e-.001*n,NormalizedMapY=.5+.001*e+.002*n});
        Check(GeoCalibration.Coordinate("43°33'58.58\"N",true,out var parsed)&&Math.Abs(parsed-(43+33d/60+58.58/3600))<1e-10,"DMS parsing");
        Check(!GeoCalibration.Coordinate("91",true,out _)&&!GeoCalibration.Coordinate("NaN",false,out _)&&!GeoCalibration.Coordinate("43°99'0\"N",true,out _),"invalid coordinates rejected");
        var t=GeoCalibration.Fit(request.Points,out var quality);Check(t!=null&&t.RmsErrorMeters<1e-7,"all five references recover rotation/scale/translation with tiny residual");
        foreach(var p in request.Points){t!.Project(p.Latitude,p.Longitude,out var x,out var y);Check(Math.Abs(x-p.NormalizedMapX)<1e-10&&Math.Abs(y-p.NormalizedMapY)<1e-10,"GPS round-trips to expected normalized location");}
        request.Points[4].NormalizedMapX+=.05;
        var noisy=GeoCalibration.Fit(request.Points,out _)!;Check(noisy.RmsErrorMeters>1&&Math.Abs(noisy.X[0]-t!.X[0])>.001,"fifth imperfect reference participates in least squares and reports residual error");request.Points[4].NormalizedMapX-=.05;
        Check(GeoCalibration.Fit(request.Points.Take(2).ToList(),out _)==null,"two points never provide calibration");
        var bad=request.Points.Take(3).Select(p=>new GeoReferencePoint{Id=p.Id,Name=p.Name,Latitude=45,Longitude=p.Longitude,NormalizedMapX=p.NormalizedMapX,NormalizedMapY=p.NormalizedMapY}).ToList();
        Check(GeoCalibration.Fit(bad,out _)==null,"collinear GPS references rejected");
        string saved=await Send(HttpMethod.Put,geo,200,request);
        var response=JsonDocument.Parse(saved).RootElement;
        var reloaded=JsonSerializer.Deserialize<List<GeoReferencePoint>>(response.GetProperty("points"),json)!;
        Check(JsonSerializer.Serialize(reloaded,json)==JsonSerializer.Serialize(request.Points,json),"all reference IDs, names, normalized coordinates, GPS and source reload exactly via fresh HTTP scope");
        await Send(HttpMethod.Put,geo,409,request);
        request.Revision=response.GetProperty("revision").GetGuid();
        var publicResult=JsonDocument.Parse(await Send(HttpMethod.Get,pub,200,authenticated:false)).RootElement;
        Check(publicResult.GetProperty("transform").GetProperty("projection").GetString()=="LocalEquirectangularAffine"&&!publicResult.TryGetProperty("points",out _),"public reusable transform contains no private point names or ordinary map features");
        await Send(HttpMethod.Post,$"/api/UnityMap/publish?farmId={farmId}",200);
        Check(saved==await Send(HttpMethod.Get,geo,200),"republishing does not overwrite farm calibration");
        request.Points.RemoveAt(4);saved=await Send(HttpMethod.Put,geo,200,request);request.Revision=JsonDocument.Parse(saved).RootElement.GetProperty("revision").GetGuid();
        Check(before==await Send(HttpMethod.Get,draftRoute,200),"save/edit/delete calibration leaves all 31 map features, Pumpkin Path geometry and label metadata byte-identical");
        farm.MapImageUploadedAt=farm.MapImageUploadedAt!.Value.AddDays(1);await db.SaveChangesAsync();
        Check(JsonDocument.Parse(await Send(HttpMethod.Get,pub,200,authenticated:false)).RootElement.GetProperty("transform").ValueKind==JsonValueKind.Null,"new basemap invalidates old calibration rather than misplacing GPS");
        await Send(HttpMethod.Put,geo,409,request);

    }
}

