using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Agriloco.Api.Dtos;

namespace agriloco.api.Pages.Search
{
    public class CropsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CropsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Variety { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? OfferingType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Availability { get; set; }

        // ✅ categories for the autocomplete datalist
        public List<string> CategorySuggestions { get; set; } = new();

        public List<CropSearchOut> Results { get; set; } = new();

        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

            // Load category suggestions for datalist
            var catsResp = await client.GetAsync("api/Crops/categories");
            if (catsResp.IsSuccessStatusCode)
            {
                var cats = await catsResp.Content.ReadFromJsonAsync<List<string>>();
                CategorySuggestions = cats ?? new List<string>();
            }
            else
            {
                CategorySuggestions = new List<string>();
            }

            // Build API URL with optional query parameters
            var qs = new List<string>();

            if (!string.IsNullOrWhiteSpace(OfferingType))
                qs.Add($"offeringType={Uri.EscapeDataString(OfferingType.Trim())}");

            if (!string.IsNullOrWhiteSpace(Availability))
                qs.Add($"availability={Uri.EscapeDataString(Availability.Trim())}");

            var url = "api/Crops/public";
            if (qs.Count > 0)
                url += "?" + string.Join("&", qs);

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                Message = $"Error loading crops: {(int)resp.StatusCode} {resp.ReasonPhrase}";
                Results = new List<CropSearchOut>();
                return;
            }

            var crops = await resp.Content.ReadFromJsonAsync<List<CropSearchOut>>();
            var list = crops ?? new List<CropSearchOut>();

            // Client-side contains filters
            if (!string.IsNullOrWhiteSpace(Category))
            {
                var cat = Category.Trim();
                list = list
                    .Where(c => (c.Category ?? "").Contains(cat, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Variety))
            {
                var v = Variety.Trim();
                list = list
                    .Where(c => (c.Variety ?? "").Contains(v, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Results = list;
        }
    }
}