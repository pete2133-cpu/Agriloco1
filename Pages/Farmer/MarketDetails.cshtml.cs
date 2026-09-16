using System.ComponentModel.DataAnnotations;
using Agriloco.Api.Data;
using Agriloco.Api.Models;
using Agriloco.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Pages.Farmer;

[FarmMapWrite(IncludeReads = true)]
[RequestSizeLimit(6 * 1024 * 1024)]
public class MarketDetailsModel(AgrilocoContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public int FarmId { get; set; }
    [BindProperty(SupportsGet = true)] public int ItemId { get; set; }
    [BindProperty] public ListingInput Listing { get; set; } = new();
    [BindProperty] public List<OptionInput> Options { get; set; } = new();
    [BindProperty] public IFormFile? ImageUpload { get; set; }
    [BindProperty] public bool RemoveImage { get; set; }
    public string ItemName { get; set; } = "";
    public bool HasImage { get; set; }

    private async Task<bool> LoadItem()
    {
        var item = await db.FarmDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == ItemId && d.FarmId == FarmId && d.IsActive);
        if (item == null) return false;
        ItemName = item.DisplayName;
        HasImage = await db.MarketDetails.AnyAsync(d => d.FarmDefinitionId == ItemId && d.Image != null);
        return true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadItem()) return NotFound();
        var detail = await db.MarketDetails.AsNoTracking().Include(d => d.SellingOptions)
            .FirstOrDefaultAsync(d => d.FarmDefinitionId == ItemId);
        if (detail != null)
        {
            Listing = new() { DisplayName = detail.DisplayName, Description = detail.Description, Category = detail.Category };
            Options = detail.SellingOptions.OrderBy(o => o.SortOrder).ThenBy(o => o.Id).Select(o => new OptionInput
            {
                Id = o.Id, Name = o.Name, Price = o.Price, Currency = o.Currency,
                SellQuantity = o.SellQuantity, SellUnit = o.SellUnit, PackageType = o.PackageType,
                Sku = o.Sku, Barcode = o.Barcode, TrackInventory = o.TrackInventory, IsPublic = o.IsPublic
            }).ToList();
        }
        return Page();
    }

    public async Task<IActionResult> OnGetImageAsync()
    {
        if (!await LoadItem()) return NotFound();
        var detail = await db.MarketDetails.AsNoTracking().FirstOrDefaultAsync(d => d.FarmDefinitionId == ItemId);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return detail?.Image == null ? NotFound() : File(detail.Image, detail.ImageContentType!);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await LoadItem()) return NotFound();
        if (Options.Count > 25) ModelState.AddModelError("", "Use no more than 25 selling options.");
        for (var i = 0; i < Options.Count; i++)
        {
            var o = Options[i];
            if (!string.IsNullOrEmpty(o.SellUnit) && !MarketUnits.Units.Contains(o.SellUnit))
                ModelState.AddModelError($"Options[{i}].SellUnit", "Choose a listed unit.");
            if (!string.IsNullOrEmpty(o.PackageType) && !MarketUnits.Packages.Contains(o.PackageType))
                ModelState.AddModelError($"Options[{i}].PackageType", "Choose a listed package.");
        }
        var detail = await db.MarketDetails.Include(d => d.SellingOptions).FirstOrDefaultAsync(d => d.FarmDefinitionId == ItemId);
        var existing = detail?.SellingOptions.ToDictionary(o => o.Id) ?? new();
        var submittedIds = Options.Where(o => o.Id != 0).Select(o => o.Id).ToList();
        if (submittedIds.Distinct().Count() != submittedIds.Count || submittedIds.Any(id => !existing.ContainsKey(id)))
            return BadRequest("A selling option does not belong to this item. Reload the page.");
        byte[]? image = null; string? imageType = null;
        if (ImageUpload != null && ImageUpload.Length > 0)
        {
            if (ImageUpload.Length > 5 * 1024 * 1024) ModelState.AddModelError("ImageUpload", "Image must be 5 MB or smaller.");
            else
            {
                using var stream = new MemoryStream(); await ImageUpload.CopyToAsync(stream); image = stream.ToArray();
                imageType = image.Length >= 12 && image.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ? "image/png" :
                    image.Length >= 12 && image[0] == 255 && image[1] == 216 && image[2] == 255 ? "image/jpeg" :
                    image.Length >= 12 && System.Text.Encoding.ASCII.GetString(image, 0, 4) == "RIFF" &&
                    System.Text.Encoding.ASCII.GetString(image, 8, 4) == "WEBP" ? "image/webp" : null;
                if (imageType == null) ModelState.AddModelError("ImageUpload", "Choose a PNG, JPEG or WebP image.");
            }
        }
        if (!ModelState.IsValid) return Page();
        if (detail == null) { detail = new() { FarmDefinitionId = ItemId }; db.MarketDetails.Add(detail); }
        detail.DisplayName = Clean(Listing.DisplayName); detail.Description = Clean(Listing.Description); detail.Category = Clean(Listing.Category);
        if (RemoveImage) { detail.Image = null; detail.ImageContentType = null; }
        if (image != null) { detail.Image = image; detail.ImageContentType = imageType; }
        db.MarketSellingOptions.RemoveRange(existing.Values.Where(o => !submittedIds.Contains(o.Id)));
        for (var i = 0; i < Options.Count; i++)
        {
            var input = Options[i];
            var option = input.Id == 0 ? new MarketSellingOption { FarmDefinitionId = ItemId } : existing[input.Id];
            if (input.Id == 0) detail.SellingOptions.Add(option);
            option.Name = Clean(input.Name); option.Price = input.Price; option.Currency = (Clean(input.Currency) ?? "CAD").ToUpperInvariant();
            option.SellQuantity = input.SellQuantity; option.SellUnit = Clean(input.SellUnit); option.PackageType = Clean(input.PackageType);
            option.Sku = Clean(input.Sku); option.Barcode = Clean(input.Barcode); option.TrackInventory = input.TrackInventory;
            option.IsPublic = input.IsPublic; option.SortOrder = i;
        }
        await db.SaveChangesAsync();
        TempData["MarketSaved"] = "Market details saved.";
        return RedirectToPage(new { FarmId, ItemId });
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public class ListingInput
    {
        [StringLength(160)] public string? DisplayName { get; set; }
        [StringLength(500)] public string? Description { get; set; }
        [StringLength(100)] public string? Category { get; set; }
    }
    public class OptionInput
    {
        public int Id { get; set; }
        [StringLength(100)] public string? Name { get; set; }
        [Range(typeof(decimal), "0", "999999999999")] public decimal? Price { get; set; }
        [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Use a three-letter currency code, such as CAD or USD.")]
        public string? Currency { get; set; } = "CAD";
        [Range(typeof(decimal), "0.000001", "999999999999")] public decimal? SellQuantity { get; set; }
        public string? SellUnit { get; set; }
        public string? PackageType { get; set; }
        [StringLength(100)] public string? Sku { get; set; }
        [StringLength(100)] public string? Barcode { get; set; }
        public bool TrackInventory { get; set; }
        public bool IsPublic { get; set; } = true;
    }
}
