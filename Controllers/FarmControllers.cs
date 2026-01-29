using System;
using System.Linq;
using System.Threading.Tasks;
using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FarmsController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public FarmsController(AgrilocoContext context)
        {
            _context = context;
        }

        // POST: api/Farms/register_minimal
        [HttpPost("register_minimal")]
        public async Task<ActionResult<FarmMinimalOut>> RegisterMinimal([FromBody] FarmMinimalIn input)
        {
            if (string.IsNullOrWhiteSpace(input.Name) ||
                string.IsNullOrWhiteSpace(input.Address) ||
                string.IsNullOrWhiteSpace(input.ContactMethod1) ||
                string.IsNullOrWhiteSpace(input.FruitCategory1))
            {
                return BadRequest("Name, address, contact method, and fruit category are required.");
            }

            // TODO: Replace this with real geocoding later
            var regionCode = "CA-ON";

            // Generate a simple Agriloco ID
            var agrilocoId = "AGR-" + Guid.NewGuid().ToString("N")[..8].ToUpper();

            var farm = new Farm
            {
                AgrilocoId = agrilocoId,
                Name = input.Name,
                Address = input.Address,
                RegionCode = regionCode,
                ContactMethod1 = input.ContactMethod1,
                FruitCategory1 = input.FruitCategory1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Farms.Add(farm);
            await _context.SaveChangesAsync();

            var result = new FarmMinimalOut
            {
                Id = farm.Id,
                AgrilocoId = farm.AgrilocoId,
                Name = farm.Name,
                RegionCode = farm.RegionCode,
                FruitCategory1 = farm.FruitCategory1,
                ContactMethod1 = farm.ContactMethod1
            };

            return CreatedAtAction(nameof(GetPublicFarm), new { id = farm.Id }, result);
        }

        // GET: api/Farms/public
        [HttpGet("public")]
        public async Task<ActionResult<PublicFarmOut[]>> GetPublicFarms()
        {
            var farms = await _context.Farms
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .Select(f => new PublicFarmOut
                {
                    Id = f.Id,
                    Name = f.Name,
                    RegionCode = f.RegionCode,
                    FruitCategory1 = f.FruitCategory1,
                    ContactMethod1 = f.ContactMethod1,
                    Address = f.Address
                })
                .ToArrayAsync();

            return farms;
        }

        // GET: api/Farms/{id}/public_single
        [HttpGet("{id}/public_single")]
        public async Task<ActionResult<PublicFarmOut>> GetPublicFarm(int id)
        {
            var farm = await _context.Farms.FindAsync(id);
            if (farm == null || !farm.IsActive)
                return NotFound();

            var result = new PublicFarmOut
            {
                Id = farm.Id,
                Name = farm.Name,
                RegionCode = farm.RegionCode,
                FruitCategory1 = farm.FruitCategory1,
                ContactMethod1 = farm.ContactMethod1,
                Address = farm.Address
            };

            return result;
        }
    }
}