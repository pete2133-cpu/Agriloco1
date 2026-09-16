using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Agriloco.Api.Models;
// Separate from all draft/published feature tables. A basemap revision owns one calibration.
public sealed class FarmGeoreference
{
    [Key, ForeignKey(nameof(Farm))] public int FarmId { get; set; }
    public Farm Farm { get; set; } = null!;
    public string? MapImageUrl { get; set; }
    public DateTime? MapImageUploadedAt { get; set; }
    public string PointsJson { get; set; } = "[]";
    [ConcurrencyCheck] public Guid Revision { get; set; }
    public DateTime UpdatedAt { get; set; }
}
