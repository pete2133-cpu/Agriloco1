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

        public async Task OnGetAsync()
        {
            NewHarvest.HarvestDate = DateTime.Today;

            var query = _db.HarvestLots
                .Where(x => x.FarmId == FarmId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(x =>
                    x.LotNumber.Contains(SearchTerm) ||
                    x.CropName.Contains(SearchTerm) ||
                    x.VarietyName.Contains(SearchTerm) ||
                    x.Status.Contains(SearchTerm));
            }

            HarvestLots = await query
                .OrderByDescending(x => x.HarvestDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var lot = new HarvestLot
            {
                FarmId = FarmId,
                CropName = NewHarvest.CropName.Trim(),
                VarietyName = string.IsNullOrWhiteSpace(NewHarvest.VarietyName) ? "" : NewHarvest.VarietyName.Trim(),
                Quantity = NewHarvest.Quantity,
                Unit = NewHarvest.Unit,
                HarvestDate = NewHarvest.HarvestDate,
                Status = "Private",
                Notes = "",
                Location = "",
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
            public string CropName { get; set; } = "";
            public string? VarietyName { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; } = "lbs";
            public DateTime HarvestDate { get; set; } = DateTime.Today;
        }
    }
}