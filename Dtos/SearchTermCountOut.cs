namespace Agriloco.Api.Dtos
{
    public class SearchTermCountOut
    {
        public string RegionCode { get; set; } = "";
        public string Term { get; set; } = "";
        public int Count { get; set; }
    }
}