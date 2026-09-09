using Agriloco.Api.Services;
using Agriloco.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Agriloco.Api.Dtos;

namespace agriloco.api.Pages.Search
{
    public class CropsModel : PageModel
    {
        private readonly AgrilocoContext _db;
        public Dictionary<int, string> FarmNames { get; private set; } = new();
        public Dictionary<int, string> FarmAddresses { get; private set; } = new();

        public CropsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Variety { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? OfferingType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Availability { get; set; }

        // categories for the autocomplete datalist
        public List<string> CategorySuggestions { get; set; } = new();

        public List<PublicFoodListing> Results { get; set; } = new();

        // NEW: points to plot on the map
        public List<MapPoint> MapPoints { get; set; } = new();

        // NEW: JSON string for the JS map
        public string MapPointsJson { get; set; } = "[]";

        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            try { await LoadResultsAsync(); }
            catch (Exception ex) when (ex is DbException || ex is TaskCanceledException || ex is JsonException)
            {
                Message = "We could not load the food listings. Please try again.";
                Results = new();
            }
        }

        private async Task LoadResultsAsync()
        {
            var list = await PublicFoodCatalog.LoadAsync(_db);
            CategorySuggestions = list.Select(c => c.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList();
            if (!string.IsNullOrWhiteSpace(OfferingType)) list = list.Where(c => c.SalesMethodCodes.Contains(OfferingType.Trim(), StringComparer.OrdinalIgnoreCase)).ToList();
            if (Availability == "Available") list = list.Where(c => PublicFoodCatalog.IsAvailable(c.Availability)).ToList();
            else if (Availability == "NotAvailable") list = list.Where(c => !PublicFoodCatalog.IsAvailable(c.Availability)).ToList();
            else if (!string.IsNullOrWhiteSpace(Availability)) list = list.Where(c => string.Equals(c.Availability, Availability.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            // Read-only farm metadata for the website results.
            var farmIds = list.Select(c => c.FarmId).Distinct().ToList();
            var farms = await _db.Farms.AsNoTracking().Where(f => farmIds.Contains(f.Id))
                .Select(f => new { f.Id, f.Name, f.Address }).ToListAsync();
            FarmNames = farms.ToDictionary(f => f.Id, f => f.Name);
            FarmAddresses = farms.ToDictionary(f => f.Id, f => f.Address ?? "");

            // Client-side contains filters
            if (!string.IsNullOrWhiteSpace(Category))
            {
                var cat = Category.Trim();
                list = list
                    .Where(c => (PublicFoodCatalog.Matches(c.Category, cat) || PublicFoodCatalog.Matches(c.Variety, cat)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Variety))
            {
                var v = Variety.Trim();
                list = list
                    .Where(c => PublicFoodCatalog.Matches(c.Variety, v))
                    .ToList();
            }

            Results = list;

            // NEW: Build map points from results (tries to read Lat/Lng fields if present on DTO)
            MapPoints = BuildMapPointsFromResults(Results);

            MapPointsJson = JsonSerializer.Serialize(MapPoints, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        // ---------- Map helpers ----------

        public class MapPoint
        {
            public int FarmId { get; set; }
            public string FarmDisplayName { get; set; } = "Farm";
            public double Lat { get; set; }
            public double Lng { get; set; }

            public string? Category { get; set; }
            public string? Variety { get; set; }
            public string? Availability { get; set; }
            public string? OfferingType { get; set; }
        }

        private static List<MapPoint> BuildMapPointsFromResults(List<PublicFoodListing> results)
        {
            var points = new List<MapPoint>();
            if (results == null || results.Count == 0) return points;

            foreach (var c in results)
            {
                if (c == null) continue;

                // These property names are what we will look for on CropSearchOut.
                // Later when you add geo to the DTO/API, use one of these names and it will start plotting immediately.
                // e.g. public double? FarmLat {get;set;} public double? FarmLng {get;set;}
                var lat = TryReadDouble(c, "FarmLat") ?? TryReadDouble(c, "Latitude") ?? TryReadDouble(c, "Lat");
                var lng = TryReadDouble(c, "FarmLng") ?? TryReadDouble(c, "Longitude") ?? TryReadDouble(c, "Lng") ?? TryReadDouble(c, "Lon");

                if (!lat.HasValue || !lng.HasValue) continue;

                // basic sanity check for valid coordinate ranges
                if (lat.Value < -90 || lat.Value > 90) continue;
                if (lng.Value < -180 || lng.Value > 180) continue;

                points.Add(new MapPoint
                {
                    FarmId = c.FarmId,
                    FarmDisplayName = $"Farm #{c.FarmId}",
                    Lat = lat.Value,
                    Lng = lng.Value,
                    Category = c.Category,
                    Variety = c.Variety,
                    Availability = c.Availability,
                    OfferingType = c.OfferingType
                });
            }

            // de-duplicate farms so you don’t stack 20 markers on the same farm
            // (keeps first point per FarmId)
            points = points
                .GroupBy(p => p.FarmId)
                .Select(g => g.First())
                .ToList();

            return points;
        }

        private static double? TryReadDouble(object obj, string propName)
        {
            var t = obj.GetType();
            var p = t.GetProperty(propName);
            if (p == null) return null;

            var v = p.GetValue(obj);
            if (v == null) return null;

            try
            {
                if (v is double d) return d;
                if (v is float f) return (double)f;
                if (v is decimal m) return (double)m;
                if (v is int i) return i;
                if (v is long l) return l;

                if (v is string s && double.TryParse(s, out var parsed))
                    return parsed;

                return Convert.ToDouble(v);
            }
            catch
            {
                return null;
            }
        }
    }
}