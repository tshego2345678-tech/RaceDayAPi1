using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;
using RaceDayMvc.Services;
namespace RaceDayMvc.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;
    public AccountController(ApiClient api) { _api = api; }

    [HttpGet] public IActionResult Login() => View(new LoginVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var res = await _api.Post<LoginResponse>("api/auth/login", new { vm.Email, vm.Password });
        if (!res.Ok || res.Data == null) { ModelState.AddModelError("", res.Error ?? "Login failed."); return View(vm); }

        HttpContext.Session.SetString("Token", res.Data.Token);
        HttpContext.Session.SetString("Role", res.Data.Role);
        HttpContext.Session.SetInt32("UserId", res.Data.UserId);

        // The login response has no name, so fetch it from the profile endpoint (token is now in session)
        var profile = await _api.Get<ProfileDto>("api/profile");
        HttpContext.Session.SetString("Name", profile.Data?.FullName ?? vm.Email);

        return res.Data.Role == "Organiser" ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");
    }

    [HttpGet] public IActionResult Register() => View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (vm.Role == "Organiser" && string.IsNullOrWhiteSpace(vm.OrganisationName))
            ModelState.AddModelError(nameof(vm.OrganisationName), "Organisation name is required for organisers.");
        if (!ModelState.IsValid) return View(vm);

        ApiResult<object> res = vm.Role == "Organiser"
            ? await _api.Post<object>("api/auth/register/organiser", new { vm.FullName, vm.Email, vm.Password, vm.OrganisationName })
            : await _api.Post<object>("api/auth/register/participant", new { vm.FullName, vm.Email, vm.Password, vm.DateOfBirth });

        if (!res.Ok) { ModelState.AddModelError("", res.Error ?? "Registration failed."); return View(vm); }
        TempData["Success"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }
}
