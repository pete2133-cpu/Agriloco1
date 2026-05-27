using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace agriloco.api.Pages.Farmer
{
    public class DashboardModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AgrilocoContext _db;
        private readonly IWebHostEnvironment _env;

        public DashboardModel(IHttpClientFactory httpClientFactory, AgrilocoContext db, IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
            _env = env;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        // ----------------------------
        // Farm info (Update section)
        // ----------------------------
        [BindProperty]
        public FarmEditInput FarmEdit { get; set; } = new();

        // Show current geo on dashboard
        public double? CurrentLat { get; set; }
        public double? CurrentLng { get; set; }

        // ----------------------------
        // Add Crop section
        // ----------------------------
        [BindProperty]
        public CropCreateIn NewCrop { get; set; } = new();

        // ----------------------------
        // Add Crop dropdown data
        // ----------------------------
        public List<string> CategoryOptions { get; set; } = new();
        public Dictionary<string, List<string>> VarietyOptionsByCategory { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
        public string VarietyOptionsJson { get; set; } = "{}";

        // ----------------------------
        // Manage Crop Assets section
        // ----------------------------
        public List<CropSearchOut> FarmCrops { get; set; } = new();

        public string? Message { get; set; }

        // ----------------------------
        // Basemap upload section
        // ----------------------------
        [BindProperty]
        public IFormFile? MapImageFile { get; set; }

        public string? UploadMessage { get; set; }
        public string? CurrentMapImageUrl { get; set; }

        // ============================================================
        // GET
        // ============================================================
        public async Task OnGetAsync()
        {
            if (FarmId <= 0) FarmId = 1;

            NewCrop.FarmId = FarmId;

            LoadFarmEditFromDb();
            LoadCurrentMapImageUrl();
            await LoadFarmCropsAsync();

            await LoadCategoryAndVarietyOptions();
        }

        // ============================================================
        // POST: Update Farm Info
        // ============================================================
        public async Task<IActionResult> OnPostUpdateFarmAsync()
        {
            if (FarmId <= 0) FarmId = 1;

            var farm = _db.Farms.FirstOrDefault(f => f.Id == FarmId);
            if (farm == null)
            {
                Message = "Farm not found.";
                LoadFarmEditFromDb();
                LoadCurrentMapImageUrl();
                await LoadFarmCropsAsync();
                await LoadCategoryAndVarietyOptions();
                return Page();
            }

            farm.Name = (FarmEdit.Name ?? "").Trim();
            farm.Address = (FarmEdit.Address ?? "").Trim();
            farm.ContactMethod1 = (FarmEdit.ContactMethod1 ?? "").Trim();

            await _db.SaveChangesAsync();

            await TouchFarmProfileAsync(farm.Id);

            Message = "Farm information updated.";

            LoadFarmEditFromDb();
            LoadCurrentMapImageUrl();
            await LoadFarmCropsAsync();
            await LoadCategoryAndVarietyOptions();
            return Page();
        }

        // ============================================================
        // POST: Add Crop
        // ============================================================
        public async Task<IActionResult> OnPostAddCropAsync()
        {
            if (FarmId <= 0) FarmId = 1;

            NewCrop.FarmId = FarmId;

            var client = CreateSiteClient();

            var response = await client.PostAsJsonAsync("api/Crops", NewCrop);
            if (!response.IsSuccessStatusCode)
            {
                Message = $"Error creating crop: {(int)response.StatusCode} {response.ReasonPhrase}";
                LoadFarmEditFromDb();
                LoadCurrentMapImageUrl();
                await LoadFarmCropsAsync();
                await LoadCategoryAndVarietyOptions();
                return Page();
            }

            Message = "Crop added.";

            await TouchFarmProfileAsync(FarmId);

            NewCrop = new CropCreateIn { FarmId = FarmId };

            LoadFarmEditFromDb();
            LoadCurrentMapImageUrl();
            await LoadFarmCropsAsync();
            await LoadCategoryAndVarietyOptions();
            return Page();
        }

        // ============================================================
        // POST: Save Row (Crop Assets)
        // ============================================================
        public async Task<IActionResult> OnPostSaveRowAsync(
            int cropId,
            string? availability,
            string? offeringType,
            int? yearPlanted,
            string? rootstock,
            string? notes)
        {
            if (FarmId <= 0) FarmId = 1;

            var client = CreateSiteClient();

            // 1) Update Availability
            var availPayload = new CropAvailabilityUpdateIn
            {
                Availability = string.IsNullOrWhiteSpace(availability) ? null : availability.Trim()
            };

            var availResp = await client.PutAsJsonAsync($"api/Crops/{cropId}/availability", availPayload);
            if (!availResp.IsSuccessStatusCode)
            {
                Message = $"Error saving Availability: {(int)availResp.StatusCode} {availResp.ReasonPhrase}";
                LoadFarmEditFromDb();
                LoadCurrentMapImageUrl();
                await LoadFarmCropsAsync();
                await LoadCategoryAndVarietyOptions();
                return Page();
            }

            // 2) Update details (OfferingType + YearPlanted + Rootstock + Notes)
            int? normalizedYear = (yearPlanted.HasValue && yearPlanted.Value > 0) ? yearPlanted : null;

            var detailsPayload = new CropDetailsUpdateIn
            {
                OfferingType = string.IsNullOrWhiteSpace(offeringType) ? null : offeringType.Trim(),
                YearPlanted = normalizedYear,
                Rootstock = string.IsNullOrWhiteSpace(rootstock) ? null : rootstock.Trim(),
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };

            var detailsResp = await client.PutAsJsonAsync($"api/Crops/{cropId}/details", detailsPayload);
            if (!detailsResp.IsSuccessStatusCode)
            {
                Message = $"Error saving Details: {(int)detailsResp.StatusCode} {detailsResp.ReasonPhrase}";
                LoadFarmEditFromDb();
                LoadCurrentMapImageUrl();
                await LoadFarmCropsAsync();
                await LoadCategoryAndVarietyOptions();
                return Page();
            }

            Message = "Saved.";

            await TouchFarmProfileAsync(FarmId);

            LoadFarmEditFromDb();
            LoadCurrentMapImageUrl();
            await LoadFarmCropsAsync();
            await LoadCategoryAndVarietyOptions();
            return Page();
        }

        // ============================================================
        // POST: Upload Basemap Image
        // ============================================================
        public async Task<IActionResult> OnPostUploadMapImageAsync()
        {
            if (FarmId <= 0) FarmId = 1;

            var farm = _db.Farms.FirstOrDefault(f => f.Id == FarmId);
            if (farm == null)
            {
                UploadMessage = "Farm not found.";
                LoadFarmEditFromDb();
                LoadCurrentMapImageUrl();
                await LoadFarmCropsAsync();
                await LoadCategoryAndVarietyOptions();
                return Page();
            }

            if (MapImageFile == null || MapImageFile.Length == 0)
            {
                UploadMessage = "Choose an image file first.";
                LoadFarmEditFromDb();
                LoadCurrentMapImageUrl();
                await LoadFarmCropsAsync();
                await LoadCategoryAndVarietyOptions();
                return Page();
            }

            var ext = Path.GetExtension(MapImageFile.FileName).ToLowerInvariant();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
            {
                UploadMessage = "Invalid file type. Use JPG, PNG, or WebP.";
                LoadFarmEditFromDb();
                LoadCurrentMapImageUrl();
                await LoadFarmCropsAsync();
                await LoadCategoryAndVarietyOptions();
                return Page();
            }

            var folder = Path.Combine(_env.WebRootPath, "uploads", "farms", farm.Id.ToString());
            Directory.CreateDirectory(folder);

            foreach (var existing in Directory.GetFiles(folder, "basemap.*"))
            {
                System.IO.File.Delete(existing);
            }

            var fileName = "basemap" + ext;
            var physicalPath = Path.Combine(folder, fileName);

            using (var stream = System.IO.File.Create(physicalPath))
            {
                await MapImageFile.CopyToAsync(stream);
            }

            farm.MapImageUrl = $"/uploads/farms/{farm.Id}/{fileName}";
            farm.MapImageUploadedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await TouchFarmProfileAsync(farm.Id);

            return RedirectToPage("/Farmer/Dashboard", new { FarmId = farm.Id });
        }

        // ============================================================
        // Helpers
        // ============================================================
        private async Task TouchFarmProfileAsync(int farmId)
        {
            var farm = _db.Farms.FirstOrDefault(f => f.Id == farmId);
            if (farm == null) return;

            farm.ProfileLastUpdatedAt = DateTime.UtcNow;
            farm.ProfileUpdateCount = (farm.ProfileUpdateCount <= 0) ? 1 : (farm.ProfileUpdateCount + 1);

            await _db.SaveChangesAsync();
        }

        private async Task LoadFarmCropsAsync()
        {
            var client = CreateSiteClient();

            var resp = await client.GetAsync($"api/Crops/byFarm/{FarmId}");
            if (!resp.IsSuccessStatusCode)
            {
                Message ??= $"Error loading farm crops: API returned {(int)resp.StatusCode} {resp.ReasonPhrase}";
                FarmCrops = new List<CropSearchOut>();
                return;
            }

            var crops = await resp.Content.ReadFromJsonAsync<List<CropSearchOut>>();
            FarmCrops = crops ?? new List<CropSearchOut>();
        }

        private void LoadFarmEditFromDb()
        {
            var farm = _db.Farms.FirstOrDefault(f => f.Id == FarmId);
            if (farm == null)
            {
                FarmEdit = new FarmEditInput();
                CurrentLat = null;
                CurrentLng = null;
                return;
            }

            FarmEdit = new FarmEditInput
            {
                Name = farm.Name,
                Address = farm.Address,
                ContactMethod1 = farm.ContactMethod1
            };

            CurrentLat = farm.Latitude;
            CurrentLng = farm.Longitude;
        }

        private void LoadCurrentMapImageUrl()
        {
            var farm = _db.Farms.FirstOrDefault(f => f.Id == FarmId);
            CurrentMapImageUrl = farm?.MapImageUrl;
        }

        // ? SQLite-safe: fetch rows, then group in-memory
        private async Task LoadCategoryAndVarietyOptions()
        {
            var client = CreateSiteClient();

            CategoryOptions = new List<string>();
            VarietyOptionsByCategory = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // Categories from API (same as Search page)
            try
            {
                var catsResp = await client.GetAsync("api/Crops/categories");
                if (catsResp.IsSuccessStatusCode)
                {
                    var cats = await catsResp.Content.ReadFromJsonAsync<List<string>>();
                    CategoryOptions = (cats ?? new List<string>())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x)
                        .ToList();
                }
            }
            catch
            {
                // ignore if API fails
            }

            // Varieties from DB (group in memory to avoid EF translation issues)
            var rows = await _db.Crops
                .AsNoTracking()
                .Where(c => !string.IsNullOrWhiteSpace(c.Category) && !string.IsNullOrWhiteSpace(c.Variety))
                .Select(c => new
                {
                    Category = c.Category!,
                    Variety = c.Variety!
                })
                .ToListAsync();

            foreach (var grp in rows
                .Select(r => new { Category = r.Category.Trim(), Variety = r.Variety.Trim() })
                .Where(r => r.Category.Length > 0 && r.Variety.Length > 0)
                .GroupBy(r => r.Category, StringComparer.OrdinalIgnoreCase))
            {
                VarietyOptionsByCategory[grp.Key] = grp
                    .Select(x => x.Variety)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
            }

            // Fallback: if categories API returned nothing, use DB-derived categories
            if (CategoryOptions.Count == 0)
            {
                CategoryOptions = VarietyOptionsByCategory.Keys
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
            }

            VarietyOptionsJson = JsonSerializer.Serialize(VarietyOptionsByCategory);
        }

        private HttpClient CreateSiteClient()
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = $"{Request.Scheme}://{Request.Host}/";
            client.BaseAddress = new Uri(baseUrl);
            return client;
        }

        public class FarmEditInput
        {
            public string? Name { get; set; }
            public string? Address { get; set; }
            public string? ContactMethod1 { get; set; }
        }
    }
}