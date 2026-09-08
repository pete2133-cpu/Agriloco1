using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnityMapController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public UnityMapController(
            AgrilocoContext context)
        {
            _context = context;
        }

        // ============================================================
        // FARM / LIVE DASHBOARD DATA
        // ============================================================
        //
        // GET:
        // /api/UnityMap/farm?farmId=1
        //
        // Used by Unity for:
        // - Farm information
        // - Basemap
        // - FarmDefinitions
        // - Hierarchy
        // - LIVE availability status
        // - Availability channels
        //
        // ============================================================

        [HttpGet("farm")]
        public async Task<IActionResult> GetUnityFarm(
            [FromQuery] int farmId)
        {
            var farm = await _context.Farms
                .AsNoTracking()
                .Where(f =>
                    f.Id == farmId &&
                    f.IsActive)
                .Select(f => new
                {
                    farmId = f.Id,
                    agrilocoId = f.AgrilocoId,
                    farmName = f.Name,

                    latitude = f.Latitude,
                    longitude = f.Longitude,

                    mapImageUrl = f.MapImageUrl,
                    mapImageUploadedAt =
                        f.MapImageUploadedAt
                })
                .FirstOrDefaultAsync();

            if (farm == null)
            {
                return NotFound(new
                {
                    message = "Farm not found.",
                    farmId
                });
            }

            var definitions =
                await _context.FarmDefinitions
                    .AsNoTracking()
                    .Where(x =>
                        x.FarmId == farmId &&
                        x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.DisplayName)
                    .Select(x => new
                    {
                        id = x.Id,

                        farmId = x.FarmId,

                        parentId =
                            x.ParentFarmDefinitionId,

                        definitionId =
                            x.DefinitionId,

                        name =
                            x.DisplayName,

                        type =
                            x.DefinitionType,

                        status =
                            x.Status,

                        isPublic =
                            x.IsPublic,

                        sortOrder =
                            x.SortOrder
                    })
                    .ToListAsync();

            var definitionIds =
                definitions
                    .Select(x => x.id)
                    .ToList();

            var channelLinks =
                await _context.FarmDefinitionChannels
                    .AsNoTracking()
                    .Where(x =>
                        definitionIds.Contains(
                            x.FarmDefinitionId) &&
                        x.IsEnabled)
                    .Join(
                        _context.AvailabilityChannels
                            .AsNoTracking()
                            .Where(x => x.IsActive),

                        link =>
                            link.AvailabilityChannelId,

                        channel =>
                            channel.Id,

                        (link, channel) => new
                        {
                            farmDefinitionId =
                                link.FarmDefinitionId,

                            channelId =
                                channel.Id,

                            channelName =
                                channel.Name,

                            sortOrder =
                                channel.SortOrder
                        })
                    .OrderBy(x => x.sortOrder)
                    .ThenBy(x => x.channelName)
                    .ToListAsync();

            var unityItems =
                definitions
                    .Select(definition => new
                    {
                        id =
                            definition.id,

                        farmId =
                            definition.farmId,

                        parentId =
                            definition.parentId,

                        definitionId =
                            definition.definitionId,

                        name =
                            definition.name,

                        type =
                            definition.type,

                        status =
                            definition.status,

                        isPublic =
                            definition.isPublic,

                        sortOrder =
                            definition.sortOrder,

                        channels =
                            channelLinks
                                .Where(x =>
                                    x.farmDefinitionId ==
                                    definition.id)
                                .Select(x => new
                                {
                                    id =
                                        x.channelId,

                                    name =
                                        x.channelName
                                })
                                .ToList()
                    })
                    .ToList();

            return Ok(new
            {
                farmId =
                    farm.farmId,

                agrilocoId =
                    farm.agrilocoId,

                farmName =
                    farm.farmName,

                latitude =
                    farm.latitude,

                longitude =
                    farm.longitude,

                mapImageUrl =
                    farm.mapImageUrl,

                mapImageUploadedAt =
                    farm.mapImageUploadedAt,

                items =
                    unityItems
            });
        }

        // ============================================================
        // GET SAVED DRAFT
        // ============================================================
        //
        // GET:
        // /api/UnityMap/draft?farmId=1
        //
        // ============================================================

        [HttpGet("draft")]
        public async Task<IActionResult> GetDraft(
            [FromQuery] int farmId)
        {
            return await GetSavedMap(
                farmId,
                "Draft");
        }

        // ============================================================
        // SAVE DRAFT
        // ============================================================
        //
        // POST:
        // /api/UnityMap/draft
        //
        // Saving a draft DOES NOT affect the published customer map.
        //
        // ============================================================

        [HttpPost("draft")]
        public async Task<IActionResult> SaveDraft(
            [FromBody] SaveMapRequest request)
        {
            if (request == null ||
                request.FarmId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "A valid FarmId is required."
                });
            }

            bool farmExists =
                await _context.Farms
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.Id == request.FarmId);

            if (!farmExists)
            {
                return NotFound(new
                {
                    message =
                        "Farm not found.",

                    farmId =
                        request.FarmId
                });
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var draft =
                    await _context.FarmMaps
                        .FirstOrDefaultAsync(x =>
                            x.FarmId ==
                                request.FarmId &&
                            x.Status ==
                                "Draft");

                if (draft == null)
                {
                    draft =
                        new FarmMap
                        {
                            FarmId =
                                request.FarmId,

                            Status =
                                "Draft",

                            CreatedAt =
                                DateTime.UtcNow,

                            UpdatedAt =
                                DateTime.UtcNow,

                            PublishedAt =
                                null
                        };

                    _context.FarmMaps.Add(
                        draft);

                    await _context
                        .SaveChangesAsync();
                }
                else
                {
                    await DeleteFeaturesForMap(
                        draft.Id);

                    draft.UpdatedAt =
                        DateTime.UtcNow;
                }

                int featureCount =
                    0;

                if (request.Features != null)
                {
                    foreach (
                        SaveMapFeatureRequest incoming
                        in request.Features)
                    {
                        FarmMapFeature feature =
                            BuildFeature(
                                draft.Id,
                                incoming);

                        _context
                            .FarmMapFeatures
                            .Add(feature);

                        featureCount++;
                    }
                }

                draft.UpdatedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();

                await transaction
                    .CommitAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Draft map saved.",

                    mapId =
                        draft.Id,

                    farmId =
                        draft.FarmId,

                    featureCount,

                    updatedAt =
                        draft.UpdatedAt
                });
            }
            catch
            {
                await transaction
                    .RollbackAsync();

                throw;
            }
        }

        // ============================================================
        // PUBLISH MAP
        // ============================================================
        //
        // POST:
        // /api/UnityMap/publish?farmId=1
        //
        // Takes a SNAPSHOT of the current saved draft.
        //
        // Further draft edits do not affect the customer map until
        // Publish Map is clicked again.
        //
        // ============================================================

        [HttpPost("publish")]
        public async Task<IActionResult> PublishMap(
            [FromQuery] int farmId)
        {
            if (farmId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "A valid FarmId is required."
                });
            }

            var draft =
                await _context.FarmMaps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.FarmId == farmId &&
                        x.Status == "Draft");

            if (draft == null)
            {
                return BadRequest(new
                {
                    message =
                        "There is no saved draft to publish."
                });
            }

            var draftFeatures =
                await _context.FarmMapFeatures
                    .AsNoTracking()
                    .Where(x =>
                        x.FarmMapId ==
                        draft.Id)
                    .OrderBy(x => x.Id)
                    .ToListAsync();

            var draftFeatureIds =
                draftFeatures
                    .Select(x => x.Id)
                    .ToList();

            var draftPoints =
                await _context.FarmMapFeaturePoints
                    .AsNoTracking()
                    .Where(x =>
                        draftFeatureIds.Contains(
                            x.FarmMapFeatureId))
                    .OrderBy(x =>
                        x.PointOrder)
                    .ToListAsync();

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var published =
                    await _context.FarmMaps
                        .FirstOrDefaultAsync(x =>
                            x.FarmId == farmId &&
                            x.Status ==
                                "Published");

                if (published == null)
                {
                    published =
                        new FarmMap
                        {
                            FarmId =
                                farmId,

                            Status =
                                "Published",

                            CreatedAt =
                                DateTime.UtcNow,

                            UpdatedAt =
                                DateTime.UtcNow,

                            PublishedAt =
                                DateTime.UtcNow
                        };

                    _context.FarmMaps.Add(
                        published);

                    await _context
                        .SaveChangesAsync();
                }
                else
                {
                    await DeleteFeaturesForMap(
                        published.Id);

                    published.UpdatedAt =
                        DateTime.UtcNow;

                    published.PublishedAt =
                        DateTime.UtcNow;
                }

                foreach (
                    FarmMapFeature sourceFeature
                    in draftFeatures)
                {
                    var copiedFeature =
                        new FarmMapFeature
                        {
                            FarmMapId =
                                published.Id,

                            FarmDefinitionId =
                                sourceFeature
                                    .FarmDefinitionId,

                            Purpose =
                                sourceFeature.Purpose,

                            GeometryType =
                                sourceFeature
                                    .GeometryType,

                            Subtype =
                                sourceFeature.Subtype,

                            DisplayPath =
                                sourceFeature
                                    .DisplayPath,

                            LineWidth =
                                sourceFeature.LineWidth,

                            Opacity =
                                sourceFeature.Opacity,

                            ShowLabel =
                                sourceFeature.ShowLabel,

                            LabelSourceFarmDefinitionId =
                                sourceFeature
                                    .LabelSourceFarmDefinitionId,

                            CustomLabel =
                                sourceFeature.CustomLabel,

                            LabelPosition =
                                sourceFeature
                                    .LabelPosition,

                            LabelFontSize =
                                sourceFeature
                                    .LabelFontSize,

                            LabelTextColour =
                                sourceFeature
                                    .LabelTextColour
                        };

                    var featurePoints =
                        draftPoints
                            .Where(x =>
                                x.FarmMapFeatureId ==
                                sourceFeature.Id)
                            .OrderBy(x =>
                                x.PointOrder)
                            .ToList();

                    foreach (
                        FarmMapFeaturePoint sourcePoint
                        in featurePoints)
                    {
                        copiedFeature.Points.Add(
                            new FarmMapFeaturePoint
                            {
                                PointOrder =
                                    sourcePoint
                                        .PointOrder,

                                X =
                                    sourcePoint.X,

                                Y =
                                    sourcePoint.Y
                            });
                    }

                    _context.FarmMapFeatures.Add(
                        copiedFeature);
                }

                published.UpdatedAt =
                    DateTime.UtcNow;

                published.PublishedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();

                await transaction
                    .CommitAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Map published.",

                    mapId =
                        published.Id,

                    farmId =
                        published.FarmId,

                    featureCount =
                        draftFeatures.Count,

                    publishedAt =
                        published.PublishedAt
                });
            }
            catch
            {
                await transaction
                    .RollbackAsync();

                throw;
            }
        }

        // ============================================================
        // GET PUBLISHED MAP
        // ============================================================
        //
        // GET:
        // /api/UnityMap/published?farmId=1
        //
        // This is the endpoint the CUSTOMER VIEWER will eventually use.
        //
        // Geometry/labels come from the published snapshot.
        //
        // Availability status comes LIVE from FarmDefinitions.
        //
        // ============================================================

        [HttpGet("published")]
        public async Task<IActionResult> GetPublishedMap(
            [FromQuery] int farmId)
        {
            return await GetSavedMap(
                farmId,
                "Published");
        }

        // ============================================================
        // OLD UNITY CELL ENDPOINT
        // ============================================================

        [HttpGet("cells")]
        public async Task<IActionResult> GetUnityCells(
            [FromQuery] int farmId)
        {
            var cells =
                await _context.MapCells
                    .AsNoTracking()
                    .Where(m =>
                        m.FarmId == farmId)
                    .Include(m =>
                        m.Crop)
                    .Select(m => new
                    {
                        id =
                            m.Id,

                        farmId =
                            m.FarmId,

                        gridX =
                            m.GridX,

                        gridY =
                            m.GridY,

                        cropId =
                            m.CropId,

                        featureType =
                            m.FeatureType,

                        category =
                            m.Crop != null
                                ? m.Crop.Category
                                : null,

                        variety =
                            m.Crop != null
                                ? m.Crop.Variety
                                : null,

                        availability =
                            m.Crop != null
                                ? m.Crop.Availability
                                : null,

                        colorCode =
                            m.CropId.HasValue
                                ? ColorForCropId(
                                    m.CropId.Value)
                                : "#000000"
                    })
                    .ToListAsync();

            return Ok(cells);
        }

        // ============================================================
        // INTERNAL SAVED MAP READER
        // ============================================================

        private async Task<IActionResult> GetSavedMap(
            int farmId,
            string status)
        {
            var map =
                await _context.FarmMaps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.FarmId == farmId &&
                        x.Status == status);

            if (map == null)
            {
                return Ok(new
                {
                    farmId,

                    status,

                    mapId = 0,

                    updatedAt =
                        (DateTime?)null,

                    publishedAt =
                        (DateTime?)null,

                    features =
                        Array.Empty<object>()
                });
            }

            var features =
                await _context.FarmMapFeatures
                    .AsNoTracking()
                    .Where(x =>
                        x.FarmMapId ==
                        map.Id)
                    .OrderBy(x =>
                        x.Id)
                    .ToListAsync();

            var featureIds =
                features
                    .Select(x => x.Id)
                    .ToList();

            var points =
                await _context.FarmMapFeaturePoints
                    .AsNoTracking()
                    .Where(x =>
                        featureIds.Contains(
                            x.FarmMapFeatureId))
                    .OrderBy(x =>
                        x.PointOrder)
                    .ToListAsync();

            // --------------------------------------------------------
            // CURRENT LIVE STATUS
            // --------------------------------------------------------

            var farmDefinitionIds =
                features
                    .Where(x =>
                        x.FarmDefinitionId > 0)
                    .Select(x =>
                        x.FarmDefinitionId)
                    .Distinct()
                    .ToList();

            var liveDefinitions =
                await _context.FarmDefinitions
                    .AsNoTracking()
                    .Where(x =>
                        farmDefinitionIds.Contains(
                            x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        x.Status,
                        x.IsPublic,
                        x.IsActive
                    })
                    .ToListAsync();

            var resultFeatures =
                features
                    .Select(feature =>
                    {
                        var liveDefinition =
                            liveDefinitions
                                .FirstOrDefault(x =>
                                    x.Id ==
                                    feature
                                        .FarmDefinitionId);

                        string liveStatus =
                            liveDefinition != null
                                ? liveDefinition.Status
                                : "";

                        bool definitionIsPublic =
                            liveDefinition == null ||
                            liveDefinition.IsPublic;

                        bool definitionIsActive =
                            liveDefinition == null ||
                            liveDefinition.IsActive;

                        return new
                        {
                            id =
                                feature.Id,

                            farmId,

                            farmDefinitionId =
                                feature
                                    .FarmDefinitionId,

                            purpose =
                                feature.Purpose,

                            geometryType =
                                feature.GeometryType,

                            subtype =
                                feature.Subtype,

                            displayPath =
                                feature.DisplayPath,

                            lineWidth =
                                feature.LineWidth,

                            opacity =
                                feature.Opacity,

                            showLabel =
                                feature.ShowLabel,

                            labelSourceFarmDefinitionId =
                                feature
                                    .LabelSourceFarmDefinitionId,

                            customLabel =
                                feature.CustomLabel,

                            labelPosition =
                                feature.LabelPosition,

                            labelFontSize =
                                feature.LabelFontSize,

                            labelTextColour =
                                feature
                                    .LabelTextColour,

                            // IMPORTANT:
                            // This is not saved into the geometry.
                            // It is the current dashboard status.
                            status =
                                liveStatus,

                            isPublic =
                                definitionIsPublic,

                            isActive =
                                definitionIsActive,

                            points =
                                points
                                    .Where(x =>
                                        x.FarmMapFeatureId ==
                                        feature.Id)
                                    .OrderBy(x =>
                                        x.PointOrder)
                                    .Select(x =>
                                        new
                                        {
                                            x =
                                                x.X,

                                            y =
                                                x.Y
                                        })
                                    .ToList()
                        };
                    })
                    .ToList();

            return Ok(new
            {
                farmId,

                status =
                    map.Status,

                mapId =
                    map.Id,

                updatedAt =
                    map.UpdatedAt,

                publishedAt =
                    map.PublishedAt,

                features =
                    resultFeatures
            });
        }

        // ============================================================
        // DELETE EXISTING MAP FEATURES
        // ============================================================

        private async Task DeleteFeaturesForMap(
            int farmMapId)
        {
            var oldFeatures =
                await _context.FarmMapFeatures
                    .Where(x =>
                        x.FarmMapId ==
                        farmMapId)
                    .ToListAsync();

            if (oldFeatures.Count == 0)
            {
                return;
            }

            var featureIds =
                oldFeatures
                    .Select(x => x.Id)
                    .ToList();

            var oldPoints =
                await _context.FarmMapFeaturePoints
                    .Where(x =>
                        featureIds.Contains(
                            x.FarmMapFeatureId))
                    .ToListAsync();

            if (oldPoints.Count > 0)
            {
                _context
                    .FarmMapFeaturePoints
                    .RemoveRange(
                        oldPoints);
            }

            _context
                .FarmMapFeatures
                .RemoveRange(
                    oldFeatures);

            await _context
                .SaveChangesAsync();
        }

        // ============================================================
        // BUILD FEATURE FROM UNITY REQUEST
        // ============================================================

        private static FarmMapFeature BuildFeature(
            int farmMapId,
            SaveMapFeatureRequest request)
        {
            var feature =
                new FarmMapFeature
                {
                    FarmMapId =
                        farmMapId,

                    FarmDefinitionId =
                        request
                            .FarmDefinitionId,

                    Purpose =
                        request.Purpose ?? "",

                    GeometryType =
                        request.GeometryType ?? "",

                    Subtype =
                        request.Subtype ?? "",

                    DisplayPath =
                        request.DisplayPath ?? "",

                    LineWidth =
                        request.LineWidth,

                    Opacity =
                        request.Opacity,

                    ShowLabel =
                        request.ShowLabel,

                    LabelSourceFarmDefinitionId =
                        request
                            .LabelSourceFarmDefinitionId,

                    CustomLabel =
                        request.CustomLabel ?? "",

                    LabelPosition =
                        request.LabelPosition ??
                        "Center",

                    LabelFontSize =
                        request.LabelFontSize,

                    LabelTextColour =
                        request.LabelTextColour ??
                        "White"
                };

            if (request.Points != null)
            {
                for (
                    int i = 0;
                    i < request.Points.Count;
                    i++)
                {
                    SaveMapPointRequest incomingPoint =
                        request.Points[i];

                    feature.Points.Add(
                        new FarmMapFeaturePoint
                        {
                            PointOrder =
                                i,

                            X =
                                incomingPoint.X,

                            Y =
                                incomingPoint.Y
                        });
                }
            }

            return feature;
        }

        // ============================================================
        // OLD UNITY COLOR SYSTEM
        // ============================================================

        private static string ColorForCropId(
            int cropId)
        {
            string[] palette =
            {
                "#FF0000",
                "#FFFF00",
                "#00BFFF",
                "#00FF00",
                "#FF00FF",
                "#FFA500",
                "#8A2BE2",
                "#00FFFF",
                "#A52A2A",
                "#808080"
            };

            var idx =
                (cropId - 1) %
                palette.Length;

            if (idx < 0)
            {
                idx = 0;
            }

            return palette[idx];
        }
    }

    // ================================================================
    // UNITY SAVE DTOs
    // ================================================================

    public class SaveMapRequest
    {
        public int FarmId { get; set; }

        public List<SaveMapFeatureRequest> Features { get; set; }
            = new List<SaveMapFeatureRequest>();
    }

    public class SaveMapFeatureRequest
    {
        public int Id { get; set; }

        public int FarmId { get; set; }

        public int FarmDefinitionId { get; set; }

        public string Purpose { get; set; }
            = "";

        public string GeometryType { get; set; }
            = "";

        public string Subtype { get; set; }
            = "";

        public string DisplayPath { get; set; }
            = "";

        public double LineWidth { get; set; }

        public double Opacity { get; set; }

        public bool ShowLabel { get; set; }

        public int LabelSourceFarmDefinitionId { get; set; }

        public string CustomLabel { get; set; }
            = "";

        public string LabelPosition { get; set; }
            = "Center";

        public double LabelFontSize { get; set; }

        public string LabelTextColour { get; set; }
            = "White";

        public List<SaveMapPointRequest> Points { get; set; }
            = new List<SaveMapPointRequest>();
    }

    public class SaveMapPointRequest
    {
        public double X { get; set; }

        public double Y { get; set; }
    }
}