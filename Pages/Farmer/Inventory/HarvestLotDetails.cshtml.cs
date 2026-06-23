using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class HarvestLotDetailsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string LotNumber { get; set; } = "";

        [BindProperty]
        public string CropName { get; set; } = "";

        [BindProperty]
        public string VarietyName { get; set; } = "";

        [BindProperty]
        public decimal Quantity { get; set; }

        [BindProperty]
        public string Unit { get; set; } = "lbs";

        [BindProperty]
        public DateTime HarvestDate { get; set; }

        [BindProperty]
        public string Status { get; set; } = "Private";

        [BindProperty]
        public string Notes { get; set; } = "";

        [BindProperty]
        public string Location { get; set; } = "";

        [BindProperty]
        public string Workers { get; set; } = "";

        [BindProperty]
        public string Condition { get; set; } = "Good";

        public void OnGet()
        {
            LotNumber = $"HAR-DEMO-{Id:000}";
            CropName = "Strawberry";
            VarietyName = "Albion";
            Quantity = 40;
            Unit = "flats";
            HarvestDate = DateTime.Today;
            Status = "Private";
        }

        public IActionResult OnPost()
        {
            return Page();
        }
    }
}