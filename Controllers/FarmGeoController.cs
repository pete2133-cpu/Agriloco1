using System;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace agriloco.api.Controllers
{
    [ApiController]
    [Route("api/farms")]
    public class FarmGeoController : ControllerBase
    {
        private readonly AgrilocoContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public FarmGeoController(AgrilocoContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // POST api/farms/{id}/geocode-address
        [HttpPost("{id:int}/geocode-address")]
        public async Task<IActionResult> GeocodeAddress(int id)
        {
            var farm = await _db.Farms.FirstOrDefaultAsync(f => f.Id == id);
            if (farm == null)
                return NotFound(new { error = "Farm not found." });

            var address = (farm.Address ?? "").Trim();
            if (string.IsNullOrWhiteSpace(address))
                return BadRequest(new { error = "Farm address is empty. Save an address first, then geocode." });

            var apiKey = (_config["Google:GeocodingApiKey"] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(500, new { error = "Google key not configured. Add Google:GeocodingApiKey." });

            var client = _httpClientFactory.CreateClient();

            var url =
                "https://maps.googleapis.com/maps/api/geocode/json" +
                "?address=" + Uri.EscapeDataString(address) +
                "&key=" + Uri.EscapeDataString(apiKey);

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, new { error = "Geocoding request failed.", statusCode = (int)resp.StatusCode });

            var body = await resp.Content.ReadFromJsonAsync<GoogleGeocodeResponse>();
            if (body == null)
                return StatusCode(500, new { error = "Could not parse Google response." });

            if (!string.Equals(body.Status, "OK", StringComparison.OrdinalIgnoreCase) ||
                body.Results == null || body.Results.Length == 0 ||
                body.Results[0].Geometry?.Location == null)
            {
                return BadRequest(new
                {
                    error = "No geocode results for this address.",
                    googleStatus = body.Status,
                    googleErrorMessage = body.ErrorMessage
                });
            }

            var loc = body.Results[0].Geometry!.Location!;
            farm.Latitude = loc.Lat;
            farm.Longitude = loc.Lng;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                farmId = farm.Id,
                latitude = farm.Latitude,
                longitude = farm.Longitude
            });
        }

        // ---- minimal Google response types ----
        public class GoogleGeocodeResponse
        {
            [JsonPropertyName("status")] public string? Status { get; set; }
            [JsonPropertyName("error_message")] public string? ErrorMessage { get; set; }
            [JsonPropertyName("results")] public GoogleResult[]? Results { get; set; }
        }

        public class GoogleResult
        {
            [JsonPropertyName("geometry")] public GoogleGeometry? Geometry { get; set; }
        }

        public class GoogleGeometry
        {
            [JsonPropertyName("location")] public GoogleLocation? Location { get; set; }
        }

        public class GoogleLocation
        {
            [JsonPropertyName("lat")] public double Lat { get; set; }
            [JsonPropertyName("lng")] public double Lng { get; set; }
        }
    }
}