using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco.Api.Pages.Farmer
{
    public class MapImageModel : PageModel
    {
        private readonly AgrilocoContext _db;
        private readonly IWebHostEnvironment _env;

        public MapImageModel(AgrilocoContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public List<Farm> AllFarms { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? FarmId { get; set; }

        public Farm? SelectedFarm { get; set; }
        public List<Crop> FarmCrops { get; set; } = new();

        [BindProperty]
        public IFormFile? MapImageFile { get; set; }

        public string? UploadMessage { get; set; }

        public void OnGet()
        {
            LoadFarms();

            if (FarmId.HasValue)
            {
                LoadFarmDetails(FarmId.Value);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LoadFarms();

            if (!FarmId.HasValue)
            {
                UploadMessage = "No farm selected.";
                return Page();
            }

            var farm = _db.Farms.FirstOrDefault(f => f.Id == FarmId.Value);
            if (farm == null)
            {
                UploadMessage = "Farm not found.";
                return Page();
            }

            if (MapImageFile == null || MapImageFile.Length == 0)
            {
                UploadMessage = "Choose an image file first.";
                LoadFarmDetails(FarmId.Value);
                return Page();
            }

            // Basic file validation
            var ext = Path.GetExtension(MapImageFile.FileName).ToLowerInvariant();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
            {
                UploadMessage = "Invalid file type. Use JPG, PNG, or WebP.";
                LoadFarmDetails(FarmId.Value);
                return Page();
            }

            // Save under wwwroot/uploads/farms/{farmId}/basemap.{ext}
            var folder = Path.Combine(_env.WebRootPath, "uploads", "farms", farm.Id.ToString());
            Directory.CreateDirectory(folder);

            // Delete any existing basemap.* to keep it clean
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

            // Redirect to GET so refresh shows preview and avoids form resubmission
            return RedirectToPage("/Farmer/MapImage", new { farmId = farm.Id });
        }

        private void LoadFarms()
        {
            AllFarms = _db.Farms
                .OrderBy(f => f.Name)
                .ToList();
        }

        private void LoadFarmDetails(int farmId)
        {
            SelectedFarm = _db.Farms.FirstOrDefault(f => f.Id == farmId);

            FarmCrops = _db.Crops
                .Where(c => c.FarmId == farmId)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Variety)
                .ToList();
        }
    }
}