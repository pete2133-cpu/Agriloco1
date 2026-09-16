using Agriloco.Api.Security;
using Agriloco.Api.Services;
using System.Security.Claims;
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
        private readonly DefinitionImages _definitionImages;

        public UnityMapController(
            AgrilocoContext context, DefinitionImages? definitionImages = null)
        {
            _context = context;
            _definitionImages = definitionImages ?? new DefinitionImages(context);
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

        // On-farm sales use the existing FarmStore channel, not external FarmersMarket.
        [HttpGet("market")]
        public async Task<IActionResult> GetMarket([FromQuery] int farmId)
        {
            if (!await _context.Farms.AsNoTracking().AnyAsync(f => f.Id == farmId && f.IsActive))
                return NotFound();
            var visible = await PublicDefinitionIdsAsync(farmId);
            var definitions = await _context.FarmDefinitions.AsNoTracking()
                .Where(d => d.FarmId == farmId && visible.Contains(d.Id))
                .OrderBy(d => d.SortOrder).ThenBy(d => d.DisplayName).ToListAsync();
            var assigned = await _context.FarmDefinitionChannels.AsNoTracking()
                .Where(l => l.IsEnabled && visible.Contains(l.FarmDefinitionId))
                .Join(_context.AvailabilityChannels.Where(c => c.IsActive && c.Code == "FarmStore"),
                    l => l.AvailabilityChannelId, c => c.Id, (l, c) => l.FarmDefinitionId)
                .Distinct().ToListAsync();
            var byId = definitions.ToDictionary(d => d.Id);
            var definitionIds = definitions.Where(d => d.DefinitionId.HasValue).Select(d => d.DefinitionId!.Value).ToList();
            var descriptions = await _context.Definitions.AsNoTracking()
                .Where(d => d.IsActive && definitionIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Description);
            var details = await _context.MarketDetails.AsNoTracking().Where(d => assigned.Contains(d.FarmDefinitionId))
                .Select(d => new { d.FarmDefinitionId, d.DisplayName, d.Description, d.Category, hasImage = d.Image != null })
                .ToDictionaryAsync(d => d.FarmDefinitionId);
            // Explicit customer-safe projection: no SKU, barcode, tracking or stock metadata.
            var options = await _context.MarketSellingOptions.AsNoTracking()
                .Where(o => assigned.Contains(o.FarmDefinitionId) && o.IsPublic)
                .OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
                .Select(o => new { o.FarmDefinitionId, id = o.Id, name = o.Name, price = o.Price,
                    currency = o.Currency, sellQuantity = o.SellQuantity, sellUnit = o.SellUnit, packageType = o.PackageType }).ToListAsync();
            var result = definitions.Where(d => assigned.Contains(d.Id)).Select(d =>
            {
                var root = d;
                while (root.ParentFarmDefinitionId.HasValue && byId.TryGetValue(root.ParentFarmDefinitionId.Value, out var parent))
                    root = parent;
                details.TryGetValue(d.Id, out var detail);
                return new { id = d.Id, name = detail?.DisplayName ?? d.DisplayName, status = d.Status,
                    categoryId = root.Id,
                    categoryKey = detail?.Category == null ? "crop:" + root.Id : "category:" + detail.Category.ToUpperInvariant(),
                    categoryName = detail?.Category ?? root.DisplayName,
                    description = detail?.Description ?? (d.DefinitionId.HasValue && descriptions.TryGetValue(d.DefinitionId.Value, out var description)
                        ? description : null),
                    imageUrl = detail?.hasImage == true ? $"/api/UnityMap/market-image?farmId={farmId}&itemId={d.Id}" : null,
                    sellingOptions = options.Where(o => o.FarmDefinitionId == d.Id).Select(o => new {
                        o.id, o.name, o.price, o.currency, o.sellQuantity, o.sellUnit, o.packageType }),
                    subtitle = d.ParentFarmDefinitionId.HasValue && byId.TryGetValue(d.ParentFarmDefinitionId.Value, out var p)
                        ? p.DisplayName : null };
            });
            return Ok(new { farmId, items = result });
        }

        [HttpGet("market-image")]
        public async Task<IActionResult> GetMarketImage([FromQuery] int farmId, [FromQuery] int itemId)
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            if (!await _context.Farms.AnyAsync(f => f.Id == farmId && f.IsActive) ||
                !(await PublicDefinitionIdsAsync(farmId)).Contains(itemId)) return NotFound();
            var assigned = await _context.FarmDefinitionChannels.AnyAsync(l => l.FarmDefinitionId == itemId && l.IsEnabled &&
                _context.AvailabilityChannels.Any(c => c.Id == l.AvailabilityChannelId && c.IsActive && c.Code == "FarmStore"));
            if (!assigned) return NotFound();
            var image = await _context.MarketDetails.AsNoTracking().Where(d => d.FarmDefinitionId == itemId)
                .Select(d => new { d.Image, d.ImageContentType }).FirstOrDefaultAsync();
            return image?.Image == null ? NotFound() : File(image.Image, image.ImageContentType!);
        }

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

            if (!await CanManageFarmAsync(farmId))
            {
                var publicIds = await PublicDefinitionIdsAsync(farmId);
                definitions = definitions.Where(d => publicIds.Contains(d.id)).ToList();
            }

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

            var imageSources = await _definitionImages.ResolveAsync(farmId, publicOnly: true);
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

                        imageUrl = imageSources.TryGetValue(definition.id, out var sourceId)
                            ? DefinitionImages.Url(farmId, sourceId, true) : null,

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

        [FarmMapWrite(IncludeReads = true, AllowEditorToken = true)]
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

        [FarmMapWrite("request.FarmId", AllowEditorToken = true)]
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

            var referenceIds = (request.Features ?? new()).SelectMany(f => new[] { f.FarmDefinitionId, f.LabelSourceFarmDefinitionId })
                .Where(id => id > 0).Distinct().ToArray();
            if (await _context.FarmDefinitions.CountAsync(d => d.FarmId == request.FarmId && referenceIds.Contains(d.Id)) != referenceIds.Length)
                return StatusCode(403);

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

            return await ExecuteMapWriteAsync(() => SaveDraftTransactionAsync(request));
        }

        private async Task<IActionResult> SaveDraftTransactionAsync(SaveMapRequest request)
        {
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
                // A broken connection may also fail rollback. Preserve the original
                // transient exception so the execution strategy can replay the unit.
                try { await transaction.RollbackAsync(); }
                catch { }

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

        [FarmMapWrite("farmId", AllowEditorToken = true)]
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

            return await ExecuteMapWriteAsync(() => PublishMapTransactionAsync(farmId));
        }

        private async Task<IActionResult> PublishMapTransactionAsync(int farmId)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

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

                            LabelLayoutJson = sourceFeature.LabelLayoutJson,

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
                // A broken connection may also fail rollback. Preserve the original
                // transient exception so the execution strategy can replay the unit.
                try { await transaction.RollbackAsync(); }
                catch { }

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
            if (!await _context.Farms.AsNoTracking().AnyAsync(f => f.Id == farmId && f.IsActive))
                return NotFound();

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

            if (status == "Published" && !await CanManageFarmAsync(farmId))
            {
                var publicIds = await PublicDefinitionIdsAsync(farmId);
                features = features.Where(f =>
                    (f.FarmDefinitionId <= 0 || publicIds.Contains(f.FarmDefinitionId)) &&
                    (f.LabelSourceFarmDefinitionId <= 0 || publicIds.Contains(f.LabelSourceFarmDefinitionId))).ToList();
            }

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

                            labelLayoutJson = feature.LabelLayoutJson,

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

        private Task<IActionResult> ExecuteMapWriteAsync(Func<Task<IActionResult>> operation)
        {
            // Azure SQL enables retries. The entire explicit transaction must run
            // inside its execution strategy, not as independently retried commands.
            return _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                // A previous attempt may have assigned identities or accepted deletes
                // before its transaction rolled back. Reload a clean graph on replay.
                _context.ChangeTracker.Clear();
                return await operation();
            });
        }

        private async Task<bool> CanManageFarmAsync(int farmId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var memberId) &&
                await _context.Members.AsNoTracking().AnyAsync(m => m.Id == memberId && m.IsActive &&
                    m.FarmId == farmId && m.Farm != null && m.Farm.IsActive);
        }

        private async Task<HashSet<int>> PublicDefinitionIdsAsync(int farmId)
        {
            var rows = await _context.FarmDefinitions.AsNoTracking().Where(d => d.FarmId == farmId)
                .Select(d => new { d.Id, d.ParentFarmDefinitionId, d.IsPublic, d.IsActive }).ToListAsync();
            var byId = rows.ToDictionary(d => d.Id);
            bool Visible(int id)
            {
                var seen = new HashSet<int>();
                while (byId.TryGetValue(id, out var row) && seen.Add(id))
                {
                    if (!row.IsActive || !row.IsPublic) return false;
                    if (!row.ParentFarmDefinitionId.HasValue) return true;
                    id = row.ParentFarmDefinitionId.Value;
                }
                return false;
            }
            return rows.Where(d => Visible(d.Id)).Select(d => d.Id).ToHashSet();
        }

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

                    LabelLayoutJson = request.LabelLayoutJson,

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

        [System.ComponentModel.DataAnnotations.MaxLength(4096)]
        public string? LabelLayoutJson { get; set; }

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
