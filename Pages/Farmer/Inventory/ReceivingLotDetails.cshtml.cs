using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Agriloco1.Services;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class ReceivingLotDetailsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public ReceivingLotDetailsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty]
        public ReceivingLot ReceivingLot { get; set; } = new();

        [BindProperty]
        public NewCustomFieldInput NewCustomField { get; set; } = new();

        public HarvestTransfer? HarvestSource { get; set; }
        public List<ReceivingLotCustomField> CustomFields { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var lot = await _db.ReceivingLots
                .FirstOrDefaultAsync(x => x.Id == Id && x.FarmId == FarmId);

            if (lot == null)
            {
                return NotFound();
            }

            ReceivingLot = lot;
            await LoadCustomFieldsAsync(lot.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var existing = await _db.ReceivingLots
                .FirstOrDefaultAsync(x => x.Id == ReceivingLot.Id && x.FarmId == FarmId);

            if (existing == null)
            {
                return NotFound();
            }

            existing.ReceivedDate = ReceivingLot.ReceivedDate;
            existing.Status = ReceivingLot.Status ?? "Received";
            existing.Condition = ReceivingLot.Condition ?? "Good";
            existing.StorageLocation = ReceivingLot.StorageLocation ?? "";
            existing.BarcodeValue = ReceivingLot.BarcodeValue ?? "";
            existing.InvoiceNumber = ReceivingLot.InvoiceNumber ?? "";
            existing.PriceTotal = ReceivingLot.PriceTotal;
            existing.Notes = ReceivingLot.Notes ?? "";

            await _db.SaveChangesAsync();

            return RedirectToPage(new { id = existing.Id, farmId = FarmId });
        }

        public async Task<IActionResult> OnPostAddCustomFieldAsync()
        {
            var lotId = ReceivingLot.Id;

            var lot = await _db.ReceivingLots
                .FirstOrDefaultAsync(x => x.Id == lotId && x.FarmId == FarmId);

            if (lot == null)
            {
                return NotFound();
            }

            if (!string.Equals(NewCustomField.FieldName?.Trim(), HarvestTransfer.FieldName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(NewCustomField.FieldName) &&
                !string.IsNullOrWhiteSpace(NewCustomField.FieldValue))
            {
                var customField = new ReceivingLotCustomField
                {
                    FarmId = FarmId,
                    ReceivingLotId = lot.Id,
                    FieldName = NewCustomField.FieldName.Trim(),
                    FieldValue = NewCustomField.FieldValue.Trim(),
                    CreatedAt = DateTime.Now
                };

                _db.ReceivingLotCustomFields.Add(customField);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { id = lot.Id, farmId = FarmId });
        }

        public async Task<IActionResult> OnPostDeleteCustomFieldAsync(int fieldId, int lotId, int farmId)
        {
            FarmId = farmId;

            var field = await _db.ReceivingLotCustomFields
                .FirstOrDefaultAsync(x =>
                    x.Id == fieldId &&
                    x.ReceivingLotId == lotId &&
                    x.FarmId == FarmId);

            if (field != null && field.FieldName != HarvestTransfer.FieldName)
            {
                _db.ReceivingLotCustomFields.Remove(field);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { id = lotId, farmId = FarmId });
        }

        private async Task LoadCustomFieldsAsync(int lotId)
        {
            CustomFields = await _db.ReceivingLotCustomFields
                .Where(x => x.ReceivingLotId == lotId && x.FarmId == FarmId)
                .OrderBy(x => x.FieldName)
                .ThenBy(x => x.Id)
                .ToListAsync();
            var sourceField = CustomFields.FirstOrDefault(x => x.FieldName == HarvestTransfer.FieldName);
            if (HarvestTransfer.TryParse(sourceField?.FieldValue, out var source)) HarvestSource = source;
            CustomFields.RemoveAll(x => x.FieldName == HarvestTransfer.FieldName);
        }

        public class NewCustomFieldInput
        {
            public string FieldName { get; set; } = "";
            public string FieldValue { get; set; } = "";
        }
    }
}
