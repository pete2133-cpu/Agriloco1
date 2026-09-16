using System.Text.Json;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Agriloco.Api.Security;
using Agriloco.Georeferencing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Agriloco.Api.Controllers;

public sealed class GeoreferenceRequest
{
    public Guid Revision { get; set; }
    public string? MapImageUrl { get; set; }
    public DateTime? MapImageUploadedAt { get; set; }
    public List<GeoReferencePoint> Points { get; set; } = new();
}
[ApiController]
public sealed class UnityGeoreferenceController(AgrilocoContext db) : ControllerBase
{
    static bool Matches(Farm f, string? url, DateTime? time) => f.MapImageUrl==url && f.MapImageUploadedAt==time;
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [HttpGet("api/UnityEditorGeoreference")]
    [FarmMapWrite(IncludeReads=true, AllowEditorToken=true)]
    public async Task<IActionResult> Read([FromQuery] int farmId)
    {
        var farm=await db.Farms.AsNoTracking().SingleAsync(f=>f.Id==farmId);
        var row=await db.Set<FarmGeoreference>().AsNoTracking().SingleOrDefaultAsync(r=>r.FarmId==farmId);
        var points=row==null?new List<GeoReferencePoint>():JsonSerializer.Deserialize<List<GeoReferencePoint>>(row.PointsJson,Json)!;
        bool matches=row==null||Matches(farm,row.MapImageUrl,row.MapImageUploadedAt);
        var transform=GeoCalibration.Fit(points,out var status);
        return Ok(new {farmId,revision=row?.Revision??Guid.Empty,farm.MapImageUrl,farm.MapImageUploadedAt,
            points,basemapMatches=matches,transform=matches?transform:null,
            status=matches?status:"Basemap changed. Review/reselect reference points before saving calibration."});
    }
    [HttpPut("api/UnityEditorGeoreference")]
    [FarmMapWrite(IncludeReads=true, AllowEditorToken=true)]
    [RequestSizeLimit(131072)]
    public async Task<IActionResult> Save([FromQuery] int farmId,[FromBody] GeoreferenceRequest request)
    {
        if(request.Points==null||request.Points.Count>100||request.Points.Any(p=>!GeoCalibration.Valid(p))||request.Points.Select(p=>p.Id).Distinct().Count()!=request.Points.Count)
            return BadRequest("Supply at most 100 valid, uniquely identified reference points.");
        var farm=await db.Farms.AsNoTracking().SingleAsync(f=>f.Id==farmId);
        if(string.IsNullOrEmpty(farm.MapImageUrl)||!Matches(farm,request.MapImageUrl,request.MapImageUploadedAt))
            return Conflict("Basemap changed. Reload the farm before calibrating.");
        var row=await db.Set<FarmGeoreference>().SingleOrDefaultAsync(r=>r.FarmId==farmId);
        if((row?.Revision??Guid.Empty)!=request.Revision)return Conflict("Calibration changed on another device. Reload before saving.");
        if(row==null){row=new FarmGeoreference{FarmId=farmId};db.Add(row);}
        row.PointsJson=JsonSerializer.Serialize(request.Points,Json);row.MapImageUrl=farm.MapImageUrl;
        row.MapImageUploadedAt=farm.MapImageUploadedAt;row.Revision=Guid.NewGuid();row.UpdatedAt=DateTime.UtcNow;
        try{await db.SaveChangesAsync();}
        catch(DbUpdateConcurrencyException){return Conflict("Calibration changed on another device. Reload before saving.");}
        catch(DbUpdateException ex) when(ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number==2601||sql.Number==2627))
        {return Conflict("Calibration was created on another device. Reload before saving.");}
        return await Read(farmId);
    }
    // Public transformation only, not ordinary map features or private reference-point names.
    [HttpGet("api/UnityMap/georeference")]
    public async Task<IActionResult> Public([FromQuery] int farmId)
    {
        var farm=await db.Farms.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==farmId&&f.IsActive);
        if(farm==null)return NotFound();
        var row=await db.Set<FarmGeoreference>().AsNoTracking().SingleOrDefaultAsync(r=>r.FarmId==farmId);
        GeoTransform? transform=null;string status="More reference points required.";
        if(row!=null&&Matches(farm,row.MapImageUrl,row.MapImageUploadedAt))transform=GeoCalibration.Fit(JsonSerializer.Deserialize<List<GeoReferencePoint>>(row.PointsJson,Json)!,out status);
        else if(row!=null)status="Basemap changed. Calibration requires review.";
        return Ok(new {farmId,farm.MapImageUrl,farm.MapImageUploadedAt,transform,status});
    }
}
