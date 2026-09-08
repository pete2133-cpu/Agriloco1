using Agriloco.Api.Data;
using Agriloco1.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer
{
    public class AddModel : PageModel
    {
        private readonly AgrilocoContext _db;

        public AddModel(AgrilocoContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int FarmId { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? ParentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        [BindProperty]
        public NewDefinitionInput NewItem { get; set; } = new();

        [BindProperty]
        public EditDefinitionInput EditItem { get; set; } = new();

        public string FarmName { get; set; } = "Agriloco Farm";

        public List<FarmDefinition> ExistingDefinitions { get; set; } = new();

        public FarmDefinition? SelectedParent { get; set; }

        public FarmDefinition? ItemBeingEdited { get; set; }

        public List<string> DefinitionTypes { get; } = new()
        {
            "Product",
            "Variety",
            "Location",
            "Production Unit",
            "Process",
            "Package",
            "Other"
        };

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadPageAsync();

            if (ParentId.HasValue)
            {
                SelectedParent = ExistingDefinitions
                    .FirstOrDefault(x => x.Id == ParentId.Value);

                // Do not allow a parent ID from another farm.
                if (SelectedParent == null)
                {
                    ParentId = null;
                }
            }

            if (EditId.HasValue)
            {
                ItemBeingEdited = ExistingDefinitions
                    .FirstOrDefault(x => x.Id == EditId.Value);

                if (ItemBeingEdited == null)
                {
                    EditId = null;
                }
                else
                {
                    EditItem.Id = ItemBeingEdited.Id;
                    EditItem.DisplayName = ItemBeingEdited.DisplayName;
                    EditItem.DefinitionType = ItemBeingEdited.DefinitionType;
                    EditItem.ParentFarmDefinitionId =
                        ItemBeingEdited.ParentFarmDefinitionId;
                    EditItem.IsPublic = ItemBeingEdited.IsPublic;
                    EditItem.Notes = ItemBeingEdited.Notes;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            await LoadPageAsync();

            // ParentId comes directly from the specific
            // item's + Add button.
            FarmDefinition? parent = null;

            if (ParentId.HasValue)
            {
                parent = await _db.FarmDefinitions
                    .FirstOrDefaultAsync(x =>
                        x.Id == ParentId.Value &&
                        x.FarmId == FarmId &&
                        x.IsActive);

                if (parent == null)
                {
                    ModelState.AddModelError(
                        "",
                        "The parent item could not be found.");

                    ParentId = null;

                    return Page();
                }

                SelectedParent = parent;
            }

            if (string.IsNullOrWhiteSpace(NewItem.DisplayName))
            {
                ModelState.AddModelError(
                    "NewItem.DisplayName",
                    "Enter a name for the item.");

                return Page();
            }

            var displayName = NewItem.DisplayName.Trim();

            var duplicateExists = await _db.FarmDefinitions
                .AnyAsync(x =>
                    x.FarmId == FarmId &&
                    x.IsActive &&
                    x.ParentFarmDefinitionId == ParentId &&
                    x.DisplayName.ToLower() ==
                        displayName.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "NewItem.DisplayName",
                    "An item with this name already exists under this parent.");

                return Page();
            }

            var maxSortOrder = await _db.FarmDefinitions
                .Where(x =>
                    x.FarmId == FarmId &&
                    x.ParentFarmDefinitionId == ParentId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? 0;

            var item = new FarmDefinition
            {
                FarmId = FarmId,

                DefinitionId = null,

                ParentFarmDefinitionId = ParentId,

                DisplayName = displayName,

                DefinitionType =
                    string.IsNullOrWhiteSpace(NewItem.DefinitionType)
                        ? "Other"
                        : NewItem.DefinitionType.Trim(),

                Status = "Unavailable",

                PresentationMode = "Index",

                IsPublic = NewItem.IsPublic,

                IsActive = true,

                SortOrder = maxSortOrder + 10,

                Notes = NewItem.Notes?.Trim() ?? "",

                CreatedAt = DateTime.Now,

                UpdatedAt = DateTime.Now
            };

            _db.FarmDefinitions.Add(item);

            await _db.SaveChangesAsync();

            return RedirectToPage(
                "/Farmer/Add",
                new
                {
                    farmId = FarmId
                });
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            await LoadPageAsync();

            var item = await _db.FarmDefinitions
                .FirstOrDefaultAsync(x =>
                    x.Id == EditItem.Id &&
                    x.FarmId == FarmId &&
                    x.IsActive);

            if (item == null)
            {
                return RedirectToPage(
                    new
                    {
                        farmId = FarmId
                    });
            }

            if (string.IsNullOrWhiteSpace(EditItem.DisplayName))
            {
                ModelState.AddModelError(
                    "EditItem.DisplayName",
                    "Enter a name for the item.");

                ItemBeingEdited = item;
                EditId = item.Id;

                return Page();
            }

            // An item cannot be its own parent.
            if (EditItem.ParentFarmDefinitionId == item.Id)
            {
                ModelState.AddModelError(
                    "EditItem.ParentFarmDefinitionId",
                    "An item cannot be its own parent.");

                ItemBeingEdited = item;
                EditId = item.Id;

                return Page();
            }

            // Verify that the selected new parent belongs
            // to this farm.
            if (EditItem.ParentFarmDefinitionId.HasValue)
            {
                var newParent = await _db.FarmDefinitions
                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                            EditItem.ParentFarmDefinitionId.Value &&
                        x.FarmId == FarmId &&
                        x.IsActive);

                if (newParent == null)
                {
                    ModelState.AddModelError(
                        "EditItem.ParentFarmDefinitionId",
                        "The selected parent could not be found.");

                    ItemBeingEdited = item;
                    EditId = item.Id;

                    return Page();
                }

                // Prevent moving a parent underneath
                // one of its own descendants.
                if (IsDescendant(
                    potentialDescendantId: newParent.Id,
                    potentialAncestorId: item.Id))
                {
                    ModelState.AddModelError(
                        "EditItem.ParentFarmDefinitionId",
                        "This move would create a circular hierarchy.");

                    ItemBeingEdited = item;
                    EditId = item.Id;

                    return Page();
                }
            }

            var newName = EditItem.DisplayName.Trim();

            var duplicateExists = await _db.FarmDefinitions
                .AnyAsync(x =>
                    x.Id != item.Id &&
                    x.FarmId == FarmId &&
                    x.IsActive &&
                    x.ParentFarmDefinitionId ==
                        EditItem.ParentFarmDefinitionId &&
                    x.DisplayName.ToLower() ==
                        newName.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "EditItem.DisplayName",
                    "An item with this name already exists under that parent.");

                ItemBeingEdited = item;
                EditId = item.Id;

                return Page();
            }

            item.DisplayName = newName;

            item.DefinitionType =
                string.IsNullOrWhiteSpace(EditItem.DefinitionType)
                    ? "Other"
                    : EditItem.DefinitionType.Trim();

            item.ParentFarmDefinitionId =
                EditItem.ParentFarmDefinitionId;

            item.IsPublic = EditItem.IsPublic;

            item.Notes =
                EditItem.Notes?.Trim() ?? "";

            item.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return RedirectToPage(
                new
                {
                    farmId = FarmId
                });
        }

        // ============================================================
        // DELETE ITEM / BRANCH
        // ============================================================

        public async Task<IActionResult> OnPostDeleteAsync(
            int farmId,
            int id)
        {
            FarmId = farmId;

            var allDefinitions = await _db.FarmDefinitions
                .Where(x => x.FarmId == FarmId)
                .ToListAsync();

            var item = allDefinitions
                .FirstOrDefault(x => x.Id == id);

            if (item == null)
            {
                return RedirectToPage(
                    new
                    {
                        farmId = FarmId
                    });
            }

            // Collect the selected item and every
            // descendant underneath it.
            var idsToDelete = new HashSet<int>();

            void AddBranch(int definitionId)
            {
                if (!idsToDelete.Add(definitionId))
                {
                    return;
                }

                var children = allDefinitions
                    .Where(x =>
                        x.ParentFarmDefinitionId ==
                        definitionId)
                    .ToList();

                foreach (var child in children)
                {
                    AddBranch(child.Id);
                }
            }

            AddBranch(item.Id);

            // Remove availability channel connections.
            var channelLinks =
                await _db.FarmDefinitionChannels
                    .Where(x =>
                        idsToDelete.Contains(
                            x.FarmDefinitionId))
                    .ToListAsync();

            if (channelLinks.Count > 0)
            {
                _db.FarmDefinitionChannels
                    .RemoveRange(channelLinks);
            }

            // Remove records connected to the branch.
            var records =
                await _db.DefinitionRecords
                    .Where(x =>
                        x.FarmId == FarmId &&
                        idsToDelete.Contains(
                            x.FarmDefinitionId))
                    .ToListAsync();

            if (records.Count > 0)
            {
                _db.DefinitionRecords
                    .RemoveRange(records);
            }

            // Delete deepest children before parents.
            var definitionsToDelete =
                allDefinitions
                    .Where(x =>
                        idsToDelete.Contains(x.Id))
                    .OrderByDescending(x =>
                        GetDepthForDelete(
                            x,
                            allDefinitions))
                    .ToList();

            _db.FarmDefinitions
                .RemoveRange(definitionsToDelete);

            await _db.SaveChangesAsync();

            return RedirectToPage(
                new
                {
                    farmId = FarmId
                });
        }

        private int GetDepthForDelete(
            FarmDefinition item,
            List<FarmDefinition> definitions)
        {
            var depth = 0;

            var parentId =
                item.ParentFarmDefinitionId;

            var safety = 0;

            while (parentId.HasValue &&
                   safety < 50)
            {
                var parent = definitions
                    .FirstOrDefault(x =>
                        x.Id == parentId.Value);

                if (parent == null)
                {
                    break;
                }

                depth++;

                parentId =
                    parent.ParentFarmDefinitionId;

                safety++;
            }

            return depth;
        }

        // ============================================================
        // LOAD / HIERARCHY
        // ============================================================

        private async Task LoadPageAsync()
        {
            var farm = await _db.Farms
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == FarmId);

            if (farm != null)
            {
                FarmName = farm.Name;
            }

            ExistingDefinitions =
                await _db.FarmDefinitions
                    .AsNoTracking()
                    .Where(x =>
                        x.FarmId == FarmId &&
                        x.IsActive)
                    .ToListAsync();

            // Parents are followed immediately by
            // their own children.
            ExistingDefinitions =
                OrderHierarchy(
                    ExistingDefinitions);
        }

        private List<FarmDefinition> OrderHierarchy(
            List<FarmDefinition> source)
        {
            var result =
                new List<FarmDefinition>();

            void AddChildren(int? parentId)
            {
                var children = source
                    .Where(x =>
                        x.ParentFarmDefinitionId ==
                        parentId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.DisplayName)
                    .ToList();

                foreach (var child in children)
                {
                    result.Add(child);

                    AddChildren(child.Id);
                }
            }

            AddChildren(null);

            // Safety fallback for old/orphaned records.
            foreach (var item in source)
            {
                if (!result.Any(x =>
                    x.Id == item.Id))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private bool IsDescendant(
            int potentialDescendantId,
            int potentialAncestorId)
        {
            var current =
                ExistingDefinitions
                    .FirstOrDefault(x =>
                        x.Id ==
                        potentialDescendantId);

            var safety = 0;

            while (current != null &&
                   current.ParentFarmDefinitionId.HasValue &&
                   safety < 50)
            {
                if (current
                    .ParentFarmDefinitionId
                    .Value ==
                    potentialAncestorId)
                {
                    return true;
                }

                var parentId =
                    current.ParentFarmDefinitionId.Value;

                current =
                    ExistingDefinitions
                        .FirstOrDefault(x =>
                            x.Id == parentId);

                safety++;
            }

            return false;
        }

        public int GetDepth(
            FarmDefinition item)
        {
            var depth = 0;

            var parentId =
                item.ParentFarmDefinitionId;

            var safety = 0;

            while (parentId.HasValue &&
                   safety < 50)
            {
                var parent =
                    ExistingDefinitions
                        .FirstOrDefault(x =>
                            x.Id ==
                            parentId.Value);

                if (parent == null)
                {
                    break;
                }

                depth++;

                parentId =
                    parent.ParentFarmDefinitionId;

                safety++;
            }

            return depth;
        }

        public string GetHierarchyName(
            FarmDefinition item)
        {
            var names = new List<string>
            {
                item.DisplayName
            };

            var parentId =
                item.ParentFarmDefinitionId;

            var safety = 0;

            while (parentId.HasValue &&
                   safety < 50)
            {
                var parent =
                    ExistingDefinitions
                        .FirstOrDefault(x =>
                            x.Id ==
                            parentId.Value);

                if (parent == null)
                {
                    break;
                }

                names.Insert(
                    0,
                    parent.DisplayName);

                parentId =
                    parent.ParentFarmDefinitionId;

                safety++;
            }

            return string.Join(
                " ? ",
                names);
        }

        public bool CanBeParentFor(
            FarmDefinition possibleParent,
            int itemBeingMovedId)
        {
            if (possibleParent.Id ==
                itemBeingMovedId)
            {
                return false;
            }

            return !IsDescendant(
                possibleParent.Id,
                itemBeingMovedId);
        }

        public class NewDefinitionInput
        {
            public string DisplayName { get; set; } = "";

            public string DefinitionType { get; set; } =
                "Product";

            public bool IsPublic { get; set; } = true;

            public string Notes { get; set; } = "";
        }

        public class EditDefinitionInput
        {
            public int Id { get; set; }

            public int? ParentFarmDefinitionId { get; set; }

            public string DisplayName { get; set; } = "";

            public string DefinitionType { get; set; } =
                "Product";

            public bool IsPublic { get; set; }

            public string Notes { get; set; } = "";
        }
    }
}