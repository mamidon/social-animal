using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SocialAnimal.Core.Domain;

namespace SocialAnimal.Web.Areas.Admin.Models.ViewModels;

public class EventListViewModel
{
    public PagedResult<EventListItem> Items { get; set; } = new();
    public EventListFilters Filters { get; set; } = new();
    public SortInfo CurrentSort { get; set; } = new("CreatedOn", "desc");
    public Dictionary<string, string?> CurrentFilters { get; set; } = new();
    public List<SelectListItem> StateOptions { get; set; } = new();
    public List<SelectListItem> CityOptions { get; set; } = new();
    public bool HasFilters => CurrentFilters.Any();
}

public class EventListItem
{
    public long Id { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public required string Location { get; set; }
    public required string FullAddress { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedOn { get; set; }
    public int InvitationCount { get; set; }
    public int AttendeeCount { get; set; }
    public string Status => IsDeleted ? "Deleted" : "Active";
}

public class EventListFilters
{
    public string? Search { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class EventListRequest
{
    public string? Search { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string SortBy { get; set; } = "CreatedOn";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; }
}

public class EventDetailsViewModel
{
    public required EventRecord Event { get; set; }
    public List<InvitationRecord> Invitations { get; set; } = new();
    public EventStatistics Statistics { get; set; } = new();
}

public class EventStatistics
{
    public int TotalInvitations { get; set; }
    public int SentInvitations { get; set; }
    public int TotalReservations { get; set; }
    public int ConfirmedAttendees { get; set; }
    public int RegretCount { get; set; }
    public int PendingResponses { get; set; }
    public double ResponseRate { get; set; }
    public double AttendanceRate => TotalReservations > 0 
        ? (double)ConfirmedAttendees / TotalReservations * 100 
        : 0;
}

public class EventFormViewModel
{
    public required EventFormModel Event { get; set; }
    public bool IsNew { get; set; }
    public List<SelectListItem> StateOptions { get; set; } = new();
}

public class EventFormModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Event title is required")]
    [StringLength(200, ErrorMessage = "Title must be less than 200 characters")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Address line 1 is required")]
    [StringLength(200, ErrorMessage = "Address line 1 must be less than 200 characters")]
    public string AddressLine1 { get; set; } = "";

    [StringLength(200, ErrorMessage = "Address line 2 must be less than 200 characters")]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(100, ErrorMessage = "City must be less than 100 characters")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "State is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "State must be a 2-letter code")]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "State must be a valid 2-letter state code")]
    public string State { get; set; } = "";

    [Required(ErrorMessage = "Postal code is required")]
    [StringLength(20, ErrorMessage = "Postal code must be less than 20 characters")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Please enter a valid US postal code")]
    public string Postal { get; set; } = "";
}