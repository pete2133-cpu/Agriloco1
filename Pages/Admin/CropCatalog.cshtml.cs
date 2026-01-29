using System.Net.Http.Json;
using Agriloco.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Agriloco.Api.Pages.Admin
{
    public class CropCatalogModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CropCatalogModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<string> Categories { get; set; } = new();
        public List<string> Varieties { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedCategory { get; set; }

        [BindProperty]
        public string? CategoryName { get; set; }

        [BindProperty]
        public string? VarietyName { get; set; }

        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();

            if (!string.IsNullOrWhiteSpace(SelectedCategory))
            {
                await LoadVarietiesAsync(SelectedCategory);
            }
        }

        public async Task<IActionResult> OnPostAsync(string action, string? CategoryName, string? SelectedCategory, string? VarietyName)
        {
            // Keep these values in the model for redisplay
            this.CategoryName = CategoryName;
            this.SelectedCategory = SelectedCategory;
            this.VarietyName = VarietyName;

            if (string.IsNullOrWhiteSpace(action))
            {
                Message = "No action specified.";
                await OnGetAsync();
                return Page();
            }

            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");

            try
            {
                if (action == "addCategory")
                {
                    if (string.IsNullOrWhiteSpace(CategoryName))
                    {
                        Message = "Category Name is required.";
                        await OnGetAsync();
                        return Page();
                    }

                    var resp = await client.PostAsJsonAsync("/api/CropCatalog/category",
                        new CropCatalogCategoryCreateIn { Category = CategoryName });

                    if (resp.IsSuccessStatusCode)
                    {
                        Message = $"Category created: {CategoryName}";
                        // clear input
                        this.CategoryName = "";
                    }
                    else
                    {
                        Message = "Error creating category: " + await resp.Content.ReadAsStringAsync();
                    }
                }
                else if (action == "addVariety")
                {
                    if (string.IsNullOrWhiteSpace(SelectedCategory))
                    {
                        Message = "Select a category first.";
                        await OnGetAsync();
                        return Page();
                    }

                    if (string.IsNullOrWhiteSpace(VarietyName))
                    {
                        Message = "Variety Name is required.";
                        await OnGetAsync();
                        return Page();
                    }

                    var resp = await client.PostAsJsonAsync("/api/CropCatalog/variety",
                        new CropCatalogVarietyCreateIn { Category = SelectedCategory, Variety = VarietyName });

                    if (resp.IsSuccessStatusCode)
                    {
                        Message = $"Variety created: {SelectedCategory} / {VarietyName}";
                        // clear input
                        this.VarietyName = "";
                    }
                    else
                    {
                        Message = "Error creating variety: " + await resp.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    Message = $"Unknown action: {action}";
                }
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
            }

            // Reload lists after changes
            await LoadCategoriesAsync();

            if (!string.IsNullOrWhiteSpace(this.SelectedCategory))
            {
                await LoadVarietiesAsync(this.SelectedCategory);
            }

            return Page();
        }

        private async Task LoadCategoriesAsync()
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");
            var cats = await client.GetFromJsonAsync<string[]>("/api/CropCatalog/categories");
            Categories = (cats ?? Array.Empty<string>()).ToList();
        }

        private async Task LoadVarietiesAsync(string category)
        {
            var client = _httpClientFactory.CreateClient("AgrilocoApiClient");
            var vars = await client.GetFromJsonAsync<string[]>($"/api/CropCatalog/varieties?category={Uri.EscapeDataString(category)}");
            Varieties = (vars ?? Array.Empty<string>()).ToList();
        }
    }
}