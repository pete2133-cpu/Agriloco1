using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class InventoryItemsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public InventoryItemsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public NewInventoryItemInput NewItem { get; set; } = new();

        public List<ItemVariationRow> ItemVariationRows { get; set; } = new();

        public async Task OnGetAsync()
        {
            var items = await _db.InventoryItems
                .Where(x => x.FarmId == FarmId)
                .OrderBy(x => x.ItemName)
                .ToListAsync();

            var packages = await _db.InventoryItemPackages
                .Where(x => x.FarmId == FarmId)
                .OrderBy(x => x.PackageName)
                .ToListAsync();

            var rows = new List<ItemVariationRow>();

            foreach (var item in items)
            {
                var itemPackages = packages
                    .Where(x => x.InventoryItemId == item.Id)
                    .ToList();

                if (!itemPackages.Any())
                {
                    rows.Add(new ItemVariationRow
                    {
                        ItemId = item.Id,
                        ItemName = item.ItemName,
                        ItemCategory = item.ItemCategory,
                        VariationName = "Add variation",
                        PackageType = "-",
                        EndAmount = "-",
                        EndUnit = item.BaseUnit,
                        Location = "-",
                        Barcode = "-",
                        Status = item.IsActive ? "Active" : "Inactive"
                    });
                }
                else
                {
                    foreach (var package in itemPackages)
                    {
                        rows.Add(new ItemVariationRow
                        {
                            ItemId = item.Id,
                            ItemName = item.ItemName,
                            ItemCategory = item.ItemCategory,
                            VariationName = package.PackageName,
                            PackageType = string.IsNullOrWhiteSpace(package.PackageType) ? "-" : package.PackageType,
                            EndAmount = package.PackageQuantity.ToString(),
                            EndUnit = package.PackageUnit,
                            Location = string.IsNullOrWhiteSpace(package.StorageLocation) ? "-" : package.StorageLocation,
                            Barcode = string.IsNullOrWhiteSpace(package.BarcodeValue) ? "-" : package.BarcodeValue,
                            Status = item.IsActive && package.IsActive ? "Active" : "Inactive"
                        });
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                rows = rows.Where(x =>
                    x.ItemName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.ItemCategory.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.VariationName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.PackageType.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.EndUnit.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.Location.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.Barcode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.Status.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ItemVariationRows = rows;
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var item = new InventoryItem
            {
                FarmId = FarmId,
                ItemName = NewItem.ItemName.Trim(),
                ItemCategory = NewItem.ItemCategory,
                BaseUnit = NewItem.BaseUnit,
                DefaultStorageLocation = "",
                ShelfLife = "",
                Notes = "",
                BarcodeValue = "",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.InventoryItems.Add(item);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int id, int farmId)
        {
            FarmId = farmId;

            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmId == FarmId);

            if (item != null)
            {
                item.IsActive = !item.IsActive;
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int farmId)
        {
            FarmId = farmId;

            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmId == FarmId);

            if (item != null)
            {
                var packages = await _db.InventoryItemPackages
                    .Where(x => x.InventoryItemId == id && x.FarmId == FarmId)
                    .ToListAsync();

                _db.InventoryItemPackages.RemoveRange(packages);
                _db.InventoryItems.Remove(item);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        public class NewInventoryItemInput
        {
            public string ItemName { get; set; } = "";
            public string ItemCategory { get; set; } = "Ingredient";
            public string BaseUnit { get; set; } = "units";
        }

        public class ItemVariationRow
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; } = "";
            public string ItemCategory { get; set; } = "";
            public string VariationName { get; set; } = "";
            public string PackageType { get; set; } = "";
            public string EndAmount { get; set; } = "";
            public string EndUnit { get; set; } = "";
            public string Location { get; set; } = "";
            public string Barcode { get; set; } = "";
            public string Status { get; set; } = "";
        }
    }
}