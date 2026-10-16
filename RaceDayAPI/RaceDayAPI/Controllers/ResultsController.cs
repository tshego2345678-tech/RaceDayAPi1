using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Validation;

namespace RaceDayApi.Controllers
{
    // DTO for creating a result
    public class ResultCreateDto
    {
        public int EnrolmentId { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // DTO for updating a result
    public class ResultUpdateDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/results
        [Authorize(Roles = "Organiser")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Result>>> GetResults()
        {
            if (!TryGetUserId(out int organiserId))
                return Unauthorized("Invalid token claims.");

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .Where(r =>
                    r.Enrolment != null &&
                    r.Enrolment.Category != null &&
                    r.Enrolment.Category.Event != null &&
                    r.Enrolment.Category.Event.OrganiserId == organiserId)
                .AsNoTracking()
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Result>> GetResult(int id)
        {
            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound();

            if (result.Enrolment?.Category?.Event == null)
                return NotFound("The related event could not be found.");

            if (User.IsInRole("Organiser"))
            {
                if (!TryGetUserId(out int organiserId))
                    return Unauthorized("Invalid token claims.");

                if (result.Enrolment.Category.Event.OrganiserId != organiserId)
                    return Forbid();

                return Ok(result);
            }

            if (User.IsInRole("Participant"))
            {
                if (!TryGetUserId(out int participantId))
                    return Unauthorized("Invalid token claims.");

                if (result.Enrolment.ParticipantId != participantId)
                    return Forbid();

                return Ok(result);
            }

            return Forbid();
        }

        // GET: api/results/participant/mine
        [Authorize(Roles = "Participant")]
        [HttpGet("participant/mine")]
        public async Task<ActionResult<IEnumerable<Result>>> GetMyResults()
        {
            if (!TryGetUserId(out int participantId))
                return Unauthorized("Invalid token claims.");

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .Where(r =>
                    r.Enrolment != null &&
                    r.Enrolment.ParticipantId == participantId)
                .AsNoTracking()
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/event/{eventId}
        [AllowAnonymous]
        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<Result>>> GetEventResults(
            int eventId)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == eventId);

            if (!eventExists)
                return NotFound("Event not found.");

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Participant)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                .Where(r =>
                    r.Enrolment != null &&
                    r.Enrolment.Category != null &&
                    r.Enrolment.Category.EventId == eventId)
                .AsNoTracking()
                .ToListAsync();

            return Ok(results);
        }

        // POST: api/results
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult<Result>> CreateResult(
            [FromBody] ResultCreateDto dto)
        {
            if (!TryGetUserId(out int organiserId))
                return Unauthorized("Invalid token claims.");

            if (!ResultStatusValidator.IsValidStatus(dto.Status))
                return BadRequest("Status must be Finished, DNF, or DSQ.");

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            if (enrolment.Category?.Event == null)
                return BadRequest("The enrolment has no valid event.");

            if (enrolment.Category.Event.OrganiserId != organiserId)
                return Forbid();

            var result = new Result
            {
                EnrolmentId = dto.EnrolmentId,
                FinishTime = dto.FinishTime,
                Position = dto.Position,
                Status = dto.Status
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetResult),
                new { id = result.ResultId },
                result);
        }

        // PUT: api/results/{id}
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResult(
            int id, [FromBody] ResultUpdateDto resultDto)
        {
            if (id != resultDto.ResultId)
                return BadRequest("Result ID mismatch.");

            if (!TryGetUserId(out int organiserId))
                return Unauthorized("Invalid token claims.");

            if (!ResultStatusValidator.IsValidStatus(resultDto.Status))
                return BadRequest("Status must be Finished, DNF, or DSQ.");

            var existingResult = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (existingResult == null)
                return NotFound();

            if (existingResult.Enrolment?.Category?.Event == null)
                return BadRequest("The related event could not be found.");

            if (existingResult.Enrolment.Category.Event.OrganiserId != organiserId)
                return Forbid();

            var targetEnrolment = await _context.Enrolments
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EnrolmentId == resultDto.EnrolmentId);

            if (targetEnrolment == null)
                return BadRequest("The selected enrolment does not exist.");

            if (targetEnrolment.Category == null ||
                targetEnrolment.Category.EventId !=
                existingResult.Enrolment.Category.EventId)
            {
                return BadRequest(
                    "The result must remain associated with the same event.");
            }

            existingResult.EnrolmentId = resultDto.EnrolmentId;
            existingResult.FinishTime = resultDto.FinishTime;
            existingResult.Position = resultDto.Position;
            existingResult.Status = resultDto.Status;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/results/{id}
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            if (!TryGetUserId(out int organiserId))
                return Unauthorized("Invalid token claims.");

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound();

            if (result.Enrolment?.Category?.Event == null)
                return BadRequest("The related event could not be found.");

            if (result.Enrolment.Category.Event.OrganiserId != organiserId)
                return Forbid();

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TryGetUserId(out int userId)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out userId);
        }
    }
}