using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class InventoryItemDetailsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public InventoryItemDetailsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty]
        public InventoryItem InventoryItem { get; set; } = new();

        [BindProperty]
        public NewVariationInput NewPackage { get; set; } = new();

        public List<InventoryItemPackage> Packages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var item = await _db.InventoryItems.FindAsync(Id);

            if (item == null || item.FarmId != FarmId)
            {
                return NotFound();
            }

            InventoryItem = item;

            Packages = await _db.InventoryItemPackages
                .Where(x => x.InventoryItemId == Id && x.FarmId == FarmId)
                .OrderBy(x => x.PackageName)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSaveItemAsync()
        {
            var existing = await _db.InventoryItems.FindAsync(InventoryItem.Id);

            if (existing == null || existing.FarmId != FarmId)
            {
                return NotFound();
            }

            existing.ItemName = InventoryItem.ItemName?.Trim() ?? "";
            existing.ItemCategory = InventoryItem.ItemCategory ?? "Other";
            existing.BaseUnit = InventoryItem.BaseUnit ?? "units";
            existing.Notes = InventoryItem.Notes ?? "";
            existing.IsActive = InventoryItem.IsActive;

            await _db.SaveChangesAsync();

            return RedirectToPage(new { id = existing.Id, farmId = FarmId });
        }

        public async Task<IActionResult> OnPostAddPackageAsync()
        {
            var itemId = InventoryItem.Id;

            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.FarmId == FarmId);

            if (item == null)
            {
                return NotFound();
            }

            var variation = new InventoryItemPackage
            {
                FarmId = FarmId,
                InventoryItemId = itemId,
                PackageName = NewPackage.PackageName.Trim(),
                PackageType = NewPackage.PackageType ?? "Other",
                PackageQuantity = NewPackage.PackageQuantity,
                PackageUnit = NewPackage.PackageUnit,
                StorageLocation = NewPackage.StorageLocation ?? "",
                BarcodeValue = NewPackage.BarcodeValue ?? "",
                ShelfLife = NewPackage.ShelfLife ?? "",
                Notes = NewPackage.Notes ?? "",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.InventoryItemPackages.Add(variation);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { id = itemId, farmId = FarmId });
        }

        public async Task<IActionResult> OnPostTogglePackageAsync(int packageId, int itemId, int farmId)
        {
            FarmId = farmId;

            var package = await _db.InventoryItemPackages
                .FirstOrDefaultAsync(x => x.Id == packageId && x.InventoryItemId == itemId && x.FarmId == FarmId);

            if (package != null)
            {
                package.IsActive = !package.IsActive;
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { id = itemId, farmId = FarmId });
        }

        public async Task<IActionResult> OnPostDeletePackageAsync(int packageId, int itemId, int farmId)
        {
            FarmId = farmId;

            var package = await _db.InventoryItemPackages
                .FirstOrDefaultAsync(x => x.Id == packageId && x.InventoryItemId == itemId && x.FarmId == FarmId);

            if (package != null)
            {
                _db.InventoryItemPackages.Remove(package);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { id = itemId, farmId = FarmId });
        }

        public class NewVariationInput
        {
            public string PackageName { get; set; } = "";
            public string? PackageType { get; set; } = "Other";
            public decimal PackageQuantity { get; set; }
            public string PackageUnit { get; set; } = "units";

            public string? StorageLocation { get; set; }
            public string? BarcodeValue { get; set; }
            public string? ShelfLife { get; set; }
            public string? Notes { get; set; }
        }
    }
}