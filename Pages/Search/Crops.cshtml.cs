using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Agriloco.Api.Dtos;

namespace agriloco.api.Pages.Search
{
    public class CropsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CropsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

        public List<CropSearchOut> Results { get; set; } = new();

        // NEW: points to plot on the map
        public List<MapPoint> MapPoints { get; set; } = new();

        // NEW: JSON string for the JS map
        public string MapPointsJson { get; set; } = "[]";

        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

            // Load category suggestions for datalist
            var catsResp = await client.GetAsync("api/Crops/categories");
            if (catsResp.IsSuccessStatusCode)
            {
                var cats = await catsResp.Content.ReadFromJsonAsync<List<string>>();
                CategorySuggestions = cats ?? new List<string>();
            }
            else
            {
                CategorySuggestions = new List<string>();
            }

            // Build API URL with optional query parameters
            var qs = new List<string>();

            if (!string.IsNullOrWhiteSpace(OfferingType))
                qs.Add($"offeringType={Uri.EscapeDataString(OfferingType.Trim())}");

            if (!string.IsNullOrWhiteSpace(Availability))
                qs.Add($"availability={Uri.EscapeDataString(Availability.Trim())}");

            var url = "api/Crops/public";
            if (qs.Count > 0)
                url += "?" + string.Join("&", qs);

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                Message = $"Error loading crops: {(int)resp.StatusCode} {resp.ReasonPhrase}";
                Results = new List<CropSearchOut>();
                MapPoints = new List<MapPoint>();
                MapPointsJson = "[]";
                return;
            }

            var crops = await resp.Content.ReadFromJsonAsync<List<CropSearchOut>>();
            var list = crops ?? new List<CropSearchOut>();

            // Client-side contains filters
            if (!string.IsNullOrWhiteSpace(Category))
            {
                var cat = Category.Trim();
                list = list
                    .Where(c => (c.Category ?? "").Contains(cat, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Variety))
            {
                var v = Variety.Trim();
                list = list
                    .Where(c => (c.Variety ?? "").Contains(v, StringComparison.OrdinalIgnoreCase))
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

        private static List<MapPoint> BuildMapPointsFromResults(List<CropSearchOut> results)
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