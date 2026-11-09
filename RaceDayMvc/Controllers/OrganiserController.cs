using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Filters;
using RaceDayMvc.Models;
using RaceDayMvc.Services;
namespace RaceDayMvc.Controllers;

[RoleAuthorize("Organiser")]
public class OrganiserController : Controller
{
    private static readonly string[] Statuses = { "Finished", "DNF", "DSQ" };
    private readonly ApiClient _api;
    public OrganiserController(ApiClient api) { _api = api; }

    private int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    // The API's GET api/events is public, so the organiser's own events are filtered by OrganiserId.
    private async Task<List<EventDto>> MyEvents(bool withCounts = false)
    {
        var res = await _api.Get<List<EventDto>>("api/events");
        if (!res.Ok) TempData["Error"] = res.Error;
        var mine = (res.Data ?? new()).Where(e => e.OrganiserId == UserId).ToList();
        if (withCounts)
        {
            var counts = await Task.WhenAll(mine.Select(async e =>
                (await _api.Get<List<ApiEnrolment>>($"api/enrolments/event/{e.Id}")).Data?.Count ?? 0));
            for (int i = 0; i < mine.Count; i++) mine[i].EnrolmentCount = counts[i];
        }
        return mine;
    }

    public async Task<IActionResult> Dashboard() => View((await MyEvents(true)).OrderBy(e => e.Date).ToList());
    public async Task<IActionResult> Events() => View((await MyEvents()).OrderBy(e => e.Date).ToList());

