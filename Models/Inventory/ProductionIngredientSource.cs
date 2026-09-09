using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Models.Inventory;

[Index(nameof(FarmId), nameof(ProductionRunIngredientId), IsUnique = true)]
public class ProductionIngredientSource
{
    public int Id { get; set; }
    public int FarmId { get; set; }
    public int ProductionRunId { get; set; }
    public int ProductionRunIngredientId { get; set; }
    public int? ReceivingLotId { get; set; }
    public string ReceiptPayload { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.Now;
}
