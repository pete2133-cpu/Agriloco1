using System.ComponentModel.DataAnnotations;

namespace Agriloco.Api.Dtos
{
    // =========================
    // FARM CREATION (MINIMAL)
    // =========================
    public class FarmMinimalIn
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string ContactMethod1 { get; set; } = string.Empty;

        [Required]
        public string FruitCategory1 { get; set; } = string.Empty;
    }

    // =========================
    // FARM CREATION OUTPUT
    // =========================
    public class FarmMinimalOut
    {
        public int Id { get; set; }

        public string AgrilocoId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string RegionCode { get; set; } = string.Empty;

        public string FruitCategory1 { get; set; } = string.Empty;

        public string ContactMethod1 { get; set; } = string.Empty;
    }

    // =========================
    // FARM PUBLIC OUTPUT (for dropdowns / public farm list)
    // =========================
    public class PublicFarmOut
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string RegionCode { get; set; } = string.Empty;

        public string FruitCategory1 { get; set; } = string.Empty;

        public string ContactMethod1 { get; set; } = string.Empty;
    }
}