    // ---------- Events ----------
    [HttpGet] public IActionResult CreateEvent() => View("EventForm", new EventDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventDto vm, IFormFile? banner)
    {
        if (!ModelState.IsValid) return View("EventForm", vm);
        var res = await _api.Post<EventDto>("api/events", Body(vm));
        if (!res.Ok || res.Data == null) { ModelState.AddModelError("", res.Error ?? "Could not create event."); return View("EventForm", vm); }
        await UploadBanner(res.Data.Id, banner);
        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var res = await _api.Get<EventDto>($"api/events/{id}");
        if (!res.Ok || res.Data == null) { TempData["Error"] = res.Error; return RedirectToAction(nameof(Events)); }
        return View("EventForm", res.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventDto vm, IFormFile? banner)
    {
        if (!ModelState.IsValid) return View("EventForm", vm);
        var res = await _api.Put<object>($"api/events/{id}", Body(vm));
        if (!res.Ok) { ModelState.AddModelError("", res.Error ?? "Could not update event."); return View("EventForm", vm); }
        await UploadBanner(id, banner);
        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var res = await _api.Delete($"api/events/{id}");
        if (res.Ok) TempData["Success"] = "Event deleted."; else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Events));
    }

    private static object Body(EventDto e) => new { e.Name, e.Description, EventDate = e.Date, e.Location, e.DistanceKm, e.EventType };

    private async Task UploadBanner(int eventId, IFormFile? banner)
    {
        if (banner is not { Length: > 0 }) return;
        if (!banner.ContentType.StartsWith("image/") || banner.Length > 5_000_000)
        { TempData["Error"] = "Banner must be an image under 5 MB (event saved without it)."; return; }
        var up = await _api.Upload<object>($"api/events/{eventId}/banner", banner);
        if (!up.Ok) TempData["Error"] = up.Error;
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories(int id)
    {
        var ev = await _api.Get<EventDto>($"api/events/{id}");
        if (!ev.Ok || ev.Data == null) { TempData["Error"] = ev.Error; return RedirectToAction(nameof(Events)); }
        var cats = await _api.Get<List<CategoryDto>>("api/categories");
        return View(new EventDetailsVm { Event = ev.Data, Categories = (cats.Data ?? new()).Where(c => c.EventId == id).ToList() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int eventId, string name, decimal distanceKm, decimal entryFee, int maxParticipants)
    {
        if (string.IsNullOrWhiteSpace(name) || distanceKm <= 0 || entryFee < 0 || maxParticipants <= 0)
        { TempData["Error"] = "Enter a name, a distance above 0, a fee of 0 or more, and a participant limit above 0."; return RedirectToAction(nameof(Categories), new { id = eventId }); }
        var res = await _api.Post<object>("api/categories", new { EventId = eventId, Name = name.Trim(), DistanceKm = distanceKm, EntryFee = entryFee, MaxParticipants = maxParticipants });
        if (res.Ok) TempData["Success"] = "Category added."; else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var res = await _api.Delete($"api/categories/{categoryId}");
        if (res.Ok) TempData["Success"] = "Category removed."; else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int id)
    {
        var ev = await _api.Get<EventDto>($"api/events/{id}");
        var res = await _api.Get<List<ApiEnrolment>>($"api/enrolments/event/{id}");
        if (!res.Ok) TempData["Error"] = res.Error;
        ViewBag.EventName = ev.Data?.Name ?? "Event"; ViewBag.EventId = id;
        return View((res.Data ?? new()).Select(EnrolmentDto.From).ToList());
    }

    // ---------- Results ----------
    [HttpGet]
    public async Task<IActionResult> Results(int id)
    {
        var ev = await _api.Get<EventDto>($"api/events/{id}");
        if (!ev.Ok || ev.Data == null) { TempData["Error"] = ev.Error; return RedirectToAction(nameof(Events)); }
        var enr = await _api.Get<List<ApiEnrolment>>($"api/enrolments/event/{id}");
        var existing = (await _api.Get<List<ApiResult>>($"api/results/event/{id}")).Data ?? new();
        if (ev.Data.Date > DateTime.Now) TempData["Error"] = "Results can only be captured after the event has concluded.";

        var vm = new ResultsEntryVm
        {
            EventId = id, EventName = ev.Data.Name,
            Rows = (enr.Data ?? new()).Select(e =>
            {
                var r = existing.FirstOrDefault(x => x.EnrolmentId == e.EnrolmentId);
                return new ResultInput
                {
                    EnrolmentId = e.EnrolmentId, ResultId = r?.ResultId ?? 0,
                    ParticipantName = e.Participant?.FullName, CategoryName = e.Category?.Name,
                    FinishTime = r?.FinishTime, Position = r?.Position, Status = r?.Status ?? "Finished"
                };
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Results(ResultsEntryVm vm)
    {
        var ev = await _api.Get<EventDto>($"api/events/{vm.EventId}");
        if (ev.Data != null && ev.Data.Date > DateTime.Now) { TempData["Error"] = "Results can only be captured after the event has concluded."; return View(vm); }

        // Rows the organiser filled in: finished rows need time + position; DNF/DSQ rows need neither
        var rows = vm.Rows.Where(r => r.Status != "Finished" || !string.IsNullOrWhiteSpace(r.FinishTime) || r.Position.HasValue).ToList();
        if (rows.Count == 0) { TempData["Error"] = "Enter at least one result."; return View(vm); }
        foreach (var r in rows)
        {
            if (!Statuses.Contains(r.Status)) { TempData["Error"] = "Invalid status selected."; return View(vm); }
            if (r.Status == "Finished")
            {
                if (r.Position is null or < 1 || !System.Text.RegularExpressions.Regex.IsMatch(r.FinishTime ?? "", @"^\d{1,2}:[0-5]\d:[0-5]\d$"))
                { TempData["Error"] = $"{r.ParticipantName}: a finished runner needs a position and a finish time in hh:mm:ss."; return View(vm); }
            }
        }
        var positions = rows.Where(r => r.Status == "Finished").Select(r => r.Position).ToList();
        if (positions.Distinct().Count() != positions.Count) { TempData["Error"] = "Finishing positions must be unique."; return View(vm); }

        foreach (var r in rows)
        {
            bool finished = r.Status == "Finished";
            string? time = finished ? r.FinishTime!.PadLeft(8, '0') : null;
            int? pos = finished ? r.Position : null;
            var res = r.ResultId > 0
                ? await _api.Put<object>($"api/results/{r.ResultId}", new { r.ResultId, r.EnrolmentId, FinishTime = time, Position = pos, r.Status })
                : await _api.Post<object>("api/results", new { r.EnrolmentId, FinishTime = time, Position = pos, r.Status });
            if (!res.Ok) { TempData["Error"] = $"{r.ParticipantName}: {res.Error}"; return View(vm); }
        }
        TempData["Success"] = "Results published.";
        return RedirectToAction(nameof(Enrolments), new { id = vm.EventId });
    }
}
