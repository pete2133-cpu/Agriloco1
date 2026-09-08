using System;
using System.Collections.Generic;

namespace Agriloco.Api.Models
{
    public class FarmMap
    {
        public int Id { get; set; }

        public int FarmId { get; set; }

        // Draft or Published
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }

        public List<FarmMapFeature> Features { get; set; }
            = new List<FarmMapFeature>();
    }
}