using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco.Api.Pages.Dashboard
{
    // Disable CSRF token requirement for this test page
    [IgnoreAntiforgeryToken]
    public class FarmerCropsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FarmerCropsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // All crops (for now – later we’ll filter by logged-in farmer)
        public List<CropSearchOut> Crops { get; set; } = new();

        // These two properties are bound from the form POST
        [BindProperty]
        public int CropIdToUpdate { get; set; }

        [BindProperty]
        public string? NewAvailability { get; set; }

        // GET: called when you first load /Dashboard/FarmerCrops
        public async Task OnGetAsync()
        {
            await LoadCropsAsync();
        }

        // POST: called when you click the "Update" button on the form
        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("OnPostAsync reached.");
            Console.WriteLine($"ModelState.IsValid = {ModelState.IsValid}");
            foreach (var kvp in ModelState)
            {
                Console.WriteLine($"ModelState[{kvp.Key}]: {string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            Console.WriteLine($"CropIdToUpdate={CropIdToUpdate}, NewAvailability='{NewAvailability}'");

            if (CropIdToUpdate > 0)
            {
                var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

                // Convert empty string ("") to null so the API treats it as "Unknown"
                string? normalizedAvailability =
                    string.IsNullOrWhiteSpace(NewAvailability) ? null : NewAvailability;

                Console.WriteLine($"Normalized availability: '{normalizedAvailability ?? "null"}'");

                var body = new { availability = normalizedAvailability };

                var response = await client.PutAsJsonAsync(
                    $"/api/Crops/{CropIdToUpdate}/availability", body);

                Console.WriteLine($"PUT /api/Crops/{CropIdToUpdate}/availability -> {(int)response.StatusCode}");
            }

            await LoadCropsAsync();
            return Page();
        }

        // Helper: calls your /api/Search/crops endpoint to fill the table
        private async Task LoadCropsAsync()
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");
            var response = await client.GetAsync("/api/Search/crops");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CropSearchOut[]>();
                if (data != null)
                {
                    Crops = data.ToList();
                }
            }
        }
    }
}