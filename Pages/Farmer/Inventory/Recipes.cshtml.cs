using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class RecipesModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public RecipesModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public NewRecipeInput NewRecipe { get; set; } = new();

        public List<Recipe> Recipes { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadRecipesAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var requestedName = NewRecipe.RecipeName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(requestedName))
            {
                ErrorMessage = "A recipe name is required.";
                await LoadRecipesAsync();
                return Page();
            }

            var normalizedRequestedName = NormalizeName(requestedName);

            var existingRecipes = await _db.Recipes
                .Where(x => x.FarmId == FarmId)
                .ToListAsync();

            var duplicate = existingRecipes.FirstOrDefault(x =>
                NormalizeName(x.RecipeName) == normalizedRequestedName);

            if (duplicate != null)
            {
                ErrorMessage =
                    $"'{requestedName}' appears to duplicate the existing recipe '{duplicate.RecipeName}'.";

                await LoadRecipesAsync();
                return Page();
            }

            var recipe = new Recipe
            {
                FarmId = FarmId,
                RecipeName = requestedName,
                RecipeCategory = string.IsNullOrWhiteSpace(NewRecipe.RecipeCategory)
                    ? "Other"
                    : NewRecipe.RecipeCategory,

                ExpectedYieldQuantity = null,
                ExpectedYieldUnit = "",

                Status = "Draft",
                Notes = "",
                IsActive = true,

                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.Recipes.Add(recipe);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int id, int farmId)
        {
            FarmId = farmId;

            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.FarmId == FarmId);

            if (recipe != null)
            {
                recipe.IsActive = !recipe.IsActive;
                recipe.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int farmId)
        {
            FarmId = farmId;

            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.FarmId == FarmId);

            if (recipe != null)
            {
                _db.Recipes.Remove(recipe);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { farmId = FarmId });
        }

        private async Task LoadRecipesAsync()
        {
            var query = _db.Recipes
                .Where(x => x.FarmId == FarmId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(x =>
                    x.RecipeName.Contains(SearchTerm) ||
                    x.RecipeCategory.Contains(SearchTerm) ||
                    x.Status.Contains(SearchTerm) ||
                    x.Notes.Contains(SearchTerm));
            }

            Recipes = await query
                .OrderBy(x => x.RecipeName)
                .ToListAsync();
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return new string(
                value
                    .Trim()
                    .ToLowerInvariant()
                    .Where(char.IsLetterOrDigit)
                    .ToArray()
            );
        }

        public class NewRecipeInput
        {
            public string RecipeName { get; set; } = "";

            public string RecipeCategory { get; set; } = "Other";
        }
    }
}