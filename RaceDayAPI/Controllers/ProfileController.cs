using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using System.Security.Claims;
namespace RaceDayApi.Controllers
{
    // DTO for updating profile details
    public class ProfileUpdateDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? OrganisationName { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayContext _context;
        public ProfileController(RaceDayContext context)
        {
            _context = context;
        }
        // Gets the profile of the logged-in user
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(
                ClaimTypes.Role)?.Value;
            if (!int.TryParse(userIdClaim, out int userId) ||
                string.IsNullOrEmpty(roleClaim))
            {
                return Unauthorized("Invalid token claims.");
            }
            if (roleClaim == "Organiser")
            {
                var organiser = await _context.Organisers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o =>
                        o.OrganiserId == userId);
                if (organiser == null)
                {
                    return NotFound("Organiser profile not found.");
                }
                // Do not expose the password hash
                return Ok(new
                {
                    organiser.OrganiserId,
                    organiser.FullName,
                    organiser.Email,
                    organiser.OrganisationName,
                    organiser.CreatedAt
                });
            }
            if (roleClaim == "Participant")
            {
                var participant = await _context.Participants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.ParticipantId == userId);
                if (participant == null)
                {
                    return NotFound("Participant profile not found.");
                }
                // Do not expose the password hash
                return Ok(new
                {
                    participant.ParticipantId,
                    participant.FullName,
                    participant.Email,
                    participant.DateOfBirth,
                    participant.CreatedAt
                });
            }
            return Forbid();
        }
        // Updates the profile of the logged-in user
        [HttpPut]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] ProfileUpdateDto dto)
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(
                ClaimTypes.Role)?.Value;
            if (!int.TryParse(userIdClaim, out int userId) ||
                string.IsNullOrEmpty(roleClaim))
            {
                return Unauthorized("Invalid token claims.");
            }
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest("Full name is required.");
            }
            if (roleClaim == "Organiser")
            {
                var organiser = await _context.Organisers
                    .FindAsync(userId);
                if (organiser == null)
                {
                    return NotFound("Organiser profile not found.");
                }
                organiser.FullName = dto.FullName.Trim();
                if (dto.OrganisationName != null)
                {
                    organiser.OrganisationName =
                        dto.OrganisationName.Trim();
                }
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "Profile updated successfully",
                    organiser.FullName,
                    organiser.OrganisationName
                });
            }
            if (roleClaim == "Participant")
            {
                var participant = await _context.Participants
                    .FindAsync(userId);
                if (participant == null)
                {
                    return NotFound("Participant profile not found.");
                }
                participant.FullName = dto.FullName.Trim();
                if (dto.DateOfBirth.HasValue)
                {
                    participant.DateOfBirth = dto.DateOfBirth;
                }
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "Profile updated successfully",
                    participant.FullName,
                    participant.DateOfBirth
                });
            }
            return Forbid();
        }
    }
}