using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class HarvestLotDetailsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public HarvestLotDetailsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public HarvestLot HarvestLot { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var lot = await _db.HarvestLots.FindAsync(Id);

            if (lot == null)
            {
                return NotFound();
            }

            HarvestLot = lot;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existing = await _db.HarvestLots.FindAsync(HarvestLot.Id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.CropName = HarvestLot.CropName;
            existing.VarietyName = HarvestLot.VarietyName ?? "";
            existing.Quantity = HarvestLot.Quantity;
            existing.Unit = HarvestLot.Unit;
            existing.HarvestDate = HarvestLot.HarvestDate;
            existing.Status = HarvestLot.Status;
            existing.Notes = HarvestLot.Notes ?? "";
            existing.Location = HarvestLot.Location ?? "";
            existing.Workers = HarvestLot.Workers ?? "";
            existing.Condition = HarvestLot.Condition ?? "";

            await _db.SaveChangesAsync();

            return RedirectToPage("/Farmer/Inventory/HarvestLots");
        }
    }
}