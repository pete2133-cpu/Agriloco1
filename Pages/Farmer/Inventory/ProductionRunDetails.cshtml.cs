using Agriloco.Api.Data;
using Agriloco1.Services;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer.Inventory
{
    public class ProductionRunDetailsModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public ProductionRunDetailsModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty]
        public ProductionRun ProductionRun { get; set; } = new();

        [BindProperty]
        public NewWorkEntryInput NewWorkEntry { get; set; } = new();

        [BindProperty]
        public NewCheckInput NewCheck { get; set; } = new();

        public List<Recipe> RecipeOptions { get; set; } = new();
        public List<ReceivingLot> ReceivingOptions { get; set; } = new();
        public Dictionary<int, ProductionIngredientSource> IngredientSources { get; set; } = new();
        public Dictionary<int, ReceivingTransfer> SourceDetails { get; set; } = new();
        public Dictionary<int, HarvestTransfer> ReceiptHarvests { get; set; } = new();
        public bool CanLoadRecipe { get; set; }

        public List<ProductionRunIngredient> Ingredients { get; set; } = new();

        public List<ProductionWorkEntry> WorkEntries { get; set; } = new();

        public List<ProductionWorkEntryWorker> Workers { get; set; } = new();

        public List<ProductionObservation> Observations { get; set; } = new();

        public List<ProductionObservationValue> ObservationValues { get; set; } = new();

        public List<InventoryItemOption> InventoryItemOptions { get; set; } = new();

        public decimal TotalLabourHours { get; set; }

        public decimal? TotalLabourCost { get; set; }

        public string? ErrorMessage { get; set; }

        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var run = await FindRunAsync(Id);

            if (run == null)
            {
                return NotFound();
            }

            ProductionRun = run;

            NewWorkEntry.WorkDate = DateTime.Now;
            NewWorkEntry.DefaultHours = 1;

            NewCheck.RecordedAt = DateTime.Now;
            NewCheck.WorkerHours = 1;
            NewCheck.ObservationType = "Batch Check";

            await LoadPageDataAsync(run.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostLoadRecipeAsync(int runId, int farmId, int recipeId)
        {
            FarmId = farmId;
            var run = await FindRunAsync(runId);
            if (run == null) return NotFound();
            await LoadPageDataAsync(run.Id);
            if (!CanLoadRecipe)
            {
                await LoadFailureAsync(run, "Recipe loading is available only for an untouched draft. Create a new production run to use another recipe after quantities, sources or work have been recorded.");
                return Page();
            }
            var recipe = await _db.Recipes.FirstOrDefaultAsync(x => x.Id == recipeId && x.FarmId == FarmId && x.IsActive);
            var rows = await _db.RecipeIngredients.Where(x => x.RecipeId == recipeId && x.FarmId == FarmId).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
            if (recipe == null || rows.Count == 0)
            {
                await LoadFailureAsync(run, "Select a saved recipe with ingredients.");
                return Page();
            }
            await using var transaction = await _db.Database.BeginTransactionAsync();
            _db.ProductionRunIngredients.RemoveRange(Ingredients);
            foreach (var row in rows)
                _db.ProductionRunIngredients.Add(new ProductionRunIngredient {
                    FarmId = FarmId, ProductionRunId = run.Id, RecipeIngredientId = row.Id,
                    InventoryItemId = row.InventoryItemId, InventoryItemPackageId = row.InventoryItemPackageId,
                    IngredientName = row.IngredientName, VariationName = row.VariationName,
                    RecipeQuantity = row.Quantity, SuggestedQuantity = row.Quantity, Unit = row.Unit,
                    Notes = row.Notes, SortOrder = row.SortOrder
                });
            run.RecipeId = recipe.Id;
            run.RecipeName = recipe.RecipeName;
            run.ExpectedYieldQuantity = recipe.ExpectedYieldQuantity;
            run.ExpectedYieldUnit = recipe.ExpectedYieldUnit;
            run.ActualYieldUnit = recipe.ExpectedYieldUnit;
            run.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostSaveSourceAsync(int runId, int farmId, int ingredientId,
            string sourceMode, int? receivingLotId, string? receiptPayload, bool confirmReceipt = false)
        {
            FarmId = farmId;
            var run = await FindRunAsync(runId);
            if (run == null) return NotFound();
            if (run.Status == "Complete" || run.Status == "Cancelled")
            {
                await LoadFailureAsync(run, "Sources are locked for completed or cancelled production runs.");
                return Page();
            }
            var ingredient = await _db.ProductionRunIngredients.FirstOrDefaultAsync(x => x.Id == ingredientId && x.ProductionRunId == run.Id && x.FarmId == FarmId);
            if (ingredient == null) return NotFound();
            ReceivingTransfer? source = null;
            string? error = null;
            if (sourceMode == "local")
            {
                var lot = await _db.ReceivingLots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == receivingLotId && x.FarmId == FarmId);
                if (lot == null || !MatchesIngredient(ingredient, lot))
                    error = "Choose a saved receiving lot for this ingredient from this farm.";
                else source = await ReceivingTransfer.FromLotAsync(_db, FarmId, lot.Id);
                if (error == null && (source == null || !ReceivingTransfer.TryParse(source.Encode(), out _)))
                    error = "This receipt's source information is incomplete or too long. Check its receiving QR label.";
            }
            else if (sourceMode == "qr")
            {
                if (!ReceivingTransfer.TryParse(receiptPayload?.Trim(), out source))
                    error = "Scan a valid Agriloco receiving QR label, including its receipt information. A harvest-only label is not a receiving record.";
                else if (!confirmReceipt)
                    error = "Review the receipt and confirm that it is the source for this ingredient.";
            }
            else if (sourceMode != "none") error = "Choose whether to record an ingredient source.";
            if (error != null)
            {
                await LoadFailureAsync(run, error);
                return Page();
            }
            var existing = await _db.ProductionIngredientSources.FirstOrDefaultAsync(x => x.FarmId == FarmId && x.ProductionRunId == run.Id && x.ProductionRunIngredientId == ingredient.Id);
            if (sourceMode == "none")
            {
                if (existing != null) _db.ProductionIngredientSources.Remove(existing);
            }
            else
            {
                if (existing == null)
                {
                    existing = new ProductionIngredientSource { FarmId = FarmId, ProductionRunId = run.Id, ProductionRunIngredientId = ingredient.Id };
                    _db.ProductionIngredientSources.Add(existing);
                }
                existing.ReceivingLotId = sourceMode == "local" ? receivingLotId : null;
                existing.ReceiptPayload = source!.Encode();
                existing.RecordedAt = DateTime.Now;
            }
            run.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return RedirectToRun(run.Id);
        }

        public static bool MatchesIngredient(ProductionRunIngredient ingredient, ReceivingLot lot) =>
            ingredient.InventoryItemId.HasValue
                ? ingredient.InventoryItemId == lot.InventoryItemId
                : string.Equals(ingredient.IngredientName.Trim(), lot.InventoryItemName.Trim(), StringComparison.OrdinalIgnoreCase);

        public async Task<IActionResult> OnPostSaveHeaderAsync()
        {
            var run = await FindRunAsync(ProductionRun.Id);

            if (run == null)
            {
                return NotFound();
            }

            run.ProductionDate = ProductionRun.ProductionDate;
            run.ProcessType = ProductionRun.ProcessType ?? "Other Food Processing";
            run.StartedAt = ProductionRun.StartedAt;
            run.EndedAt = ProductionRun.EndedAt;
            run.ActualYieldQuantity = ProductionRun.ActualYieldQuantity;
            run.ActualYieldUnit = ProductionRun.ActualYieldUnit ?? "";
            run.OutputInventoryItemId = ProductionRun.OutputInventoryItemId;
            run.Notes = ProductionRun.Notes ?? "";
            run.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostSaveIngredientAsync(
            int ingredientId,
            int runId,
            int farmId,
            decimal? actualQuantity)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            if (run.Status == "Complete" ||
                run.Status == "Cancelled")
            {
                return RedirectToRun(run.Id);
            }

            var ingredient = await _db.ProductionRunIngredients
                .FirstOrDefaultAsync(x =>
                    x.Id == ingredientId &&
                    x.ProductionRunId == run.Id &&
                    x.FarmId == FarmId);

            if (ingredient == null)
            {
                return NotFound();
            }

            if (actualQuantity.HasValue &&
                actualQuantity.Value < 0)
            {
                await LoadFailureAsync(
                    run,
                    "Actual quantity cannot be negative.");

                return Page();
            }

            ingredient.ActualQuantity = actualQuantity;

            if (!run.ScaleFactor.HasValue &&
                actualQuantity.HasValue &&
                actualQuantity.Value > 0 &&
                ingredient.RecipeQuantity > 0)
            {
                run.ScaleFactor =
                    actualQuantity.Value /
                    ingredient.RecipeQuantity;

                run.ScaleAnchorIngredientId = ingredient.Id;
                ingredient.IsScaleAnchor = true;

                await RecalculateSuggestionsAsync(run);
            }

            if (run.IngredientsCommitted)
            {
                await ApplyInventoryDifferenceAsync(run, ingredient);
            }

            run.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostResetScaleAsync(
            int runId,
            int farmId)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            if (run.IngredientsCommitted)
            {
                return RedirectToRun(run.Id);
            }

            var ingredients = await GetIngredientsAsync(run.Id);

            run.ScaleFactor = null;
            run.ScaleAnchorIngredientId = null;

            foreach (var ingredient in ingredients)
            {
                ingredient.ActualQuantity = null;
                ingredient.SuggestedQuantity = ingredient.RecipeQuantity;
                ingredient.IsScaleAnchor = false;
            }

            run.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostStartBatchAsync(
            int runId,
            int farmId)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            var ingredients = await GetIngredientsAsync(run.Id);

            if (!ingredients.Any(x =>
                    x.ActualQuantity.HasValue &&
                    x.ActualQuantity.Value > 0))
            {
                await LoadFailureAsync(
                    run,
                    "Enter at least one actual ingredient quantity before starting the batch.");

                return Page();
            }

            foreach (var ingredient in ingredients)
            {
                await ApplyInventoryDifferenceAsync(run, ingredient);
            }

            run.IngredientsCommitted = true;
            run.InventoryReturned = false;
            run.Status = "In Progress";
            run.StartedAt ??= DateTime.Now;
            run.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostSaveProgressAsync(
            int runId,
            int farmId)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            var ingredients = await GetIngredientsAsync(run.Id);

            foreach (var ingredient in ingredients)
            {
                await ApplyInventoryDifferenceAsync(run, ingredient);
            }

            run.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostCompleteBatchAsync(
            int runId,
            int farmId)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            var ingredients = await GetIngredientsAsync(run.Id);

            foreach (var ingredient in ingredients)
            {
                await ApplyInventoryDifferenceAsync(run, ingredient);
            }

            run.IngredientsCommitted = true;
            run.Status = "Complete";
            run.EndedAt ??= DateTime.Now;
            run.UpdatedAt = DateTime.Now;

            if (!run.OutputPosted &&
                run.OutputInventoryItemId.HasValue &&
                run.ActualYieldQuantity.HasValue &&
                run.ActualYieldQuantity.Value > 0)
            {
                var outputItem = await _db.InventoryItems
                    .FirstOrDefaultAsync(x =>
                        x.Id == run.OutputInventoryItemId.Value &&
                        x.FarmId == FarmId);

                if (outputItem != null)
                {
                    var outputMovement = new InventoryMovement
                    {
                        FarmId = FarmId,
                        InventoryItemId = outputItem.Id,
                        InventoryItemPackageId =
                            run.OutputInventoryItemPackageId,
                        ReceivingLotId = null,
                        ProductionRunId = run.Id,
                        ProductionRunIngredientId = null,
                        MovementType = "Production Output",
                        Quantity = run.ActualYieldQuantity.Value,
                        Unit = string.IsNullOrWhiteSpace(run.ActualYieldUnit)
                            ? outputItem.BaseUnit
                            : run.ActualYieldUnit,
                        ReferenceNumber = run.BatchNumber,
                        Notes = $"Output from {run.RecipeName}",
                        OccurredAt = DateTime.Now,
                        CreatedAt = DateTime.Now
                    };

                    _db.InventoryMovements.Add(outputMovement);
                    run.OutputPosted = true;
                }
            }

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostCancelBatchAsync(
            int runId,
            int farmId)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            if (run.OutputPosted)
            {
                await LoadFailureAsync(
                    run,
                    "A completed batch with posted output cannot be cancelled through this page.");

                return Page();
            }

            var ingredients = await GetIngredientsAsync(run.Id);

            foreach (var ingredient in ingredients)
            {
                if (ingredient.InventoryItemId.HasValue &&
                    ingredient.CommittedQuantity != 0)
                {
                    var returnMovement = new InventoryMovement
                    {
                        FarmId = FarmId,
                        InventoryItemId = ingredient.InventoryItemId,
                        InventoryItemPackageId =
                            ingredient.InventoryItemPackageId,
                        ReceivingLotId = null,
                        ProductionRunId = run.Id,
                        ProductionRunIngredientId = ingredient.Id,
                        MovementType = "Production Cancellation Return",
                        Quantity = ingredient.CommittedQuantity,
                        Unit = ingredient.Unit,
                        ReferenceNumber = run.BatchNumber,
                        Notes = $"Returned from cancelled batch {run.BatchNumber}",
                        OccurredAt = DateTime.Now,
                        CreatedAt = DateTime.Now
                    };

                    _db.InventoryMovements.Add(returnMovement);
                    ingredient.CommittedQuantity = 0;
                }
            }

            run.InventoryReturned = true;
            run.IngredientsCommitted = false;
            run.Status = "Cancelled";
            run.EndedAt = DateTime.Now;
            run.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostAddWorkEntryAsync(
            int runId,
            int farmId)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            var defaultHours =
                NewWorkEntry.DefaultHours < 0
                    ? 0
                    : NewWorkEntry.DefaultHours;

            var entry = new ProductionWorkEntry
            {
                FarmId = FarmId,
                ProductionRunId = run.Id,
                WorkType = string.IsNullOrWhiteSpace(NewWorkEntry.WorkType)
                    ? "Production"
                    : NewWorkEntry.WorkType.Trim(),
                WorkDate = NewWorkEntry.WorkDate,
                StartedAt = null,
                EndedAt = null,
                DefaultHours = defaultHours,
                Notes = NewWorkEntry.Notes?.Trim() ?? "",
                CreatedAt = DateTime.Now
            };

            _db.ProductionWorkEntries.Add(entry);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(
                    NewWorkEntry.FirstWorkerName))
            {
                AddWorkerRecord(
                    run,
                    entry,
                    NewWorkEntry.FirstWorkerName,
                    defaultHours,
                    NewWorkEntry.FirstWorkerHourlyRate);
            }

            run.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostAddWorkerAsync(
            int workEntryId,
            int runId,
            int farmId,
            string workerName,
            decimal hoursWorked,
            decimal? hourlyRate)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            var entry = await _db.ProductionWorkEntries
                .FirstOrDefaultAsync(x =>
                    x.Id == workEntryId &&
                    x.ProductionRunId == run.Id &&
                    x.FarmId == FarmId);

            if (entry == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(workerName))
            {
                AddWorkerRecord(
                    run,
                    entry,
                    workerName,
                    Math.Max(0, hoursWorked),
                    hourlyRate);

                run.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();
            }

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostUpdateWorkerAsync(
            int workerId,
            int runId,
            int farmId,
            decimal hoursWorked,
            decimal? hourlyRate)
        {
            FarmId = farmId;

            var worker = await _db.ProductionWorkEntryWorkers
                .FirstOrDefaultAsync(x =>
                    x.Id == workerId &&
                    x.ProductionRunId == runId &&
                    x.FarmId == FarmId);

            if (worker == null)
            {
                return NotFound();
            }

            worker.HoursWorked = Math.Max(0, hoursWorked);
            worker.HourlyRate = hourlyRate;
            worker.LabourCost = hourlyRate.HasValue
                ? worker.HoursWorked * hourlyRate.Value
                : null;

            await _db.SaveChangesAsync();

            return RedirectToRun(runId);
        }

        public async Task<IActionResult> OnPostAddCheckAsync(
            int runId,
            int farmId)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            var workEntry = new ProductionWorkEntry
            {
                FarmId = FarmId,
                ProductionRunId = run.Id,
                WorkType = "Batch Check",
                WorkDate = NewCheck.RecordedAt,
                StartedAt = null,
                EndedAt = null,
                DefaultHours = Math.Max(0, NewCheck.WorkerHours),
                Notes = NewCheck.Notes?.Trim() ?? "",
                CreatedAt = DateTime.Now
            };

            _db.ProductionWorkEntries.Add(workEntry);
            await _db.SaveChangesAsync();

            AddWorkerRecord(
                run,
                workEntry,
                NewCheck.WorkerName,
                Math.Max(0, NewCheck.WorkerHours),
                NewCheck.HourlyRate);

            var observation = new ProductionObservation
            {
                FarmId = FarmId,
                ProductionRunId = run.Id,
                ProductionWorkEntryId = workEntry.Id,
                RecordedAt = NewCheck.RecordedAt,
                ObservationType =
                    string.IsNullOrWhiteSpace(NewCheck.ObservationType)
                        ? "Batch Check"
                        : NewCheck.ObservationType.Trim(),
                Notes = NewCheck.Notes?.Trim() ?? "",
                CreatedAt = DateTime.Now
            };

            _db.ProductionObservations.Add(observation);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(
                    NewCheck.MeasurementName))
            {
                AddObservationValueRecord(
                    run,
                    observation,
                    NewCheck.MeasurementName,
                    NewCheck.ValueText,
                    NewCheck.Unit,
                    "",
                    1);
            }

            run.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToRun(run.Id);
        }

        public async Task<IActionResult> OnPostAddObservationValueAsync(
            int observationId,
            int runId,
            int farmId,
            string measurementName,
            string valueText,
            string unit,
            string notes)
        {
            FarmId = farmId;

            var run = await FindRunAsync(runId);

            if (run == null)
            {
                return NotFound();
            }

            var observation = await _db.ProductionObservations
                .FirstOrDefaultAsync(x =>
                    x.Id == observationId &&
                    x.ProductionRunId == run.Id &&
                    x.FarmId == FarmId);

            if (observation == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(measurementName))
            {
                var nextSortOrder =
                    await _db.ProductionObservationValues
                        .Where(x =>
                            x.ProductionObservationId ==
                            observation.Id &&
                            x.FarmId == FarmId)
                        .Select(x => (int?)x.SortOrder)
                        .MaxAsync() ?? 0;

                AddObservationValueRecord(
                    run,
                    observation,
                    measurementName,
                    valueText,
                    unit,
                    notes,
                    nextSortOrder + 1);

                await _db.SaveChangesAsync();
            }

            return RedirectToRun(run.Id);
        }

        private async Task ApplyInventoryDifferenceAsync(
            ProductionRun run,
            ProductionRunIngredient ingredient)
        {
            if (!ingredient.InventoryItemId.HasValue)
            {
                return;
            }

            var actual = ingredient.ActualQuantity ?? 0;
            var difference =
                actual - ingredient.CommittedQuantity;

            if (difference == 0)
            {
                return;
            }

            var movement = new InventoryMovement
            {
                FarmId = FarmId,
                InventoryItemId = ingredient.InventoryItemId,
                InventoryItemPackageId =
                    ingredient.InventoryItemPackageId,
                ReceivingLotId = null,
                ProductionRunId = run.Id,
                ProductionRunIngredientId = ingredient.Id,
                MovementType =
                    difference > 0
                        ? "Production Consumption"
                        : "Production Quantity Return",
                Quantity = -difference,
                Unit = ingredient.Unit,
                ReferenceNumber = run.BatchNumber,
                Notes =
                    $"{ingredient.IngredientName} for {run.BatchNumber}",
                OccurredAt = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _db.InventoryMovements.Add(movement);

            ingredient.CommittedQuantity = actual;
        }

        private async Task RecalculateSuggestionsAsync(
            ProductionRun run)
        {
            var ingredients = await GetIngredientsAsync(run.Id);

            foreach (var ingredient in ingredients)
            {
                ingredient.SuggestedQuantity =
                    run.ScaleFactor.HasValue
                        ? ingredient.RecipeQuantity *
                          run.ScaleFactor.Value
                        : ingredient.RecipeQuantity;

                ingredient.IsScaleAnchor =
                    run.ScaleAnchorIngredientId ==
                    ingredient.Id;
            }
        }

        private void AddWorkerRecord(
            ProductionRun run,
            ProductionWorkEntry entry,
            string workerName,
            decimal hoursWorked,
            decimal? hourlyRate)
        {
            if (string.IsNullOrWhiteSpace(workerName))
            {
                return;
            }

            var worker = new ProductionWorkEntryWorker
            {
                FarmId = FarmId,
                ProductionRunId = run.Id,
                ProductionWorkEntryId = entry.Id,
                WorkerName = workerName.Trim(),
                HoursWorked = hoursWorked,
                HourlyRate = hourlyRate,
                LabourCost = hourlyRate.HasValue
                    ? hoursWorked * hourlyRate.Value
                    : null,
                CreatedAt = DateTime.Now
            };

            _db.ProductionWorkEntryWorkers.Add(worker);
        }

        private void AddObservationValueRecord(
            ProductionRun run,
            ProductionObservation observation,
            string measurementName,
            string valueText,
            string unit,
            string notes,
            int sortOrder)
        {
            decimal? numericValue = null;

            if (decimal.TryParse(valueText, out var parsed))
            {
                numericValue = parsed;
            }

            var value = new ProductionObservationValue
            {
                FarmId = FarmId,
                ProductionRunId = run.Id,
                ProductionObservationId = observation.Id,
                MeasurementName =
                    measurementName?.Trim() ?? "",
                ValueText = valueText?.Trim() ?? "",
                NumericValue = numericValue,
                Unit = unit?.Trim() ?? "",
                Notes = notes?.Trim() ?? "",
                SortOrder = sortOrder,
                CreatedAt = DateTime.Now
            };

            _db.ProductionObservationValues.Add(value);
        }

        private async Task<ProductionRun?> FindRunAsync(int runId)
        {
            return await _db.ProductionRuns
                .FirstOrDefaultAsync(x =>
                    x.Id == runId &&
                    x.FarmId == FarmId);
        }

        private async Task<List<ProductionRunIngredient>>
            GetIngredientsAsync(int runId)
        {
            return await _db.ProductionRunIngredients
                .Where(x =>
                    x.ProductionRunId == runId &&
                    x.FarmId == FarmId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        private async Task LoadPageDataAsync(int runId)
        {
            Ingredients = await GetIngredientsAsync(runId);
            RecipeOptions = await _db.Recipes.AsNoTracking().Where(x => x.FarmId == FarmId && x.IsActive).OrderBy(x => x.RecipeName).ToListAsync();
            ReceivingOptions = await _db.ReceivingLots.AsNoTracking().Where(x => x.FarmId == FarmId).OrderByDescending(x => x.ReceivedDate).ThenByDescending(x => x.Id).ToListAsync();
            var harvestFields = await _db.ReceivingLotCustomFields.AsNoTracking().Where(x => x.FarmId == FarmId && x.FieldName == HarvestTransfer.FieldName).ToListAsync();
            ReceiptHarvests = new();
            foreach (var field in harvestFields)
                if (HarvestTransfer.TryParse(field.FieldValue, out var harvest)) ReceiptHarvests[field.ReceivingLotId] = harvest!;
            IngredientSources = await _db.ProductionIngredientSources.AsNoTracking().Where(x => x.FarmId == FarmId && x.ProductionRunId == runId).ToDictionaryAsync(x => x.ProductionRunIngredientId);
            SourceDetails = new();
            foreach (var entry in IngredientSources)
                if (ReceivingTransfer.TryParse(entry.Value.ReceiptPayload, out var receipt)) SourceDetails[entry.Key] = receipt!;
            var sourceRun = await FindRunAsync(runId);
            CanLoadRecipe = sourceRun != null && sourceRun.Status == "Draft" && !sourceRun.IngredientsCommitted
                && !sourceRun.OutputPosted && sourceRun.StartedAt == null && sourceRun.ScaleFactor == null
                && sourceRun.ActualYieldQuantity == null && IngredientSources.Count == 0
                && Ingredients.All(x => x.ActualQuantity == null && x.CommittedQuantity == 0)
                && !await _db.ProductionWorkEntries.AnyAsync(x => x.FarmId == FarmId && x.ProductionRunId == runId)
                && !await _db.ProductionObservations.AnyAsync(x => x.FarmId == FarmId && x.ProductionRunId == runId);

            WorkEntries = await _db.ProductionWorkEntries
                .Where(x =>
                    x.ProductionRunId == runId &&
                    x.FarmId == FarmId)
                .OrderByDescending(x => x.WorkDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            Workers = await _db.ProductionWorkEntryWorkers
                .Where(x =>
                    x.ProductionRunId == runId &&
                    x.FarmId == FarmId)
                .OrderBy(x => x.WorkerName)
                .ToListAsync();

            Observations = await _db.ProductionObservations
                .Where(x =>
                    x.ProductionRunId == runId &&
                    x.FarmId == FarmId)
                .OrderByDescending(x => x.RecordedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            ObservationValues =
                await _db.ProductionObservationValues
                    .Where(x =>
                        x.ProductionRunId == runId &&
                        x.FarmId == FarmId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Id)
                    .ToListAsync();

            InventoryItemOptions = await _db.InventoryItems
                .Where(x =>
                    x.FarmId == FarmId &&
                    x.IsActive)
                .OrderBy(x => x.ItemName)
                .Select(x => new InventoryItemOption
                {
                    Id = x.Id,
                    DisplayName =
                        $"{x.ItemName} ({x.BaseUnit})"
                })
                .ToListAsync();

            TotalLabourHours =
                Workers.Sum(x => x.HoursWorked);

            var labourCosts = Workers
                .Where(x => x.LabourCost.HasValue)
                .Select(x => x.LabourCost!.Value)
                .ToList();

            TotalLabourCost =
                labourCosts.Any()
                    ? labourCosts.Sum()
                    : null;
        }

        private async Task LoadFailureAsync(
            ProductionRun run,
            string message)
        {
            ProductionRun = run;
            ErrorMessage = message;

            await LoadPageDataAsync(run.Id);
        }

        private RedirectToPageResult RedirectToRun(int runId)
        {
            return RedirectToPage(
                "/Farmer/Inventory/ProductionRunDetails",
                new
                {
                    id = runId,
                    farmId = FarmId
                });
        }

        public List<ProductionWorkEntryWorker>
            GetWorkers(int workEntryId)
        {
            return Workers
                .Where(x =>
                    x.ProductionWorkEntryId ==
                    workEntryId)
                .OrderBy(x => x.WorkerName)
                .ToList();
        }

        public decimal GetWorkEntryHours(int workEntryId)
        {
            return Workers
                .Where(x =>
                    x.ProductionWorkEntryId ==
                    workEntryId)
                .Sum(x => x.HoursWorked);
        }

        public List<ProductionObservationValue>
            GetObservationValues(int observationId)
        {
            return ObservationValues
                .Where(x =>
                    x.ProductionObservationId ==
                    observationId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public List<ProductionWorkEntryWorker>
            GetObservationWorkers(
                ProductionObservation observation)
        {
            if (!observation
                .ProductionWorkEntryId.HasValue)
            {
                return new List<
                    ProductionWorkEntryWorker>();
            }

            return GetWorkers(
                observation.ProductionWorkEntryId.Value);
        }

        public string GetIngredientState(
            ProductionRunIngredient ingredient)
        {
            if (!ingredient.ActualQuantity.HasValue)
            {
                return "Suggested";
            }

            if (ingredient.IsScaleAnchor)
            {
                return "Anchor";
            }

            if (!ingredient.SuggestedQuantity.HasValue)
            {
                return "Actual";
            }

            var suggested =
                ingredient.SuggestedQuantity.Value;

            var actual =
                ingredient.ActualQuantity.Value;

            var tolerance = Math.Max(
                0.001m,
                Math.Abs(suggested) * 0.001m);

            return Math.Abs(actual - suggested)
                   <= tolerance
                ? "Match"
                : "Difference";
        }

        public string GetActualQuantityStyle(
            string state)
        {
            return state switch
            {
                "Difference" =>
                    "color:red; font-weight:bold;",

                "Suggested" =>
                    "color:grey;",

                _ =>
                    "color:black; font-weight:bold;"
            };
        }

        public class NewWorkEntryInput
        {
            public string WorkType { get; set; }
                = "Production";

            public DateTime WorkDate { get; set; }
                = DateTime.Now;

            public decimal DefaultHours { get; set; }
                = 1;

            public string FirstWorkerName { get; set; }
                = "";

            public decimal? FirstWorkerHourlyRate
            { get; set; }

            public string Notes { get; set; } = "";
        }

        public class NewCheckInput
        {
            public DateTime RecordedAt { get; set; }
                = DateTime.Now;

            public string ObservationType { get; set; }
                = "Batch Check";

            public string WorkerName { get; set; }
                = "";

            public decimal WorkerHours { get; set; }
                = 1;

            public decimal? HourlyRate { get; set; }

            public string MeasurementName { get; set; }
                = "";

            public string ValueText { get; set; }
                = "";

            public string Unit { get; set; }
                = "";

            public string Notes { get; set; }
                = "";
        }

        public class InventoryItemOption
        {
            public int Id { get; set; }

            public string DisplayName { get; set; }
                = "";
        }
    }
}
