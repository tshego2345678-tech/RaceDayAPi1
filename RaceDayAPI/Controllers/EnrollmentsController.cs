using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace RaceDayApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;
        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }
        // POST: /api/categories/{categoryId}/enrol (Participant only)
        [Authorize(Roles = "Participant")]
        [HttpPost("/api/categories/{categoryId}/enrol")]
        public async Task<IActionResult> CreateEnrolment(int categoryId)
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out int participantId))
            {
                return Unauthorized("Invalid token claims.");
            }
            // Verify category exists
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound("Category not found.");
            }
            // Check if already enrolled
            var existingEnrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e =>
                    e.ParticipantId == participantId &&
                    e.CategoryId == categoryId);
            if (existingEnrolment != null)
            {
                return BadRequest("Already enrolled in this category.");
            }
            var enrolment = new Enrolment
            {
                ParticipantId = participantId,
                CategoryId = categoryId,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Confirmed"
            };
            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Successfully enrolled",
                enrolment.EnrolmentId,
                enrolment.CategoryId,
                enrolment.EnrolmentDate,
                enrolment.Status
            });
        }
        // GET: api/enrolments/mine (Participant only)
        [Authorize(Roles = "Participant")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out int participantId))
            {
                return Unauthorized("Invalid token claims.");
            }
            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == participantId)
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .AsNoTracking()
                .ToListAsync();
            return Ok(enrolments);
        }
        // GET: api/enrolments/event/{eventId}
        // Organiser only - must own the event
        [Authorize(Roles = "Organiser")]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out int organiserId))
            {
                return Unauthorized("Invalid token claims.");
            }
            var raceEvent = await _context.Events.FindAsync(eventId);
            if (raceEvent == null)
            {
                return NotFound("Event not found.");
            }
            // Ensure the organiser owns this event
            if (raceEvent.OrganiserId != organiserId)
            {
                return Forbid();
            }
            var enrolments = await _context.Enrolments
                .Where(e => e.Category.EventId == eventId)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .AsNoTracking()
                .ToListAsync();
            return Ok(enrolments);
        }
        // DELETE: api/enrolments/5
        // Participant only - must own the enrolment
        [Authorize(Roles = "Participant")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnrolment(int id)
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out int participantId))
            {
                return Unauthorized("Invalid token claims.");
            }
            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null)
            {
                return NotFound("Enrolment not found.");
            }
            // Ensure the participant owns this enrolment
            if (enrolment.ParticipantId != participantId)
            {
                return Forbid();
            }
            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}