using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco.Api.Pages.Farmer
{
    public class AddCropAssetModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AddCropAssetModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<MemberPublicOut> Farms { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<string> Varieties { get; set; } = new();

        public object? CreatedPreview { get; set; }

        public string? Message { get; set; }

        [BindProperty]
        public CropAssetInput Input { get; set; } = new();

        public class CropAssetInput
        {
            [Required(ErrorMessage = "Please select a farm.")]
            public int? FarmId { get; set; }

            [Required(ErrorMessage = "Category is required.")]
            public string? Category { get; set; }

            public string? Variety { get; set; }
            public string? Availability { get; set; }

            public int? YearPlanted { get; set; }
            public string? Rootstock { get; set; }
            public string? Notes { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadListsAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? submitAction)
        {
            // Always load dropdown data
            await LoadListsAsync();

            // If category was chosen/changed, reload varieties for that category and stop
            // (This supports the onchange submit behavior)
            if (submitAction != "create")
            {
                if (!string.IsNullOrWhiteSpace(Input.Category))
                {
                    await LoadVarietiesAsync(Input.Category);
                }

                return Page();
            }

            // Creating a crop asset
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

                // IMPORTANT:
                // This assumes your Crops create DTO supports these fields.
                // If your DTO differs, paste it and I’ll match it exactly.
                var payload = new CropCreateIn
                {
                    FarmId = Input.FarmId!.Value,
                    Category = Input.Category!.Trim(),
                    Variety = string.IsNullOrWhiteSpace(Input.Variety) ? null : Input.Variety.Trim(),
                    Availability = string.IsNullOrWhiteSpace(Input.Availability) ? null : Input.Availability,
                    YearPlanted = Input.YearPlanted,
                    Rootstock = string.IsNullOrWhiteSpace(Input.Rootstock) ? null : Input.Rootstock.Trim(),
                    Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim()
                };

                var resp = await client.PostAsJsonAsync("/api/Crops", payload);

                if (resp.IsSuccessStatusCode)
                {
                    var createdText = await resp.Content.ReadAsStringAsync();
                    Message = "Crop asset created successfully.";
                    CreatedPreview = createdText;

                    // Keep farm + category selected, clear only optional text inputs
                    Input.Rootstock = null;
                    Input.Notes = null;

                    // Refresh varieties list for selected category
                    if (!string.IsNullOrWhiteSpace(Input.Category))
                        await LoadVarietiesAsync(Input.Category);

                    return Page();
                }
                else
                {
                    var error = await resp.Content.ReadAsStringAsync();
                    Message = "Error creating crop asset: " + error;

                    if (!string.IsNullOrWhiteSpace(Input.Category))
                        await LoadVarietiesAsync(Input.Category);

                    return Page();
                }
            }
            catch (Exception ex)
            {
                Message = "Error creating crop asset: " + ex.Message;

                if (!string.IsNullOrWhiteSpace(Input.Category))
                    await LoadVarietiesAsync(Input.Category);

                return Page();
            }
        }

        private async Task LoadListsAsync()
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

            // Farms
            var farms = await client.GetFromJsonAsync<MemberPublicOut[]>("/api/Farms/public");
            Farms = (farms ?? Array.Empty<MemberPublicOut>()).ToList();

            // Categories
            var cats = await client.GetFromJsonAsync<string[]>("/api/CropCatalog/categories");
            Categories = (cats ?? Array.Empty<string>()).ToList();

            // Varieties depends on selected category
            if (!string.IsNullOrWhiteSpace(Input.Category))
            {
                await LoadVarietiesAsync(Input.Category);
            }
            else
            {
                Varieties = new List<string>();
            }
        }

        private async Task LoadVarietiesAsync(string category)
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");
            var vars = await client.GetFromJsonAsync<string[]>(
                $"/api/CropCatalog/varieties?category={Uri.EscapeDataString(category)}"
            );
            Varieties = (vars ?? Array.Empty<string>()).ToList();
        }
    }
}