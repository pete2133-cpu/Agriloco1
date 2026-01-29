using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco.Api.Pages.Farmer
{
    public class GridPainterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GridPainterModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int? FarmId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int W { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public int H { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string Mode { get; set; } = "paint";

        [BindProperty(SupportsGet = true)]
        public int SelectedCropId { get; set; } = 0;

        public string? Message { get; set; }

        public List<PublicFarmOut> Farms { get; set; } = new();
        public List<CropPublicOut> Crops { get; set; } = new();

        public Dictionary<int, string> ColorLookup { get; set; } = new();
        public Dictionary<string, MapCellOut> CellLookup { get; set; } = new();

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

            var farms = await client.GetFromJsonAsync<PublicFarmOut[]>("/api/Farms/public");
            Farms = (farms ?? Array.Empty<PublicFarmOut>()).ToList();

            if (FarmId == null)
                return;

            var crops = await client.GetFromJsonAsync<CropPublicOut[]>($"/api/Crops/byfarm/{FarmId.Value}");
            Crops = (crops ?? Array.Empty<CropPublicOut>()).ToList();

            if (Crops.Count > 0)
            {
                ColorLookup = Crops.ToDictionary(c => c.Id, c => ColorForCropId(c.Id));

                if (SelectedCropId == 0 || !ColorLookup.ContainsKey(SelectedCropId))
                    SelectedCropId = Crops[0].Id;
            }
            else
            {
                ColorLookup = new Dictionary<int, string>();
                SelectedCropId = 0;
            }

            var cells = await client.GetFromJsonAsync<MapCellOut[]>($"/api/Map/cells?farmId={FarmId.Value}&onlyAvailable=false");
            var cellList = (cells ?? Array.Empty<MapCellOut>()).ToList();

            CellLookup = new Dictionary<string, MapCellOut>();
            foreach (var cell in cellList)
            {
                if (cell.GridX < 0 || cell.GridY < 0) continue;
                if (cell.GridX >= W || cell.GridY >= H) continue;

                var key = $"{cell.GridX},{cell.GridY}";
                CellLookup[key] = cell;
            }
        }

        public async Task<IActionResult> OnPostAsync(
            int FarmId,
            int W,
            int H,
            string Mode,
            int SelectedCropId,
            int GridX,
            int GridY,

            string? MapAddress,
            string? MapZoom,
            string? MapH,
            string? GridScale,

            string? MapLat,
            string? MapLng,
            string? MapHeading,
            string? MapTilt)
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

            int? cropIdToUse = null;
            string featureTypeToUse;

            if (string.Equals(Mode, "paint", StringComparison.OrdinalIgnoreCase))
            {
                cropIdToUse = SelectedCropId == 0 ? null : SelectedCropId;
                featureTypeToUse = "Crop";
            }
            else if (string.Equals(Mode, "label", StringComparison.OrdinalIgnoreCase))
            {
                cropIdToUse = SelectedCropId == 0 ? null : SelectedCropId;
                featureTypeToUse = "RowLabel";
            }
            else
            {
                cropIdToUse = null;
                featureTypeToUse = "Empty";
            }

            var payload = new MapCellCreateIn
            {
                FarmId = FarmId,
                CropId = cropIdToUse,
                GridX = GridX,
                GridY = GridY,
                FeatureType = featureTypeToUse
            };

            var resp = await client.PostAsJsonAsync("/api/Map/cells", payload);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Message = "Could not update tile. " + body;
            }

            // ✅ IMPORTANT: force the page path so routing can’t get confused and produce a dead URL.
            return RedirectToPage("/Farmer/GridPainter", new
            {
                FarmId = FarmId,
                W = W,
                H = H,
                Mode = Mode,
                SelectedCropId = SelectedCropId,

                MapAddress = MapAddress,
                MapZoom = MapZoom,
                MapH = MapH,
                GridScale = GridScale,

                MapLat = MapLat,
                MapLng = MapLng,
                MapHeading = MapHeading,
                MapTilt = MapTilt
            });
        }

        public static string ColorForCropId(int cropId)
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

            var idx = (cropId - 1) % palette.Length;
            if (idx < 0) idx = 0;
            return palette[idx];
        }

        public string TileBgCss(int x, int y)
        {
            var key = $"{x},{y}";
            if (!CellLookup.TryGetValue(key, out var cell) || cell == null)
                return "background: transparent;";

            if (string.Equals(cell.FeatureType, "RowLabel", StringComparison.OrdinalIgnoreCase))
            {
                return "background: repeating-linear-gradient(45deg, rgba(0,0,0,0.12), rgba(0,0,0,0.12) 6px, rgba(0,0,0,0.04) 6px, rgba(0,0,0,0.04) 12px);";
            }

            if (string.Equals(cell.FeatureType, "Empty", StringComparison.OrdinalIgnoreCase))
                return "background: transparent;";

            if (cell.CropId.HasValue && cell.CropId.Value > 0)
                return $"background: {ColorForCropId(cell.CropId.Value)};";

            return "background: transparent;";
        }

        public bool IsLabel(int x, int y)
        {
            var key = $"{x},{y}";
            return CellLookup.TryGetValue(key, out var cell)
                   && cell != null
                   && string.Equals(cell.FeatureType, "RowLabel", StringComparison.OrdinalIgnoreCase);
        }
    }
}