using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace RaceDayMvc.Models;

// ---------- Auth ----------
public class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public class RegisterVm
{
    [Required, Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [Display(Name = "Date of birth"), DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
    [Display(Name = "Organisation name")] public string? OrganisationName { get; set; }
}
public class LoginResponse { public string Token { get; set; } = ""; public string Role { get; set; } = ""; public int UserId { get; set; } }

// ---------- Events / Categories ----------
public class EventDto
{
    [JsonPropertyName("eventId")] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    [Required] public string? Description { get; set; }
    [JsonPropertyName("eventDate"), Required, DataType(DataType.DateTime), Display(Name = "Date and time")] public DateTime Date { get; set; } = DateTime.Today.AddDays(30);
    [Required] public string Location { get; set; } = "";
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km."), Display(Name = "Distance (km)")] public decimal DistanceKm { get; set; }
    [Required, Display(Name = "Event type")] public string EventType { get; set; } = "Running";
    public string? BannerUrl { get; set; }
    public int OrganiserId { get; set; }
    [JsonIgnore] public int EnrolmentCount { get; set; }
}
public class CategoryDto
{
    [JsonPropertyName("categoryId")] public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
}
public class EventDetailsVm { public EventDto Event { get; set; } = new(); public List<CategoryDto> Categories { get; set; } = new(); }

// ---------- Enrolments (API entity shape -> flat view model) ----------
public class ApiEvent { public int EventId { get; set; } public string Name { get; set; } = ""; public DateTime EventDate { get; set; } }
public class ApiCategory { public int CategoryId { get; set; } public int EventId { get; set; } public string Name { get; set; } = ""; public ApiEvent? Event { get; set; } }
public class ApiPerson { public string FullName { get; set; } = ""; }
public class ApiEnrolment
{
    public int EnrolmentId { get; set; }
    public string Status { get; set; } = "";
    public ApiPerson? Participant { get; set; }
    public ApiCategory? Category { get; set; }
}
public class EnrolmentDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string ParticipantName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string Status { get; set; } = "";
    public static EnrolmentDto From(ApiEnrolment e) => new()
    {
        Id = e.EnrolmentId, Status = e.Status,
        ParticipantName = e.Participant?.FullName ?? "",
        CategoryName = e.Category?.Name ?? "",
        EventId = e.Category?.Event?.EventId ?? e.Category?.EventId ?? 0,
        EventName = e.Category?.Event?.Name ?? "",
        EventDate = e.Category?.Event?.EventDate ?? default
    };
}

// ---------- Results ----------
public class ApiEnrolmentRef { public ApiPerson? Participant { get; set; } public ApiCategory? Category { get; set; } }
public class ApiResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "";
    public ApiEnrolmentRef? Enrolment { get; set; }
}
public class ResultDto
{
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public string FinishTime { get; set; } = "";
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
    public string Status { get; set; } = "";
}
public class ResultInput
{
    public int EnrolmentId { get; set; }
    public int ResultId { get; set; }
    public string? ParticipantName { get; set; }
    public string? CategoryName { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}
public class ResultsEntryVm { public int EventId { get; set; } public string EventName { get; set; } = ""; public List<ResultInput> Rows { get; set; } = new(); }

// ---------- Profile ----------
public class ProfileDto
{
    [Required, Display(Name = "Full name")] public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    [Display(Name = "Date of birth"), DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
    public string? OrganisationName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
