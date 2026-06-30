using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class SuppliersModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public SuppliersModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public NewSupplierInput NewSupplier { get; set; } = new();

        public List<Supplier> Suppliers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var query = _db.Suppliers
                .Where(x => x.FarmId == FarmId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(x =>
                    x.SupplierName.Contains(SearchTerm) ||
                    x.SupplierType.Contains(SearchTerm) ||
                    x.ContactName.Contains(SearchTerm) ||
                    x.Email.Contains(SearchTerm) ||
                    x.Phone.Contains(SearchTerm));
            }

            Suppliers = await query
                .OrderBy(x => x.SupplierName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var supplier = new Supplier
            {
                FarmId = FarmId,
                SupplierName = NewSupplier.SupplierName.Trim(),
                SupplierType = NewSupplier.SupplierType,
                LinkedFarmId = NewSupplier.LinkedFarmId,
                ContactName = "",
                Phone = "",
                Email = "",
                Address = "",
                Notes = "",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int id, int farmId)
        {
            FarmId = farmId;

            var supplier = await _db.Suppliers
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmId == FarmId);

            if (supplier != null)
            {
                supplier.IsActive = !supplier.IsActive;
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int farmId)
        {
            FarmId = farmId;

            var supplier = await _db.Suppliers
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmId == FarmId);

            if (supplier != null)
            {
                _db.Suppliers.Remove(supplier);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        public class NewSupplierInput
        {
            public string SupplierName { get; set; } = "";
            public string SupplierType { get; set; } = "Farm";
            public int? LinkedFarmId { get; set; }
        }
    }
}