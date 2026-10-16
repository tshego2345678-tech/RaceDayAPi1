using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace RaceDayApi.Controllers
{
    // DTO for creating an event
    public class EventCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
    }
    // DTO for updating an event
    public class EventUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
    }
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;
        public EventsController(RaceDayContext context)
        {
            _context = context;
        }
        // GET: api/events (Public access)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Events>>> GetEvents()
        {
            return await _context.Events
                .AsNoTracking()
                .ToListAsync();
        }
        // GET: api/events/5 (Public access)
        [HttpGet("{id}")]
        public async Task<ActionResult<Events>> GetEvent(int id)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);
            if (raceEvent == null)
            {
                return NotFound();
            }
            return raceEvent;
        }
        // POST: api/events (Organiser only)
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult<Events>> CreateEvent(
            [FromBody] EventCreateDto dto)
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out int organiserId))
            {
                return Unauthorized("Invalid token claims.");
            }
            var raceEvent = new Events
            {
                OrganiserId = organiserId,
                Name = dto.Name,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                CreatedAt = DateTime.UtcNow
            };
            _context.Events.Add(raceEvent);
            await _context.SaveChangesAsync();
            return CreatedAtAction(
                nameof(GetEvent),
                new { id = raceEvent.EventId },
                raceEvent);
        }
        // PUT: api/events/5 (Organiser only - must own the event)
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] EventUpdateDto dto)
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out int organiserId))
            {
                return Unauthorized("Invalid token claims.");
            }
            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
            {
                return NotFound();
            }
            // Only the organiser who owns the event can update it
            if (existingEvent.OrganiserId != organiserId)
            {
                return Forbid();
            }
            existingEvent.Name = dto.Name;
            existingEvent.Description = dto.Description;
            existingEvent.EventDate = dto.EventDate;
            existingEvent.Location = dto.Location;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        // DELETE: api/events/5 (Organiser only - must own the event)
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out int organiserId))
            {
                return Unauthorized("Invalid token claims.");
            }
            var raceEvent = await _context.Events.FindAsync(id);
            if (raceEvent == null)
            {
                return NotFound();
            }
            // Only the organiser who owns the event can delete it
            if (raceEvent.OrganiserId != organiserId)
            {
                return Forbid();
            }
            _context.Events.Remove(raceEvent);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}