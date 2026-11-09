using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Filters;
using RaceDayMvc.Models;
using RaceDayMvc.Services;
namespace RaceDayMvc.Controllers;

[RoleAuthorize("Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;
    public ParticipantController(ApiClient api) { _api = api; }

    public async Task<IActionResult> MyEnrolments()
    {
        var res = await _api.Get<List<ApiEnrolment>>("api/enrolments/mine");
        if (!res.Ok) TempData["Error"] = res.Error;
        return View((res.Data ?? new()).Select(EnrolmentDto.From).OrderBy(e => e.EventDate).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var res = await _api.Delete($"api/enrolments/{id}");
        if (res.Ok) TempData["Success"] = "Enrolment cancelled."; else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(MyEnrolments));
    }

    public async Task<IActionResult> MyResults()
    {
        var mine = await _api.Get<List<ApiResult>>("api/results/participant/mine");
        if (!mine.Ok) { TempData["Error"] = mine.Error; return View(new List<ResultDto>()); }
        var list = mine.Data ?? new();

        // Count finishers per event (from the public event-results endpoint) for "47th out of 312"
        var totals = new Dictionary<int, int>();
        foreach (var eid in list.Select(r => r.Enrolment?.Category?.EventId ?? 0).Where(i => i > 0).Distinct())
        {
            var all = await _api.Get<List<ApiResult>>($"api/results/event/{eid}");
            totals[eid] = (all.Data ?? new()).Count(r => r.Status == "Finished");
        }

        var rows = list.Select(r =>
        {
            var cat = r.Enrolment?.Category;
            return new ResultDto
            {
                EventName = cat?.Event?.Name ?? "",
                EventDate = cat?.Event?.EventDate ?? default,
                CategoryName = cat?.Name ?? "",
                FinishTime = r.FinishTime ?? "",
                Position = r.Position,
                Status = r.Status,
                TotalFinishers = totals.GetValueOrDefault(cat?.EventId ?? 0)
            };
        }).OrderByDescending(r => r.EventDate).ToList();
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var res = await _api.Get<ProfileDto>("api/profile");
        if (!res.Ok) TempData["Error"] = res.Error;
        return View(res.Data ?? new ProfileDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileDto vm, IFormFile? picture)
    {
        if (!ModelState.IsValid) return View(vm);
        if (picture is { Length: > 0 })
        {
            if (!picture.ContentType.StartsWith("image/") || picture.Length > 5_000_000)
            { ModelState.AddModelError("", "Profile picture must be an image under 5 MB."); return View(vm); }
            var up = await _api.Upload<object>("api/profile/picture", picture);
            if (!up.Ok) { ModelState.AddModelError("", up.Error ?? "Upload failed."); return View(vm); }
        }
        var res = await _api.Put<object>("api/profile", new { vm.FullName, vm.DateOfBirth });
        if (!res.Ok) { ModelState.AddModelError("", res.Error ?? "Update failed."); return View(vm); }
        HttpContext.Session.SetString("Name", vm.FullName);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }
}
