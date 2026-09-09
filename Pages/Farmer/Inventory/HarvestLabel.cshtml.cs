using Agriloco.Api.Data;
using Agriloco1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Agriloco1.Pages.Farmer.Inventory;

public class HarvestLabelModel(AgrilocoContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public int FarmId { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public HarvestTransfer Source { get; set; } = null!;
    public string Payload { get; set; } = "";
    public string Image { get; set; } = "";
    public string? Error { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        var lot = await db.HarvestLots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Id && x.FarmId == FarmId);
        var farm = await db.Farms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == FarmId);
        if (lot == null || farm == null) return NotFound();
        Source = HarvestTransfer.From(lot, farm);
        Payload = Source.Encode();
        if (!HarvestTransfer.TryParse(Payload, out _))
        {
            Error = "This harvest needs a farm name, crop, lot number, date and positive quantity with a unit. Its sharing details must fit within 2,000 bytes. Check the harvest before making a label.";
            return Page();
        }
        using var data = QRCodeGenerator.GenerateQrCode(Payload, QRCodeGenerator.ECCLevel.M);
        using var png = new PngByteQRCode(data);
        Image = "data:image/png;base64," + Convert.ToBase64String(png.GetGraphic(8));
        return Page();
    }
}
