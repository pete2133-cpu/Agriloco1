using Agriloco.Api.Data;
using Agriloco1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;

namespace Agriloco1.Pages.Farmer.Inventory;

public class ReceivingLabelModel(AgrilocoContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public int FarmId { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public ReceivingTransfer? Source { get; set; }
    public string Payload { get; set; } = "";
    public string Image { get; set; } = "";
    public string? Error { get; set; }
    public async Task OnGetAsync()
    {
        Source = await ReceivingTransfer.FromLotAsync(db, FarmId, Id);
        Payload = Source?.Encode() ?? "";
        if (!ReceivingTransfer.TryParse(Payload, out _))
        {
            Error = "Cannot make this label. Check that the receipt exists for this farm, has a positive quantity and complete item/unit information, and its harvest source is readable. Combined sharing details must fit within 2,200 bytes.";
            return;
        }
        using var data = QRCodeGenerator.GenerateQrCode(Payload, QRCodeGenerator.ECCLevel.M);
        using var png = new PngByteQRCode(data);
        Image = "data:image/png;base64," + Convert.ToBase64String(png.GetGraphic(8));
    }
}
