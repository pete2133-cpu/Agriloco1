using System.Net.Http;
using System.Net.Http.Json;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace agriloco.api.Pages.Farmer
{
    public class CropsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CropsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; }

        [BindProperty]
        public CropCreateIn NewCrop { get; set; } = new();

        public List<CropSearchOut> ExistingCrops { get; set; } = new();

        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            // Keep the form in sync with the selected farm
            NewCrop.FarmId = FarmId;

            await LoadCropsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Keep the form in sync with the selected farm
            NewCrop.FarmId = FarmId;

            var client = CreateSiteClient();

            var response = await client.PostAsJsonAsync("api/Crops", NewCrop);
            if (!response.IsSuccessStatusCode)
            {
                Message = $"Error creating crop: {(int)response.StatusCode} {response.ReasonPhrase}";
                await LoadCropsAsync();
                return Page();
            }

            // Clear form after success (keep FarmId)
            NewCrop = new CropCreateIn { FarmId = FarmId };

            await LoadCropsAsync();
            return Page();
        }

        private async Task LoadCropsAsync()
        {
            var client = CreateSiteClient();

            var resp = await client.GetAsync("api/Crops/public");
            if (!resp.IsSuccessStatusCode)
            {
                Message = $"Error loading crops: API returned {(int)resp.StatusCode} {resp.ReasonPhrase}";
                ExistingCrops = new List<CropSearchOut>();
                return;
            }

            var crops = await resp.Content.ReadFromJsonAsync<CropSearchOut[]>();
            ExistingCrops = crops?.ToList() ?? new List<CropSearchOut>();
        }

        private HttpClient CreateSiteClient()
        {
            var client = _httpClientFactory.CreateClient();

            // BaseAddress is required for relative URLs like "api/Crops/public"
            // This resolves correctly to http://localhost:5227/ (or whatever host you are running)
            var baseUrl = $"{Request.Scheme}://{Request.Host}/";
            client.BaseAddress = new Uri(baseUrl);

            return client;
        }
    }
}