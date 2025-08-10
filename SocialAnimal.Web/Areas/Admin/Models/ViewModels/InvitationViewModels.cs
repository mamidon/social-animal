using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SocialAnimal.Core.Domain;

namespace SocialAnimal.Web.Areas.Admin.Models.ViewModels;

public class InvitationListViewModel
{
    public PagedResult<InvitationListItem> Items { get; set; } = new();
    public InvitationListFilters Filters { get; set; } = new();
    public SortInfo CurrentSort { get; set; } = new("CreatedOn", "desc");
    public Dictionary<string, string?> CurrentFilters { get; set; } = new();
    public List<SelectListItem> EventOptions { get; set; } = new();
    public bool HasFilters => CurrentFilters.Any();
}

public class InvitationListItem
{
    public long Id { get; set; }
    public required string Slug { get; set; }
    public long EventId { get; set; }
    public required string EventTitle { get; set; }
    public required string GuestName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int MaxPartySize { get; set; }
    public bool IsSent { get; set; }
    public bool HasResponse { get; set; }
    public string? ResponseStatus { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedOn { get; set; }
    
    public string ContactInfo => !string.IsNullOrEmpty(Email) ? Email : Phone ?? "No contact";
    public string Status => IsDeleted ? "Deleted" : (IsSent ? "Sent" : "Pending");
}

public class InvitationListFilters
{
    public string? Search { get; set; }
    public long? EventId { get; set; }
    public bool? IsSent { get; set; }
    public bool? HasResponse { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class InvitationListRequest
{
    public string? Search { get; set; }
    public long? EventId { get; set; }
    public bool? IsSent { get; set; }
    public bool? HasResponse { get; set; }
    public string SortBy { get; set; } = "CreatedOn";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; }
}

public class InvitationDetailsViewModel
{
    public required InvitationRecord Invitation { get; set; }
    public EventRecord? Event { get; set; }
    public ReservationRecord? Reservation { get; set; }
    public InvitationStatistics Statistics { get; set; } = new();
    
    // Extended fields (would come from enhanced invitation record in real implementation)
    public required string GuestName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int MaxPartySize { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    
    public string ContactInfo => !string.IsNullOrEmpty(Email) ? Email : Phone ?? "No contact";
    public bool HasContact => !string.IsNullOrEmpty(Email) || !string.IsNullOrEmpty(Phone);
    public bool HasResponse => Reservation != null;
}

public class InvitationStatistics
{
    public int TimesSent { get; set; }
    public DateTime? LastSentAt { get; set; }
    public bool ResponseReceived { get; set; }
    public DateTime? ResponseDate { get; set; }
    public int PartySize { get; set; }
    public bool IsAttending => PartySize > 0;
}

public class InvitationFormViewModel
{
    public required InvitationFormModel Invitation { get; set; }
    public bool IsNew { get; set; }
    public List<SelectListItem> EventOptions { get; set; } = new();
}

public class InvitationFormModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Event is required")]
    [Range(1, long.MaxValue, ErrorMessage = "Please select a valid event")]
    public long EventId { get; set; }

    [Required(ErrorMessage = "Guest name is required")]
    [StringLength(200, ErrorMessage = "Guest name must be less than 200 characters")]
    public string GuestName { get; set; } = "";

    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(255, ErrorMessage = "Email must be less than 255 characters")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [StringLength(20, ErrorMessage = "Phone number must be less than 20 characters")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Maximum party size is required")]
    [Range(1, 20, ErrorMessage = "Maximum party size must be between 1 and 20")]
    public int MaxPartySize { get; set; } = 2;

    public bool SendImmediately { get; set; }
    
    // Custom validation to ensure either email or phone is provided
    public bool HasContact => !string.IsNullOrEmpty(Email) || !string.IsNullOrEmpty(Phone);
}

// Additional enum for response status
public enum InvitationResponseStatus
{
    NoResponse,
    Attending,
    Regrets,
    Pending
}