using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class RecipeDetailsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public RecipeDetailsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty]
        public Recipe Recipe { get; set; } = new();

        [BindProperty]
        public NewIngredientInput NewIngredient { get; set; } = new();

        public List<RecipeIngredient> Ingredients { get; set; } = new();

        public List<InventoryItemOption> InventoryItemOptions { get; set; } = new();

        public List<ItemVariationOption> ItemVariationOptions { get; set; } = new();

        public decimal? TotalEstimatedRecipeCost { get; set; }

        public string? ErrorMessage { get; set; }

        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(x =>
                    x.Id == Id &&
                    x.FarmId == FarmId);

            if (recipe == null)
            {
                return NotFound();
            }

            Recipe = recipe;

            await LoadPageDataAsync(recipe.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostSaveRecipeAsync()
        {
            var existing = await _db.Recipes
                .FirstOrDefaultAsync(x =>
                    x.Id == Recipe.Id &&
                    x.FarmId == FarmId);

            if (existing == null)
            {
                return NotFound();
            }

            var requestedName = Recipe.RecipeName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(requestedName))
            {
                ErrorMessage = "Recipe name is required.";

                Recipe = existing;
                await LoadPageDataAsync(existing.Id);

                return Page();
            }

            var normalizedRequestedName = NormalizeName(requestedName);

            var otherRecipes = await _db.Recipes
                .Where(x =>
                    x.FarmId == FarmId &&
                    x.Id != existing.Id)
                .ToListAsync();

            var duplicate = otherRecipes.FirstOrDefault(x =>
                NormalizeName(x.RecipeName) == normalizedRequestedName);

            if (duplicate != null)
            {
                ErrorMessage =
                    $"'{requestedName}' appears to duplicate the existing recipe '{duplicate.RecipeName}'.";

                Recipe = existing;
                await LoadPageDataAsync(existing.Id);

                return Page();
            }

            existing.RecipeName = requestedName;

            existing.RecipeCategory =
                string.IsNullOrWhiteSpace(Recipe.RecipeCategory)
                    ? "Other"
                    : Recipe.RecipeCategory;

            existing.ExpectedYieldQuantity = Recipe.ExpectedYieldQuantity;

            existing.ExpectedYieldUnit =
                Recipe.ExpectedYieldUnit?.Trim() ?? "";

            existing.Status =
                string.IsNullOrWhiteSpace(Recipe.Status)
                    ? "Draft"
                    : Recipe.Status;

            existing.Notes = Recipe.Notes ?? "";

            existing.IsActive = Recipe.IsActive;

            existing.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToPage(new
            {
                id = existing.Id,
                farmId = FarmId
            });
        }

        public async Task<IActionResult> OnPostAddIngredientAsync()
        {
            var recipeId = Recipe.Id;

            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(x =>
                    x.Id == recipeId &&
                    x.FarmId == FarmId);

            if (recipe == null)
            {
                return NotFound();
            }

            var inventoryItemId =
                ExtractLeadingId(NewIngredient.ItemSearch);

            var packageId =
                ExtractLeadingId(NewIngredient.VariationSearch);

            InventoryItem? inventoryItem = null;
            InventoryItemPackage? package = null;

            if (inventoryItemId.HasValue)
            {
                inventoryItem = await _db.InventoryItems
                    .FirstOrDefaultAsync(x =>
                        x.Id == inventoryItemId.Value &&
                        x.FarmId == FarmId);

                if (inventoryItem == null)
                {
                    ErrorMessage =
                        "The selected inventory item could not be found.";

                    Recipe = recipe;
                    await LoadPageDataAsync(recipe.Id);

                    return Page();
                }
            }

            if (packageId.HasValue)
            {
                package = await _db.InventoryItemPackages
                    .FirstOrDefaultAsync(x =>
                        x.Id == packageId.Value &&
                        x.FarmId == FarmId);

                if (package == null)
                {
                    ErrorMessage =
                        "The selected item variation could not be found.";

                    Recipe = recipe;
                    await LoadPageDataAsync(recipe.Id);

                    return Page();
                }

                if (inventoryItem == null ||
                    package.InventoryItemId != inventoryItem.Id)
                {
                    ErrorMessage =
                        "The selected variation does not belong to the selected item.";

                    Recipe = recipe;
                    await LoadPageDataAsync(recipe.Id);

                    return Page();
                }
            }

            var ingredientName =
                NewIngredient.IngredientName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(ingredientName))
            {
                ingredientName = inventoryItem?.ItemName ?? "";
            }

            if (string.IsNullOrWhiteSpace(ingredientName))
            {
                ErrorMessage =
                    "Enter an ingredient name or select an inventory item.";

                Recipe = recipe;
                await LoadPageDataAsync(recipe.Id);

                return Page();
            }

            if (NewIngredient.Quantity <= 0)
            {
                ErrorMessage =
                    "Ingredient quantity must be greater than zero.";

                Recipe = recipe;
                await LoadPageDataAsync(recipe.Id);

                return Page();
            }

            var unit = NewIngredient.Unit?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(unit))
            {
                unit =
                    package?.PackageUnit ??
                    inventoryItem?.BaseUnit ??
                    "units";
            }

            var nextSortOrder =
                await _db.RecipeIngredients
                    .Where(x =>
                        x.RecipeId == recipe.Id &&
                        x.FarmId == FarmId)
                    .Select(x => (int?)x.SortOrder)
                    .MaxAsync() ?? 0;

            decimal? estimatedTotalCost = null;

            if (NewIngredient.EstimatedUnitCost.HasValue)
            {
                estimatedTotalCost =
                    NewIngredient.Quantity *
                    NewIngredient.EstimatedUnitCost.Value;
            }

            var ingredient = new RecipeIngredient
            {
                FarmId = FarmId,

                RecipeId = recipe.Id,

                InventoryItemId = inventoryItem?.Id,

                InventoryItemPackageId = package?.Id,

                IngredientName = ingredientName,

                VariationName = package?.PackageName ?? "",

                Quantity = NewIngredient.Quantity,

                Unit = unit,

                EstimatedUnitCost =
                    NewIngredient.EstimatedUnitCost,

                EstimatedTotalCost =
                    estimatedTotalCost,

                Notes = NewIngredient.Notes?.Trim() ?? "",

                SortOrder = nextSortOrder + 1,

                CreatedAt = DateTime.Now
            };

            _db.RecipeIngredients.Add(ingredient);

            recipe.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToPage(new
            {
                id = recipe.Id,
                farmId = FarmId
            });
        }

        public async Task<IActionResult> OnPostDeleteIngredientAsync(
            int ingredientId,
            int recipeId,
            int farmId)
        {
            FarmId = farmId;

            var ingredient = await _db.RecipeIngredients
                .FirstOrDefaultAsync(x =>
                    x.Id == ingredientId &&
                    x.RecipeId == recipeId &&
                    x.FarmId == FarmId);

            if (ingredient != null)
            {
                _db.RecipeIngredients.Remove(ingredient);

                await _db.SaveChangesAsync();

                await RenumberIngredientsAsync(recipeId);
            }

            return RedirectToPage(new
            {
                id = recipeId,
                farmId = FarmId
            });
        }

        public async Task<IActionResult> OnPostMoveIngredientUpAsync(
            int ingredientId,
            int recipeId,
            int farmId)
        {
            FarmId = farmId;

            var ingredients = await _db.RecipeIngredients
                .Where(x =>
                    x.RecipeId == recipeId &&
                    x.FarmId == FarmId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var currentIndex =
                ingredients.FindIndex(x => x.Id == ingredientId);

            if (currentIndex > 0)
            {
                var current = ingredients[currentIndex];
                var previous = ingredients[currentIndex - 1];

                var currentOrder = current.SortOrder;

                current.SortOrder = previous.SortOrder;
                previous.SortOrder = currentOrder;

                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new
            {
                id = recipeId,
                farmId = FarmId
            });
        }

        public async Task<IActionResult> OnPostMoveIngredientDownAsync(
            int ingredientId,
            int recipeId,
            int farmId)
        {
            FarmId = farmId;

            var ingredients = await _db.RecipeIngredients
                .Where(x =>
                    x.RecipeId == recipeId &&
                    x.FarmId == FarmId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var currentIndex =
                ingredients.FindIndex(x => x.Id == ingredientId);

            if (currentIndex >= 0 &&
                currentIndex < ingredients.Count - 1)
            {
                var current = ingredients[currentIndex];
                var next = ingredients[currentIndex + 1];

                var currentOrder = current.SortOrder;

                current.SortOrder = next.SortOrder;
                next.SortOrder = currentOrder;

                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new
            {
                id = recipeId,
                farmId = FarmId
            });
        }

        private async Task LoadPageDataAsync(int recipeId)
        {
            Ingredients = await _db.RecipeIngredients
                .Where(x =>
                    x.RecipeId == recipeId &&
                    x.FarmId == FarmId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var estimatedCosts = Ingredients
                .Where(x => x.EstimatedTotalCost.HasValue)
                .Select(x => x.EstimatedTotalCost!.Value)
                .ToList();

            TotalEstimatedRecipeCost =
                estimatedCosts.Any()
                    ? estimatedCosts.Sum()
                    : null;

            var items = await _db.InventoryItems
                .Where(x =>
                    x.FarmId == FarmId &&
                    x.IsActive)
                .OrderBy(x => x.ItemName)
                .ToListAsync();

            InventoryItemOptions = items
                .Select(x => new InventoryItemOption
                {
                    ItemId = x.Id,
                    ItemName = x.ItemName,
                    BaseUnit = x.BaseUnit,
                    DisplayName =
                        $"{x.ItemName} ({x.BaseUnit})"
                })
                .ToList();

            var packages = await _db.InventoryItemPackages
                .Where(x =>
                    x.FarmId == FarmId &&
                    x.IsActive)
                .OrderBy(x => x.PackageName)
                .ToListAsync();

            ItemVariationOptions = packages
                .Join(
                    items,
                    package => package.InventoryItemId,
                    item => item.Id,
                    (package, item) => new ItemVariationOption
                    {
                        ItemId = item.Id,

                        PackageId = package.Id,

                        VariationName = package.PackageName,

                        Unit = package.PackageUnit,

                        DisplayName =
                            $"{item.ItemName} - " +
                            $"{package.PackageName} " +
                            $"({package.PackageQuantity} " +
                            $"{package.PackageUnit} each)"
                    })
                .OrderBy(x => x.DisplayName)
                .ToList();
        }

        private async Task RenumberIngredientsAsync(int recipeId)
        {
            var ingredients = await _db.RecipeIngredients
                .Where(x =>
                    x.RecipeId == recipeId &&
                    x.FarmId == FarmId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            for (var index = 0;
                 index < ingredients.Count;
                 index++)
            {
                ingredients[index].SortOrder = index + 1;
            }

            await _db.SaveChangesAsync();
        }

        private static int? ExtractLeadingId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var firstPart =
                value.Split('|')[0].Trim();

            if (int.TryParse(firstPart, out var id))
            {
                return id;
            }

            return null;
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

        public class NewIngredientInput
        {
            public string ItemSearch { get; set; } = "";

            public string VariationSearch { get; set; } = "";

            public string IngredientName { get; set; } = "";

            public decimal Quantity { get; set; }

            public string Unit { get; set; } = "";

            public decimal? EstimatedUnitCost { get; set; }

            public string Notes { get; set; } = "";
        }

        public class InventoryItemOption
        {
            public int ItemId { get; set; }

            public string ItemName { get; set; } = "";

            public string BaseUnit { get; set; } = "";

            public string DisplayName { get; set; } = "";
        }

        public class ItemVariationOption
        {
            public int ItemId { get; set; }

            public int PackageId { get; set; }

            public string VariationName { get; set; } = "";

            public string Unit { get; set; } = "";

            public string DisplayName { get; set; } = "";
        }
    }
}