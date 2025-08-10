using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SocialAnimal.Core.Domain;

namespace SocialAnimal.Web.Areas.Admin.Models.ViewModels;

public class ReservationListViewModel
{
    public PagedResult<ReservationListItem> Items { get; set; } = new();
    public ReservationListFilters Filters { get; set; } = new();
    public SortInfo CurrentSort { get; set; } = new("CreatedOn", "desc");
    public Dictionary<string, string?> CurrentFilters { get; set; } = new();
    public List<SelectListItem> EventOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();
    public bool HasFilters => CurrentFilters.Any();
}

public class ReservationListItem
{
    public long Id { get; set; }
    public long InvitationId { get; set; }
    public long UserId { get; set; }
    public uint PartySize { get; set; }
    public bool IsAttending { get; set; }
    public DateTime CreatedOn { get; set; }
    
    // Extended fields from related entities
    public required string GuestName { get; set; }
    public required string EventTitle { get; set; }
    public required string UserName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    
    public string Status => IsAttending ? "Attending" : "Regrets";
    public string ContactInfo => !string.IsNullOrEmpty(Email) ? Email : Phone ?? "No contact";
}

public class ReservationListFilters
{
    public string? Search { get; set; }
    public long? EventId { get; set; }
    public long? UserId { get; set; }
    public bool? IsAttending { get; set; }
}

public class ReservationListRequest
{
    public string? Search { get; set; }
    public long? EventId { get; set; }
    public long? UserId { get; set; }
    public bool? IsAttending { get; set; }
    public string SortBy { get; set; } = "CreatedOn";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ReservationDetailsViewModel
{
    public required ReservationRecord Reservation { get; set; }
    public InvitationRecord? Invitation { get; set; }
    public UserRecord? User { get; set; }
    public EventRecord? Event { get; set; }
    public ReservationStatistics Statistics { get; set; } = new();
    
    // Extended fields (would come from enhanced reservation record in real implementation)
    public required string GuestName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Notes { get; set; } = "";
    public string DietaryRestrictions { get; set; } = "";
    
    public string ContactInfo => !string.IsNullOrEmpty(Email) ? Email : Phone ?? "No contact";
    public bool HasContact => !string.IsNullOrEmpty(Email) || !string.IsNullOrEmpty(Phone);
    public bool IsAttending => Reservation.PartySize > 0;
    public bool HasDeclined => Reservation.PartySize == 0;
}

public class ReservationStatistics
{
    public TimeSpan ResponseTime { get; set; }
    public bool IsLateResponse { get; set; }
    public int ModificationCount { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public bool HasBeenModified => ModificationCount > 0;
}

public class ReservationFormViewModel
{
    public required ReservationFormModel Reservation { get; set; }
    public bool IsNew { get; set; }
    public List<SelectListItem> InvitationOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();
}

public class ReservationFormModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Invitation is required")]
    [Range(1, long.MaxValue, ErrorMessage = "Please select a valid invitation")]
    public long InvitationId { get; set; }

    [Required(ErrorMessage = "User is required")]
    [Range(1, long.MaxValue, ErrorMessage = "Please select a valid user")]
    public long UserId { get; set; }

    [Required(ErrorMessage = "Party size is required")]
    [Range(0, 20, ErrorMessage = "Party size must be between 0 and 20")]
    public uint PartySize { get; set; } = 1;

    public string Notes { get; set; } = "";
    
    public string DietaryRestrictions { get; set; } = "";
    
    public bool IsAttending => PartySize > 0;
    public bool HasDeclined => PartySize == 0;
}

// Additional enums for reservation status
public enum ReservationStatus
{
    Pending,
    Attending,
    Regrets,
    Attended,
    NoShow
}