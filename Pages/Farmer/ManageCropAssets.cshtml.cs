using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Agriloco.Api.Dtos;

namespace agriloco.api.Pages.Farmer
{
    public class ManageCropAssetsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ManageCropAssetsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; }

        public List<CropSearchOut> Crops { get; set; } = new();

        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            if (FarmId <= 0)
                return;

            await LoadCropsAsync();
        }

        // Single Save button updates both Availability + OfferingType
        public async Task<IActionResult> OnPostSaveRowAsync(int farmId, int cropId, string? availability, string? offeringType)
        {
            FarmId = farmId;

            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

            // 1) Update Availability
            var availPayload = new CropAvailabilityUpdateIn
            {
                Availability = string.IsNullOrWhiteSpace(availability) ? null : availability.Trim()
            };

            var availResp = await client.PutAsJsonAsync($"api/Crops/{cropId}/availability", availPayload);
            if (!availResp.IsSuccessStatusCode)
            {
                Message = $"Error saving Availability: {(int)availResp.StatusCode} {availResp.ReasonPhrase}";
                await LoadCropsAsync();
                return Page();
            }

            // 2) Update OfferingType (via details endpoint)
            var detailsPayload = new CropDetailsUpdateIn
            {
                OfferingType = string.IsNullOrWhiteSpace(offeringType) ? null : offeringType.Trim()
            };

            var detailsResp = await client.PutAsJsonAsync($"api/Crops/{cropId}/details", detailsPayload);
            if (!detailsResp.IsSuccessStatusCode)
            {
                Message = $"Error saving OfferingType: {(int)detailsResp.StatusCode} {detailsResp.ReasonPhrase}";
                await LoadCropsAsync();
                return Page();
            }

            Message = "Saved.";
            await LoadCropsAsync();
            return Page();
        }

        private async Task LoadCropsAsync()
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

            var resp = await client.GetAsync($"api/Crops/byFarm/{FarmId}");
            if (!resp.IsSuccessStatusCode)
            {
                Message = $"Error loading farm crops: {(int)resp.StatusCode} {resp.ReasonPhrase}";
                Crops = new List<CropSearchOut>();
                return;
            }

            var crops = await resp.Content.ReadFromJsonAsync<List<CropSearchOut>>();
            Crops = crops ?? new List<CropSearchOut>();
        }
    }
}