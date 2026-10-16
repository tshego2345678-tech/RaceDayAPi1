using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RaceDayApi.Data;
using RaceDayApi.Models;
namespace RaceDayApi.Controllers
{
    // DTO for creating a category
    public class CategoryCreateDto
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; }
    }
    // DTO for updating a category
    public class CategoryUpdateDto
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; }
    }
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;
        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }
        // Checks whether the logged-in organiser owns the specified event
        private async Task<bool> OwnsEvent(int eventId)
        {
            var organiserIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier);
            if (organiserIdClaim == null ||
                !int.TryParse(organiserIdClaim.Value, out int organiserId))
            {
                return false;
            }
            return await _context.Events.AnyAsync(e =>
                e.EventId == eventId &&
                e.OrganiserId == organiserId);
        }
        // GET: api/categories (Public access)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories
                .AsNoTracking()
                .ToListAsync();
        }
        // GET: api/categories/5 (Public access)
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }
            return category;
        }
        // POST: api/categories (Organiser only)
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(
            [FromBody] CategoryCreateDto dto)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == dto.EventId);
            if (!eventExists)
            {
                return NotFound(
                    $"Event with ID {dto.EventId} does not exist.");
            }
            if (!await OwnsEvent(dto.EventId))
            {
                return Forbid();
            }
            var category = new Category
            {
                EventId = dto.EventId,
                Name = dto.Name,
                DistanceKm = dto.DistanceKm,
                EntryFee = dto.EntryFee,
                MaxParticipants = dto.MaxParticipants
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.CategoryId },
                category);
        }
        // PUT: api/categories/5 (Organiser only)
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] CategoryUpdateDto dto)
        {
            var existingCategory = await _context.Categories
                .FindAsync(id);
            if (existingCategory == null)
            {
                return NotFound();
            }
            // Check ownership of the category's current event
            if (!await OwnsEvent(existingCategory.EventId))
            {
                return Forbid();
            }
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == dto.EventId);
            if (!eventExists)
            {
                return NotFound(
                    $"Event with ID {dto.EventId} does not exist.");
            }
            // Check ownership of the destination event too
            if (!await OwnsEvent(dto.EventId))
            {
                return Forbid();
            }
            existingCategory.EventId = dto.EventId;
            existingCategory.Name = dto.Name;
            existingCategory.DistanceKm = dto.DistanceKm;
            existingCategory.EntryFee = dto.EntryFee;
            existingCategory.MaxParticipants = dto.MaxParticipants;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        // DELETE: api/categories/5 (Organiser only)
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            if (!await OwnsEvent(category.EventId))
            {
                return Forbid();
            }
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}