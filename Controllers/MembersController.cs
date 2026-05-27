using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Agriloco.Api.Data;
using Agriloco.Api.Dtos;
using Agriloco.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/member")]
    public class MembersController : ControllerBase
    {
        private readonly AgrilocoContext _context;

        public MembersController(AgrilocoContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<MemberPublicOut>> Register(MemberRegisterIn input)
        {
            if (string.IsNullOrWhiteSpace(input.FarmName) ||
                string.IsNullOrWhiteSpace(input.Address) ||
                string.IsNullOrWhiteSpace(input.Email) ||
                string.IsNullOrWhiteSpace(input.Username) ||
                string.IsNullOrWhiteSpace(input.Password))
            {
                return BadRequest("FarmName, Address, Email, Username, and Password are required.");
            }

            var usernameTaken = await _context.Members.AnyAsync(m => m.Username == input.Username);
            if (usernameTaken)
                return BadRequest("Username is already taken.");

            var emailTaken = await _context.Members.AnyAsync(m => m.Email == input.Email);
            if (emailTaken)
                return BadRequest("Email is already in use.");

            var farm = new Farm
            {
                Name = input.FarmName.Trim(),
                Address = input.Address.Trim(),
                RegionCode = "CA-ON",
                ContactMethod1 = input.Email.Trim(),
                FruitCategory1 = "Mixed",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,

                // NEW: optional geo fields at signup
                Latitude = input.Latitude,
                Longitude = input.Longitude
            };

            _context.Farms.Add(farm);
            await _context.SaveChangesAsync();

            var member = new Member
            {
                FarmId = farm.Id,
                Email = input.Email.Trim(),
                AltEmail = string.IsNullOrWhiteSpace(input.AltEmail) ? null : input.AltEmail.Trim(),
                Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
                Username = input.Username.Trim(),

                PasswordHash = Encoding.UTF8.GetBytes(input.Password),

                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            var output = new MemberPublicOut
            {
                Id = member.Id,
                MemberId = member.Id,

                FarmId = farm.Id,
                FarmName = farm.Name,

                Email = member.Email,
                AltEmail = member.AltEmail,
                Phone = member.Phone,

                Username = member.Username,
                CreatedAt = member.CreatedAt
            };

            return CreatedAtAction(nameof(GetMemberById), new { id = member.Id }, output);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MemberPublicOut>> GetMemberById(int id)
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == id);
            if (member == null)
                return NotFound();

            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == member.FarmId);
            if (farm == null)
                return NotFound();

            var output = new MemberPublicOut
            {
                Id = member.Id,
                MemberId = member.Id,

                FarmId = farm.Id,
                FarmName = farm.Name,

                Name = farm.Name,
                RegionCode = farm.RegionCode ?? "",

                Email = member.Email,
                AltEmail = member.AltEmail,
                Phone = member.Phone,
                Username = member.Username,
                CreatedAt = member.CreatedAt
            };

            return Ok(output);
        }

        [HttpGet("byusername/{username}")]
        public async Task<ActionResult<MemberPublicOut>> GetMemberByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Username is required.");

            var member = await _context.Members.FirstOrDefaultAsync(m => m.Username == username);
            if (member == null)
                return NotFound();

            var farm = await _context.Farms.FirstOrDefaultAsync(f => f.Id == member.FarmId);
            if (farm == null)
                return NotFound();

            var output = new MemberPublicOut
            {
                Id = member.Id,
                MemberId = member.Id,

                FarmId = farm.Id,
                FarmName = farm.Name,

                Name = farm.Name,
                RegionCode = farm.RegionCode ?? "",

                Email = member.Email,
                AltEmail = member.AltEmail,
                Phone = member.Phone,
                Username = member.Username,
                CreatedAt = member.CreatedAt
            };

            return Ok(output);
        }
    }
}