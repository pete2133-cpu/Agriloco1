using System.Collections.Generic;

namespace Agriloco.Api.Models
{
    public class FarmMapFeature
    {
        public int Id { get; set; }

        public int FarmMapId { get; set; }

        public int FarmDefinitionId { get; set; }

        public string Purpose { get; set; }
            = "FarmDefinition";

        public string GeometryType { get; set; }
            = "Line";

        public string Subtype { get; set; }
            = "";

        public string DisplayPath { get; set; }
            = "";

        public double LineWidth { get; set; }
            = 6;

        public double Opacity { get; set; }
            = 1;

        public bool ShowLabel { get; set; }
            = true;

        public int LabelSourceFarmDefinitionId { get; set; }

        public string CustomLabel { get; set; }
            = "";

        public string LabelPosition { get; set; }
            = "Center";

        public double LabelFontSize { get; set; }
            = 18;

        public string LabelTextColour { get; set; }
            = "White";

        public FarmMap? FarmMap { get; set; }

        public List<FarmMapFeaturePoint> Points { get; set; }
            = new List<FarmMapFeaturePoint>();
    }
}