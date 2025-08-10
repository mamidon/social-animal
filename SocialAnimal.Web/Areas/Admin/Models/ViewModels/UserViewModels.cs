using System.ComponentModel.DataAnnotations;
using SocialAnimal.Core.Domain;

namespace SocialAnimal.Web.Areas.Admin.Models.ViewModels;

public class UserListViewModel
{
    public PagedResult<UserListItem> Items { get; set; } = new();
    public UserListFilters Filters { get; set; } = new();
    public SortInfo CurrentSort { get; set; } = new("CreatedOn", "desc");
    public Dictionary<string, string?> CurrentFilters { get; set; } = new();
    public bool HasFilters => CurrentFilters.Any();
}

public class UserListItem
{
    public long Id { get; set; }
    public required string Slug { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedOn { get; set; }
    public int ReservationCount { get; set; }
}

public class UserListFilters
{
    public string? Search { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class UserListRequest
{
    public string? Search { get; set; }
    public string SortBy { get; set; } = "CreatedOn";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; }
}

public class UserDetailsViewModel
{
    public required UserRecord User { get; set; }
    public List<ReservationRecord> Reservations { get; set; } = new();
    public UserStatistics Statistics { get; set; } = new();
}

public class UserStatistics
{
    public int TotalReservations { get; set; }
    public int AttendedEvents { get; set; }
    public int RegretEvents { get; set; }
    public int TotalPartySize { get; set; }
    public double AttendanceRate => TotalReservations > 0 
        ? (double)AttendedEvents / TotalReservations * 100 
        : 0;
}

public class UserFormViewModel
{
    public required UserFormModel User { get; set; }
    public bool IsNew { get; set; }
}

public class UserFormModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name must be less than 100 characters")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name must be less than 100 characters")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [StringLength(20, ErrorMessage = "Phone number must be less than 20 characters")]
    public string Phone { get; set; } = "";
}

public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int StartItem => (Page - 1) * PageSize + 1;
    public int EndItem => Math.Min(Page * PageSize, TotalCount);
}

public class SortInfo
{
    public string Field { get; set; }
    public string Direction { get; set; }

    public SortInfo(string field, string direction)
    {
        Field = field;
        Direction = direction;
    }
}