using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Filters;
using RaceDayMvc.Models;
using RaceDayMvc.Services;
namespace RaceDayMvc.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;
    public HomeController(ApiClient api) { _api = api; }

    public async Task<IActionResult> Index(string? search, string? type, string? location)
    {
        if (HttpContext.Session.GetString("Role") == "Organiser") return RedirectToAction("Dashboard", "Organiser");
        var res = await _api.Get<List<EventDto>>("api/events");
        if (!res.Ok) TempData["Error"] = res.Error;
        var events = (res.Data ?? new()).Where(e => e.Date >= DateTime.Today).OrderBy(e => e.Date).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search)) events = events.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type)) events = events.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(location)) events = events.Where(e => e.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
        ViewBag.Search = search; ViewBag.Type = type; ViewBag.Location = location;
        return View(events.ToList());
    }

    [RoleAuthorize]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.Get<EventDto>($"api/events/{id}");
        if (!ev.Ok || ev.Data == null) { TempData["Error"] = ev.Error ?? "Event not found."; return RedirectToAction(nameof(Index)); }
        var cats = await _api.Get<List<CategoryDto>>("api/categories");
        return View(new EventDetailsVm { Event = ev.Data, Categories = (cats.Data ?? new()).Where(c => c.EventId == id).ToList() });
    }

    [HttpPost, ValidateAntiForgeryToken, RoleAuthorize("Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId == 0) { TempData["Error"] = "Please select a category."; return RedirectToAction(nameof(Details), new { id = eventId }); }
        var res = await _api.Post<object>($"api/categories/{categoryId}/enrol", null);
        if (!res.Ok) { TempData["Error"] = res.Error; return RedirectToAction(nameof(Details), new { id = eventId }); }
        TempData["Success"] = "You are enrolled! Check My Enrolments for your status.";
        return RedirectToAction("MyEnrolments", "Participant");
    }

    public IActionResult Error() => View();
}
