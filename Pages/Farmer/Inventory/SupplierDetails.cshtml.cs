using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class SupplierDetailsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public SupplierDetailsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty]
        public Supplier Supplier { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var supplier = await _db.Suppliers.FindAsync(Id);

            if (supplier == null || supplier.FarmId != FarmId)
            {
                return NotFound();
            }

            Supplier = supplier;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existing = await _db.Suppliers.FindAsync(Supplier.Id);

            if (existing == null || existing.FarmId != FarmId)
            {
                return NotFound();
            }

            existing.SupplierName = Supplier.SupplierName?.Trim() ?? "";
            existing.SupplierType = Supplier.SupplierType ?? "Other";
            existing.LinkedFarmId = Supplier.LinkedFarmId;
            existing.ContactName = Supplier.ContactName ?? "";
            existing.Phone = Supplier.Phone ?? "";
            existing.Email = Supplier.Email ?? "";
            existing.Address = Supplier.Address ?? "";
            existing.Notes = Supplier.Notes ?? "";
            existing.IsActive = Supplier.IsActive;

            await _db.SaveChangesAsync();

            return RedirectToPage("/Farmer/Inventory/Suppliers", new { farmId = FarmId });
        }
    }
}