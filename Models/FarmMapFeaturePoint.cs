namespace Agriloco.Api.Models
{
    public class FarmMapFeaturePoint
    {
        public int Id { get; set; }

        public int FarmMapFeatureId { get; set; }

        public int PointOrder { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public FarmMapFeature? FarmMapFeature { get; set; }
    }
}