using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class HarvestLotsModel : PageModel
    {
        private static readonly List<HarvestLotRow> _harvestLots = new();
        private static int _nextId = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public NewHarvestInput NewHarvest { get; set; } = new();

        public List<HarvestLotRow> HarvestLots { get; set; } = new();

        public void OnGet()
        {
            NewHarvest.HarvestDate = DateTime.Today;

            HarvestLots = _harvestLots
                .Where(x =>
                    string.IsNullOrWhiteSpace(SearchTerm)
                    || x.LotNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                    || x.CropName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(x.VarietyName) && x.VarietyName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    || x.Status.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.HarvestDate)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public IActionResult OnPostCreate()
        {
            if (!ModelState.IsValid)
            {
                OnGet();
                return Page();
            }

            var id = _nextId++;

            var lot = new HarvestLotRow
            {
                Id = id,
                LotNumber = GenerateLotNumber(id, NewHarvest.HarvestDate),
                CropName = NewHarvest.CropName.Trim(),
                VarietyName = string.IsNullOrWhiteSpace(NewHarvest.VarietyName)
                    ? ""
                    : NewHarvest.VarietyName.Trim(),
                Quantity = NewHarvest.Quantity,
                Unit = NewHarvest.Unit,
                HarvestDate = NewHarvest.HarvestDate,
                Status = "Private",
                PhotoCount = 0,
                LocationLabel = "Add"
            };

            _harvestLots.Add(lot);

            return RedirectToPage();
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            var lot = _harvestLots.FirstOrDefault(x => x.Id == id);

            if (lot != null)
            {
                lot.Status = lot.Status == "Private" ? "Available" : "Private";
            }

            return RedirectToPage();
        }

        private static string GenerateLotNumber(int id, DateTime harvestDate)
        {
            return $"HAR-{harvestDate:yyyyMMdd}-{id:000}";
        }

        public class NewHarvestInput
        {
            public string CropName { get; set; } = "";
            public string? VarietyName { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; } = "lbs";
            public DateTime HarvestDate { get; set; } = DateTime.Today;
        }

        public class HarvestLotRow
        {
            public int Id { get; set; }
            public string LotNumber { get; set; } = "";
            public string CropName { get; set; } = "";
            public string VarietyName { get; set; } = "";
            public decimal Quantity { get; set; }
            public string Unit { get; set; } = "";
            public DateTime HarvestDate { get; set; }
            public string Status { get; set; } = "Private";
            public int PhotoCount { get; set; }
            public string LocationLabel { get; set; } = "Add";
        }
    }
}