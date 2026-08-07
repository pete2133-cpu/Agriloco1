using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class ProductionRunsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public ProductionRunsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public NewProductionRunInput NewRun { get; set; } = new();

        public List<ProductionRun> ProductionRuns { get; set; } = new();

        public List<RecipeOption> RecipeOptions { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            NewRun.ProductionDate = DateTime.Today;

            await LoadPageDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!NewRun.RecipeId.HasValue)
            {
                ErrorMessage = "Select a recipe.";

                await LoadPageDataAsync();

                return Page();
            }

            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(x =>
                    x.Id == NewRun.RecipeId.Value &&
                    x.FarmId == FarmId &&
                    x.IsActive);

            if (recipe == null)
            {
                ErrorMessage = "The selected recipe could not be found.";

                await LoadPageDataAsync();

                return Page();
            }

            var recipeIngredients = await _db.RecipeIngredients
                .Where(x =>
                    x.RecipeId == recipe.Id &&
                    x.FarmId == FarmId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            if (!recipeIngredients.Any())
            {
                ErrorMessage =
                    "This recipe has no ingredients. Add ingredients before creating a production run.";

                await LoadPageDataAsync();

                return Page();
            }

            var run = new ProductionRun
            {
                FarmId = FarmId,

                RecipeId = recipe.Id,

                RecipeName = recipe.RecipeName,

                BatchNumber = "",

                ProcessType =
                    string.IsNullOrWhiteSpace(NewRun.ProcessType)
                        ? "Other Food Processing"
                        : NewRun.ProcessType,

                Status = "Draft",

                ProductionDate = NewRun.ProductionDate,

                StartedAt = null,

                EndedAt = null,

                ScaleFactor = null,

                ScaleAnchorIngredientId = null,

                ExpectedYieldQuantity =
                    recipe.ExpectedYieldQuantity,

                ExpectedYieldUnit =
                    recipe.ExpectedYieldUnit ?? "",

                ActualYieldQuantity = null,

                ActualYieldUnit =
                    recipe.ExpectedYieldUnit ?? "",

                OutputInventoryItemId = null,

                OutputInventoryItemPackageId = null,

                IngredientsCommitted = false,

                OutputPosted = false,

                InventoryReturned = false,

                Notes = "",

                CreatedAt = DateTime.Now,

                UpdatedAt = DateTime.Now
            };

            _db.ProductionRuns.Add(run);

            await _db.SaveChangesAsync();

            run.BatchNumber =
                $"BAT-{run.ProductionDate:yyyyMMdd}-{run.Id:000}";

            foreach (var recipeIngredient in recipeIngredients)
            {
                var runIngredient = new ProductionRunIngredient
                {
                    FarmId = FarmId,

                    ProductionRunId = run.Id,

                    RecipeIngredientId =
                        recipeIngredient.Id,

                    InventoryItemId =
                        recipeIngredient.InventoryItemId,

                    InventoryItemPackageId =
                        recipeIngredient.InventoryItemPackageId,

                    IngredientName =
                        recipeIngredient.IngredientName,

                    VariationName =
                        recipeIngredient.VariationName ?? "",

                    RecipeQuantity =
                        recipeIngredient.Quantity,

                    Unit =
                        recipeIngredient.Unit,

                    SuggestedQuantity =
                        recipeIngredient.Quantity,

                    ActualQuantity = null,

                    IsScaleAnchor = false,

                    CommittedQuantity = 0,

                    Notes =
                        recipeIngredient.Notes ?? "",

                    SortOrder =
                        recipeIngredient.SortOrder,

                    CreatedAt = DateTime.Now
                };

                _db.ProductionRunIngredients.Add(runIngredient);
            }

            await _db.SaveChangesAsync();

            return RedirectToPage(
                "/Farmer/Inventory/ProductionRunDetails",
                new
                {
                    id = run.Id,
                    farmId = FarmId
                });
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            int id,
            int farmId)
        {
            FarmId = farmId;

            var run = await _db.ProductionRuns
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.FarmId == FarmId);

            if (run == null)
            {
                return RedirectToPage(new
                {
                    farmId = FarmId
                });
            }

            if (run.IngredientsCommitted ||
                run.Status != "Draft")
            {
                return RedirectToPage(new
                {
                    farmId = FarmId
                });
            }

            var ingredients = await _db.ProductionRunIngredients
                .Where(x =>
                    x.ProductionRunId == run.Id &&
                    x.FarmId == FarmId)
                .ToListAsync();

            var workEntries = await _db.ProductionWorkEntries
                .Where(x =>
                    x.ProductionRunId == run.Id &&
                    x.FarmId == FarmId)
                .ToListAsync();

            var workers = await _db.ProductionWorkEntryWorkers
                .Where(x =>
                    x.ProductionRunId == run.Id &&
                    x.FarmId == FarmId)
                .ToListAsync();

            var observations = await _db.ProductionObservations
                .Where(x =>
                    x.ProductionRunId == run.Id &&
                    x.FarmId == FarmId)
                .ToListAsync();

            var observationValues =
                await _db.ProductionObservationValues
                    .Where(x =>
                        x.ProductionRunId == run.Id &&
                        x.FarmId == FarmId)
                    .ToListAsync();

            _db.ProductionObservationValues
                .RemoveRange(observationValues);

            _db.ProductionObservations
                .RemoveRange(observations);

            _db.ProductionWorkEntryWorkers
                .RemoveRange(workers);

            _db.ProductionWorkEntries
                .RemoveRange(workEntries);

            _db.ProductionRunIngredients
                .RemoveRange(ingredients);

            _db.ProductionRuns.Remove(run);

            await _db.SaveChangesAsync();

            return RedirectToPage(new
            {
                farmId = FarmId
            });
        }

        private async Task LoadPageDataAsync()
        {
            var runsQuery = _db.ProductionRuns
                .Where(x => x.FarmId == FarmId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                runsQuery = runsQuery.Where(x =>
                    x.BatchNumber.Contains(SearchTerm) ||
                    x.RecipeName.Contains(SearchTerm) ||
                    x.ProcessType.Contains(SearchTerm) ||
                    x.Status.Contains(SearchTerm));
            }

            ProductionRuns = await runsQuery
                .OrderByDescending(x => x.ProductionDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            RecipeOptions = await _db.Recipes
                .Where(x =>
                    x.FarmId == FarmId &&
                    x.IsActive)
                .OrderBy(x => x.RecipeName)
                .Select(x => new RecipeOption
                {
                    Id = x.Id,

                    Name =
                        $"{x.RecipeName} - {x.Status}"
                })
                .ToListAsync();
        }

        public class NewProductionRunInput
        {
            public int? RecipeId { get; set; }

            public DateTime ProductionDate { get; set; }
                = DateTime.Today;

            public string ProcessType { get; set; }
                = "Other Food Processing";
        }

        public class RecipeOption
        {
            public int Id { get; set; }

            public string Name { get; set; } = "";
        }
    }
}