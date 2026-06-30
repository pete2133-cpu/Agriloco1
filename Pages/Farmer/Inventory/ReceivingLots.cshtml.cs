using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class ReceivingLotsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public ReceivingLotsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public NewReceivingInput NewReceiving { get; set; } = new();

        public List<ReceivingLot> ReceivingLots { get; set; } = new();

        public List<SupplierOption> SupplierOptions { get; set; } = new();
        public List<ItemVariationOption> ItemVariationOptions { get; set; } = new();

        public async Task OnGetAsync()
        {
            NewReceiving.ReceivedDate = DateTime.Today;

            await LoadOptionsAsync();

            var query = _db.ReceivingLots
                .Where(x => x.FarmId == FarmId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(x =>
                    x.LotNumber.Contains(SearchTerm) ||
                    x.SupplierName.Contains(SearchTerm) ||
                    x.InventoryItemName.Contains(SearchTerm) ||
                    x.VariationName.Contains(SearchTerm) ||
                    x.Status.Contains(SearchTerm));
            }

            ReceivingLots = await query
                .OrderByDescending(x => x.ReceivedDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (NewReceiving.SupplierId == null ||
                NewReceiving.InventoryItemPackageId == null ||
                NewReceiving.UnitsReceived <= 0)
            {
                await OnGetAsync();
                return Page();
            }

            var supplier = await _db.Suppliers
                .FirstOrDefaultAsync(x => x.Id == NewReceiving.SupplierId && x.FarmId == FarmId);

            var package = await _db.InventoryItemPackages
                .FirstOrDefaultAsync(x => x.Id == NewReceiving.InventoryItemPackageId && x.FarmId == FarmId);

            if (supplier == null || package == null)
            {
                await OnGetAsync();
                return Page();
            }

            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.Id == package.InventoryItemId && x.FarmId == FarmId);

            if (item == null)
            {
                await OnGetAsync();
                return Page();
            }

            var totalUsable = NewReceiving.UnitsReceived * package.PackageQuantity;

            var lot = new ReceivingLot
            {
                FarmId = FarmId,
                LotNumber = "",
                ReceivedDate = NewReceiving.ReceivedDate,

                SupplierId = supplier.Id,
                SupplierName = supplier.SupplierName,

                InventoryItemId = item.Id,
                InventoryItemName = item.ItemName,

                InventoryItemPackageId = package.Id,
                VariationName = package.PackageName,
                PackageType = package.PackageType,

                UnitsReceived = NewReceiving.UnitsReceived,
                UsableQuantityPerUnit = package.PackageQuantity,
                UsableUnit = package.PackageUnit,
                TotalUsableQuantity = totalUsable,

                ItemName = item.ItemName,
                Quantity = totalUsable,
                Unit = package.PackageUnit,

                Status = "Received",
                Condition = "Good",

                StorageLocation = package.StorageLocation,
                BarcodeValue = package.BarcodeValue,

                InvoiceNumber = "",
                PriceTotal = null,

                PhotoCount = 0,
                DocumentCount = 0,

                SourceHarvestLotId = null,

                Notes = "",
                CreatedAt = DateTime.Now
            };

            _db.ReceivingLots.Add(lot);
            await _db.SaveChangesAsync();

            lot.LotNumber = $"REC-{lot.ReceivedDate:yyyyMMdd}-{lot.Id:000}";
            await _db.SaveChangesAsync();

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int farmId)
        {
            FarmId = farmId;

            var lot = await _db.ReceivingLots
                .FirstOrDefaultAsync(x => x.Id == id && x.FarmId == FarmId);

            if (lot != null)
            {
                _db.ReceivingLots.Remove(lot);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        private async Task LoadOptionsAsync()
        {
            SupplierOptions = await _db.Suppliers
                .Where(x => x.FarmId == FarmId && x.IsActive)
                .OrderBy(x => x.SupplierName)
                .Select(x => new SupplierOption
                {
                    Id = x.Id,
                    Name = x.SupplierName
                })
                .ToListAsync();

            var items = await _db.InventoryItems
                .Where(x => x.FarmId == FarmId && x.IsActive)
                .ToListAsync();

            var packages = await _db.InventoryItemPackages
                .Where(x => x.FarmId == FarmId && x.IsActive)
                .OrderBy(x => x.PackageName)
                .ToListAsync();

            ItemVariationOptions = packages
                .Join(
                    items,
                    package => package.InventoryItemId,
                    item => item.Id,
                    (package, item) => new ItemVariationOption
                    {
                        PackageId = package.Id,
                        DisplayName = $"{item.ItemName} - {package.PackageName} ({package.PackageQuantity} {package.PackageUnit} each)"
                    })
                .OrderBy(x => x.DisplayName)
                .ToList();
        }

        public class NewReceivingInput
        {
            public DateTime ReceivedDate { get; set; } = DateTime.Today;
            public int? SupplierId { get; set; }
            public int? InventoryItemPackageId { get; set; }
            public decimal UnitsReceived { get; set; }
        }

        public class SupplierOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class ItemVariationOption
        {
            public int PackageId { get; set; }
            public string DisplayName { get; set; } = "";
        }
    }
}