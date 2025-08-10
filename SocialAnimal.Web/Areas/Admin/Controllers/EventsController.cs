using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Services;
using SocialAnimal.Core.Stubs;
using SocialAnimal.Web.Areas.Admin.Models.ViewModels;

namespace SocialAnimal.Web.Areas.Admin.Controllers;

[Route("admin/events")]
public class EventsController : AdminControllerBase
{
    private readonly IEventService _eventService;
    private readonly IInvitationService _invitationService;
    private readonly IReservationService _reservationService;

    public EventsController(
        IEventService eventService, 
        IInvitationService invitationService,
        IReservationService reservationService)
    {
        _eventService = eventService;
        _invitationService = invitationService;
        _reservationService = reservationService;
    }

    // List Actions
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search = null,
        string? state = null,
        string? city = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20,
        bool includeDeleted = false)
    {
        var request = new EventListRequest
        {
            Search = search,
            State = state,
            City = city,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            IncludeDeleted = includeDeleted
        };

        var result = await GetPagedEventsAsync(request);
        
        var model = new EventListViewModel
        {
            Items = result,
            Filters = new EventListFilters
            {
                Search = search,
                State = state,
                City = city,
                IncludeDeleted = includeDeleted
            },
            CurrentSort = new SortInfo(sortBy, sortOrder),
            CurrentFilters = new Dictionary<string, string?>(),
            StateOptions = GetStateOptions(),
            CityOptions = await GetCityOptionsAsync(state)
        };

        // Build current filters for form state
        if (!string.IsNullOrEmpty(search))
            model.CurrentFilters["search"] = search;
        if (!string.IsNullOrEmpty(state))
            model.CurrentFilters["state"] = state;
        if (!string.IsNullOrEmpty(city))
            model.CurrentFilters["city"] = city;
        if (includeDeleted)
            model.CurrentFilters["includeDeleted"] = "true";

        return View(model);
    }

    [HttpGet("list-partial")]
    public async Task<IActionResult> ListPartial(
        string? search = null,
        string? state = null,
        string? city = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20,
        bool includeDeleted = false)
    {
        var request = new EventListRequest
        {
            Search = search,
            State = state,
            City = city,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            IncludeDeleted = includeDeleted
        };

        var result = await GetPagedEventsAsync(request);
        return PartialView("_EventList", result);
    }

    // Detail Actions
    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var eventRecord = await _eventService.GetEventBySlugAsync(slug);
        if (eventRecord == null)
            return NotFound();

        var invitations = await _invitationService.GetEventInvitationsAsync(eventRecord.Id);
        var reservations = await _reservationService.GetEventReservationsAsync(eventRecord.Id);
        
        var model = new EventDetailsViewModel
        {
            Event = eventRecord,
            Invitations = invitations.Take(10).ToList(),
            Statistics = new EventStatistics
            {
                TotalInvitations = invitations.Count,
                SentInvitations = invitations.Count(i => i.IsSent),
                TotalReservations = reservations.Count,
                ConfirmedAttendees = reservations.Sum(r => r.PartySize),
                RegretCount = reservations.Count(r => r.PartySize == 0),
                PendingResponses = invitations.Count - reservations.Count,
                ResponseRate = invitations.Count > 0 
                    ? (double)reservations.Count / invitations.Count * 100 
                    : 0
            }
        };

        return View(model);
    }

    // Create Actions
    [HttpGet("create")]
    public IActionResult Create()
    {
        var model = new EventFormViewModel
        {
            Event = new EventFormModel(),
            IsNew = true,
            StateOptions = GetStateOptions()
        };
        return View("Form", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return HandleFormError(model, isNew: true);
        }

        try
        {
            var slug = await _eventService.GenerateUniqueSlugAsync(model.Title);
            var stub = new EventStub
            {
                Slug = slug,
                Title = model.Title,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                State = model.State,
                Postal = model.Postal
            };

            var created = await _eventService.CreateEventAsync(stub);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/events/{created.Slug}";
                return PartialView("_SuccessMessage", "Event created successfully!");
            }

            TempData["Success"] = "Event created successfully!";
            return RedirectToAction("Details", new { slug = created.Slug });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return HandleFormError(model, isNew: true);
        }
    }

    // Edit Actions
    [HttpGet("{slug}/edit")]
    public async Task<IActionResult> Edit(string slug)
    {
        var eventRecord = await _eventService.GetEventBySlugAsync(slug);
        if (eventRecord == null)
            return NotFound();

        var model = new EventFormViewModel
        {
            Event = new EventFormModel
            {
                Id = eventRecord.Id,
                Title = eventRecord.Title,
                AddressLine1 = eventRecord.AddressLine1,
                AddressLine2 = eventRecord.AddressLine2,
                City = eventRecord.City,
                State = eventRecord.State,
                Postal = eventRecord.Postal
            },
            IsNew = false,
            StateOptions = GetStateOptions()
        };

        return View("Form", model);
    }

    [HttpPost("{slug}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string slug, EventFormModel model)
    {
        var eventRecord = await _eventService.GetEventBySlugAsync(slug);
        if (eventRecord == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            return HandleFormError(model, isNew: false);
        }

        try
        {
            var newSlug = await _eventService.GenerateUniqueSlugAsync(model.Title);
            var stub = new EventStub
            {
                Slug = newSlug,
                Title = model.Title,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                State = model.State,
                Postal = model.Postal
            };

            var updated = await _eventService.UpdateEventAsync(eventRecord.Id, stub);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/events/{updated.Slug}";
                return PartialView("_SuccessMessage", "Event updated successfully!");
            }

            TempData["Success"] = "Event updated successfully!";
            return RedirectToAction("Details", new { slug = updated.Slug });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return HandleFormError(model, isNew: false);
        }
    }

    // Delete Actions
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            await _eventService.DeleteEventAsync(id);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = "/admin/events";
                return PartialView("_SuccessMessage", "Event deleted successfully!");
            }

            TempData["Success"] = "Event deleted successfully!";
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            if (IsHtmxRequest())
                return PartialView("_ErrorMessage", ex.Message);
                
            TempData["Error"] = ex.Message;
            return BadRequest();
        }
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(long id)
    {
        try
        {
            await _eventService.RestoreEventAsync(id);

            if (IsHtmxRequest())
                return PartialView("_SuccessMessage", "Event restored successfully!");

            TempData["Success"] = "Event restored successfully!";
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            if (IsHtmxRequest())
                return PartialView("_ErrorMessage", ex.Message);
                
            TempData["Error"] = ex.Message;
            return BadRequest();
        }
    }

    // Bulk Actions
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete(List<long> ids)
    {
        var successCount = 0;
        var failures = new List<string>();

        foreach (var id in ids)
        {
            try
            {
                await _eventService.DeleteEventAsync(id);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"Event ID {id}: {ex.Message}");
            }
        }

        if (successCount > 0)
        {
            TempData["Success"] = $"Successfully deleted {successCount} events.";
        }

        if (failures.Any())
        {
            TempData["Error"] = $"Failed to delete {failures.Count} events: {string.Join("; ", failures)}";
        }

        return RedirectToAction("Index");
    }

    // Export Actions
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        string? search = null,
        string? state = null,
        string? city = null,
        bool includeDeleted = false)
    {
        var request = new EventListRequest
        {
            Search = search,
            State = state,
            City = city,
            SortBy = "CreatedOn",
            SortOrder = "desc",
            Page = 1,
            PageSize = int.MaxValue,
            IncludeDeleted = includeDeleted
        };

        var events = await GetPagedEventsAsync(request);
        var csv = GenerateEventsCsv(events.Data);

        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"events_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        );
    }

    // AJAX endpoints for form helpers
    [HttpGet("cities")]
    public async Task<IActionResult> GetCitiesByState(string state)
    {
        var cities = await GetCityOptionsAsync(state);
        return Json(cities.Select(c => new { value = c.Value, text = c.Text }));
    }

    [HttpPost("validate-address")]
    public async Task<IActionResult> ValidateAddress([FromBody] EventFormModel model)
    {
        var stub = new EventStub
        {
            Slug = "", // Not needed for validation
            Title = model.Title,
            AddressLine1 = model.AddressLine1,
            AddressLine2 = model.AddressLine2,
            City = model.City,
            State = model.State,
            Postal = model.Postal
        };

        var isValid = await _eventService.ValidateAddressAsync(stub);
        return Json(new { valid = isValid });
    }

    // Helper Methods
    private IActionResult HandleFormError(EventFormModel model, bool isNew)
    {
        if (IsHtmxRequest())
        {
            return PartialView("_FormErrors", ModelState);
        }

        var viewModel = new EventFormViewModel
        {
            Event = model,
            IsNew = isNew,
            StateOptions = GetStateOptions()
        };

        return View("Form", viewModel);
    }

    private List<SelectListItem> GetStateOptions()
    {
        // US States - in a real application, this might come from a service
        return new List<SelectListItem>
        {
            new() { Value = "", Text = "Select State" },
            new() { Value = "AL", Text = "Alabama" },
            new() { Value = "AK", Text = "Alaska" },
            new() { Value = "AZ", Text = "Arizona" },
            new() { Value = "AR", Text = "Arkansas" },
            new() { Value = "CA", Text = "California" },
            new() { Value = "CO", Text = "Colorado" },
            new() { Value = "CT", Text = "Connecticut" },
            new() { Value = "DE", Text = "Delaware" },
            new() { Value = "FL", Text = "Florida" },
            new() { Value = "GA", Text = "Georgia" },
            new() { Value = "HI", Text = "Hawaii" },
            new() { Value = "ID", Text = "Idaho" },
            new() { Value = "IL", Text = "Illinois" },
            new() { Value = "IN", Text = "Indiana" },
            new() { Value = "IA", Text = "Iowa" },
            new() { Value = "KS", Text = "Kansas" },
            new() { Value = "KY", Text = "Kentucky" },
            new() { Value = "LA", Text = "Louisiana" },
            new() { Value = "ME", Text = "Maine" },
            new() { Value = "MD", Text = "Maryland" },
            new() { Value = "MA", Text = "Massachusetts" },
            new() { Value = "MI", Text = "Michigan" },
            new() { Value = "MN", Text = "Minnesota" },
            new() { Value = "MS", Text = "Mississippi" },
            new() { Value = "MO", Text = "Missouri" },
            new() { Value = "MT", Text = "Montana" },
            new() { Value = "NE", Text = "Nebraska" },
            new() { Value = "NV", Text = "Nevada" },
            new() { Value = "NH", Text = "New Hampshire" },
            new() { Value = "NJ", Text = "New Jersey" },
            new() { Value = "NM", Text = "New Mexico" },
            new() { Value = "NY", Text = "New York" },
            new() { Value = "NC", Text = "North Carolina" },
            new() { Value = "ND", Text = "North Dakota" },
            new() { Value = "OH", Text = "Ohio" },
            new() { Value = "OK", Text = "Oklahoma" },
            new() { Value = "OR", Text = "Oregon" },
            new() { Value = "PA", Text = "Pennsylvania" },
            new() { Value = "RI", Text = "Rhode Island" },
            new() { Value = "SC", Text = "South Carolina" },
            new() { Value = "SD", Text = "South Dakota" },
            new() { Value = "TN", Text = "Tennessee" },
            new() { Value = "TX", Text = "Texas" },
            new() { Value = "UT", Text = "Utah" },
            new() { Value = "VT", Text = "Vermont" },
            new() { Value = "VA", Text = "Virginia" },
            new() { Value = "WA", Text = "Washington" },
            new() { Value = "WV", Text = "West Virginia" },
            new() { Value = "WI", Text = "Wisconsin" },
            new() { Value = "WY", Text = "Wyoming" }
        };
    }

    private async Task<List<SelectListItem>> GetCityOptionsAsync(string? state)
    {
        // In a real application, this would query a database or external service
        var cities = new List<SelectListItem> { new() { Value = "", Text = "All Cities" } };
        
        if (!string.IsNullOrEmpty(state))
        {
            // Mock city data - in real app, get from database
            var mockCities = state.ToUpper() switch
            {
                "CA" => new[] { "Los Angeles", "San Francisco", "San Diego", "Sacramento" },
                "NY" => new[] { "New York", "Buffalo", "Rochester", "Albany" },
                "TX" => new[] { "Houston", "Dallas", "Austin", "San Antonio" },
                "WA" => new[] { "Seattle", "Spokane", "Tacoma", "Vancouver" },
                _ => Array.Empty<string>()
            };

            cities.AddRange(mockCities.Select(city => new SelectListItem { Value = city, Text = city }));
        }

        return cities;
    }

    private async Task<PagedResult<EventListItem>> GetPagedEventsAsync(EventListRequest request)
    {
        // This is a simplified implementation - in a real system, this would be in a service
        var allEvents = new List<EventRecord>(); // This would come from a repository query
        
        // Apply search filter
        if (!string.IsNullOrEmpty(request.Search))
        {
            allEvents = allEvents.Where(e => 
                e.Title.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                e.City.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                e.State.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Apply state filter
        if (!string.IsNullOrEmpty(request.State))
        {
            allEvents = allEvents.Where(e => e.State == request.State).ToList();
        }

        // Apply city filter
        if (!string.IsNullOrEmpty(request.City))
        {
            allEvents = allEvents.Where(e => e.City == request.City).ToList();
        }

        // Apply deleted filter
        if (!request.IncludeDeleted)
        {
            allEvents = allEvents.Where(e => !e.IsDeleted).ToList();
        }

        // Apply sorting
        allEvents = request.SortBy switch
        {
            "Title" => request.SortOrder == "asc" 
                ? allEvents.OrderBy(e => e.Title).ToList()
                : allEvents.OrderByDescending(e => e.Title).ToList(),
            "City" => request.SortOrder == "asc" 
                ? allEvents.OrderBy(e => e.City).ToList()
                : allEvents.OrderByDescending(e => e.City).ToList(),
            "State" => request.SortOrder == "asc" 
                ? allEvents.OrderBy(e => e.State).ToList()
                : allEvents.OrderByDescending(e => e.State).ToList(),
            _ => request.SortOrder == "asc" 
                ? allEvents.OrderBy(e => e.CreatedOn).ToList()
                : allEvents.OrderByDescending(e => e.CreatedOn).ToList()
        };

        var totalCount = allEvents.Count;
        var pagedEvents = allEvents
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EventListItem
            {
                Id = e.Id,
                Slug = e.Slug,
                Title = e.Title,
                Location = $"{e.City}, {e.State}",
                FullAddress = e.FullAddress,
                IsDeleted = e.IsDeleted,
                CreatedOn = e.CreatedOn.ToDateTimeOffset().DateTime,
                InvitationCount = 0, // Would be calculated from invitations
                AttendeeCount = 0    // Would be calculated from reservations
            })
            .ToList();

        return new PagedResult<EventListItem>
        {
            Data = pagedEvents,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    private string GenerateEventsCsv(IEnumerable<EventListItem> events)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Title,Location,Address,Created On,Invitations,Attendees,Status");

        foreach (var eventItem in events)
        {
            csv.AppendLine($"{eventItem.Title},{eventItem.Location},\"{eventItem.FullAddress}\",{eventItem.CreatedOn:yyyy-MM-dd},{eventItem.InvitationCount},{eventItem.AttendeeCount},{(eventItem.IsDeleted ? "Deleted" : "Active")}");
        }

        return csv.ToString();
    }
}