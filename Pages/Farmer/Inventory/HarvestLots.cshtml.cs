using Agriloco.Api.Services;
using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class HarvestLotsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public HarvestLotsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public NewHarvestInput NewHarvest { get; set; } = new();

        public List<HarvestLot> HarvestLots { get; set; } = new();

        public HarvestSourceCatalog Sources { get; private set; } = new();

        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        private async Task LoadPageAsync()
        {
            Sources = HarvestSourceCatalog.FromDefinitions(await _db.FarmDefinitions.AsNoTracking().Where(x => x.FarmId == FarmId).ToListAsync(), FarmId);

            var query = _db.HarvestLots
                .Where(x => x.FarmId == FarmId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(x =>
                    x.LotNumber.Contains(SearchTerm) ||
                    x.CropName.Contains(SearchTerm) ||
                    x.VarietyName.Contains(SearchTerm) ||
                    x.Status.Contains(SearchTerm) || x.Location.Contains(SearchTerm));
            }

            HarvestLots = await query
                .OrderByDescending(x => x.HarvestDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            await LoadPageAsync();
            var crop = Sources.Crops.FirstOrDefault(x => x.Id == NewHarvest.CropDefinitionId);
            var varieties = Sources.Varieties.Where(x => x.CropId == NewHarvest.CropDefinitionId).ToList();
            var variety = varieties.FirstOrDefault(x => x.Id == NewHarvest.VarietyDefinitionId);
            var locations = Sources.Locations.Where(x => x.CropId == NewHarvest.CropDefinitionId && x.VarietyId == NewHarvest.VarietyDefinitionId).ToList();
            var location = locations.FirstOrDefault(x => x.Id == NewHarvest.LocationDefinitionId);
            if (crop == null) ModelState.AddModelError("NewHarvest.CropDefinitionId", "Choose a saved crop from this farm.");
            if ((varieties.Count > 0 || NewHarvest.VarietyDefinitionId.HasValue) && variety == null)
                ModelState.AddModelError("NewHarvest.VarietyDefinitionId", "Choose a variety belonging to this crop.");
            if ((locations.Count > 0 || NewHarvest.LocationDefinitionId.HasValue) && location == null)
                ModelState.AddModelError("NewHarvest.LocationDefinitionId", "Choose the row or location harvested for this crop and variety.");
            if (NewHarvest.Quantity <= 0) ModelState.AddModelError("NewHarvest.Quantity", "Enter a quantity greater than zero.");
            if (!new[] { "lbs", "kg", "bushels", "bins", "flats", "pails", "units" }.Contains(NewHarvest.Unit))
                ModelState.AddModelError("NewHarvest.Unit", "Choose a unit from the list.");
            if (NewHarvest.HarvestDate == default) ModelState.AddModelError("NewHarvest.HarvestDate", "Choose a harvest date.");
            if (!ModelState.IsValid) return Page();
            var lot = new HarvestLot
            {
                FarmId = FarmId,
                CropName = crop!.Name,
                VarietyName = variety?.Name ?? "",
                Quantity = NewHarvest.Quantity,
                Unit = NewHarvest.Unit,
                HarvestDate = NewHarvest.HarvestDate,
                Status = "Private",
                Notes = "",
                Location = location?.Name ?? "",
                Workers = "",
                Condition = "Good",
                PhotoCount = 0,
                CreatedAt = DateTime.Now
            };

            _db.HarvestLots.Add(lot);
            await _db.SaveChangesAsync();

            lot.LotNumber = $"HAR-{lot.HarvestDate:yyyyMMdd}-{lot.Id:000}";
            await _db.SaveChangesAsync();

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var lot = await _db.HarvestLots
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmId == FarmId);

            if (lot != null)
            {
                lot.Status = lot.Status == "Private" ? "Available" : "Private";
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var lot = await _db.HarvestLots
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmId == FarmId);

            if (lot != null)
            {
                _db.HarvestLots.Remove(lot);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        public class NewHarvestInput
        {
            public int? CropDefinitionId { get; set; }
            public int? VarietyDefinitionId { get; set; }
            public int? LocationDefinitionId { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; } = "lbs";
            public DateTime HarvestDate { get; set; } = DateTime.Today;
        }
    }
}