using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net;

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
        public List<InventoryItemOption> InventoryItemOptions { get; set; } = new();
        public List<ItemVariationOption> ItemVariationOptions { get; set; } = new();

        public Dictionary<int, string> CustomDetailsByLotId { get; set; } = new();

        public string? ErrorMessage { get; set; }

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

            await LoadCustomDetailsAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var supplierId = ExtractLeadingId(NewReceiving.SupplierSearch);
            var itemId = ExtractLeadingId(NewReceiving.ItemSearch);
            var packageId = ExtractLeadingId(NewReceiving.ItemVariationSearch);

            if (supplierId == null ||
                itemId == null ||
                NewReceiving.UnitsReceived <= 0)
            {
                ErrorMessage = "Please select a valid supplier, item, and quantity.";
                await OnGetAsync();
                return Page();
            }

            var supplier = await _db.Suppliers
                .FirstOrDefaultAsync(x => x.Id == supplierId && x.FarmId == FarmId);

            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.FarmId == FarmId);

            if (supplier == null || item == null)
            {
                ErrorMessage = "The selected supplier or item could not be found.";
                await OnGetAsync();
                return Page();
            }

            InventoryItemPackage? package = null;

            if (packageId != null)
            {
                package = await _db.InventoryItemPackages
                    .FirstOrDefaultAsync(x =>
                        x.Id == packageId &&
                        x.FarmId == FarmId &&
                        x.InventoryItemId == item.Id);

                if (package == null)
                {
                    ErrorMessage = "The selected variation does not belong to the selected item.";
                    await OnGetAsync();
                    return Page();
                }
            }

            decimal usableQuantityPerUnit;
            string usableUnit;
            decimal totalUsable;
            string variationName;
            string packageType;
            string storageLocation;
            string barcodeValue;

            if (package != null)
            {
                usableQuantityPerUnit = package.PackageQuantity;
                usableUnit = package.PackageUnit;
                totalUsable = NewReceiving.UnitsReceived * package.PackageQuantity;
                variationName = package.PackageName;
                packageType = package.PackageType;
                storageLocation = package.StorageLocation;
                barcodeValue = package.BarcodeValue;
            }
            else
            {
                usableQuantityPerUnit = 1;
                usableUnit = item.BaseUnit;
                totalUsable = NewReceiving.UnitsReceived;
                variationName = "Base Item";
                packageType = "Simple";
                storageLocation = item.DefaultStorageLocation;
                barcodeValue = item.BarcodeValue;
            }

            var lot = new ReceivingLot
            {
                FarmId = FarmId,
                LotNumber = "",
                ReceivedDate = NewReceiving.ReceivedDate,

                SupplierId = supplier.Id,
                SupplierName = supplier.SupplierName,

                InventoryItemId = item.Id,
                InventoryItemName = item.ItemName,

                InventoryItemPackageId = package?.Id,
                VariationName = variationName,
                PackageType = packageType,

                UnitsReceived = NewReceiving.UnitsReceived,
                UsableQuantityPerUnit = usableQuantityPerUnit,
                UsableUnit = usableUnit,
                TotalUsableQuantity = totalUsable,

                ItemName = item.ItemName,
                Quantity = totalUsable,
                Unit = usableUnit,

                Status = "Received",
                Condition = "Good",

                StorageLocation = storageLocation,
                BarcodeValue = barcodeValue,

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
                var fields = await _db.ReceivingLotCustomFields
                    .Where(x => x.ReceivingLotId == lot.Id && x.FarmId == FarmId)
                    .ToListAsync();

                _db.ReceivingLotCustomFields.RemoveRange(fields);
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
                .OrderBy(x => x.ItemName)
                .ToListAsync();

            InventoryItemOptions = items
                .Select(x => new InventoryItemOption
                {
                    ItemId = x.Id,
                    DisplayName = $"{x.ItemName} ({x.BaseUnit})"
                })
                .ToList();

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
                        ItemId = item.Id,
                        PackageId = package.Id,
                        DisplayName = $"{item.ItemName} - {package.PackageName} ({package.PackageQuantity} {package.PackageUnit} each)"
                    })
                .OrderBy(x => x.DisplayName)
                .ToList();
        }

        private async Task LoadCustomDetailsAsync()
        {
            CustomDetailsByLotId = new Dictionary<int, string>();

            var lotIds = ReceivingLots.Select(x => x.Id).ToList();

            if (!lotIds.Any())
            {
                return;
            }

            var fields = await _db.ReceivingLotCustomFields
                .Where(x => x.FarmId == FarmId && lotIds.Contains(x.ReceivingLotId))
                .OrderBy(x => x.FieldName)
                .ThenBy(x => x.Id)
                .ToListAsync();

            CustomDetailsByLotId = fields
                .GroupBy(x => x.ReceivingLotId)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join("<br />", group.Select(field =>
                        $"<strong>{WebUtility.HtmlEncode(field.FieldName)}:</strong> {WebUtility.HtmlEncode(field.FieldValue)}"
                    ))
                );
        }

        private static int? ExtractLeadingId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var firstPart = value.Split('|')[0].Trim();

            if (int.TryParse(firstPart, out var id))
                return id;

            return null;
        }

        public class NewReceivingInput
        {
            public DateTime ReceivedDate { get; set; } = DateTime.Today;
            public string SupplierSearch { get; set; } = "";
            public string ItemSearch { get; set; } = "";
            public string ItemVariationSearch { get; set; } = "";
            public decimal UnitsReceived { get; set; }
        }

        public class SupplierOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class InventoryItemOption
        {
            public int ItemId { get; set; }
            public string DisplayName { get; set; } = "";
        }

        public class ItemVariationOption
        {
            public int ItemId { get; set; }
            public int PackageId { get; set; }
            public string DisplayName { get; set; } = "";
        }
    }
}