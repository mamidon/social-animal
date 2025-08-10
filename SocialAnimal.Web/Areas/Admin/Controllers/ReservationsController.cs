using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Services;
using SocialAnimal.Core.Stubs;
using SocialAnimal.Web.Areas.Admin.Models.ViewModels;

namespace SocialAnimal.Web.Areas.Admin.Controllers;

[Route("admin/reservations")]
public class ReservationsController : AdminControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly IInvitationService _invitationService;
    private readonly IUserService _userService;
    private readonly IEventService _eventService;

    public ReservationsController(
        IReservationService reservationService,
        IInvitationService invitationService,
        IUserService userService,
        IEventService eventService)
    {
        _reservationService = reservationService;
        _invitationService = invitationService;
        _userService = userService;
        _eventService = eventService;
    }

    // List Actions
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search = null,
        long? eventId = null,
        long? userId = null,
        bool? isAttending = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20)
    {
        var request = new ReservationListRequest
        {
            Search = search,
            EventId = eventId,
            UserId = userId,
            IsAttending = isAttending,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize
        };

        var result = await GetPagedReservationsAsync(request);
        
        var model = new ReservationListViewModel
        {
            Items = result,
            Filters = new ReservationListFilters
            {
                Search = search,
                EventId = eventId,
                UserId = userId,
                IsAttending = isAttending
            },
            CurrentSort = new SortInfo(sortBy, sortOrder),
            CurrentFilters = new Dictionary<string, string?>(),
            EventOptions = await GetEventOptionsAsync(),
            UserOptions = await GetUserOptionsAsync()
        };

        // Build current filters for form state
        if (!string.IsNullOrEmpty(search))
            model.CurrentFilters["search"] = search;
        if (eventId.HasValue)
            model.CurrentFilters["eventId"] = eventId.Value.ToString();
        if (userId.HasValue)
            model.CurrentFilters["userId"] = userId.Value.ToString();
        if (isAttending.HasValue)
            model.CurrentFilters["isAttending"] = isAttending.Value.ToString().ToLower();

        return View(model);
    }

    [HttpGet("list-partial")]
    public async Task<IActionResult> ListPartial(
        string? search = null,
        long? eventId = null,
        long? userId = null,
        bool? isAttending = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20)
    {
        var request = new ReservationListRequest
        {
            Search = search,
            EventId = eventId,
            UserId = userId,
            IsAttending = isAttending,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize
        };

        var result = await GetPagedReservationsAsync(request);
        return PartialView("_ReservationList", result);
    }

    // Detail Actions
    [HttpGet("{id}")]
    public async Task<IActionResult> Details(long id)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(id);
        if (reservation == null)
            return NotFound();

        // Get related data
        var invitation = reservation.Invitation ?? await _invitationService.GetInvitationByIdAsync(reservation.InvitationId);
        var user = reservation.User ?? await _userService.GetUserByIdAsync(reservation.UserId);
        var eventRecord = invitation?.Event ?? (invitation != null ? await _eventService.GetEventBySlugAsync(invitation.EventId.ToString()) : null);
        
        var model = new ReservationDetailsViewModel
        {
            Reservation = reservation,
            Invitation = invitation,
            User = user,
            Event = eventRecord,
            // Mock data - would come from extended reservation record in real implementation
            GuestName = user?.FullName ?? $"User {reservation.UserId}",
            Email = user != null ? $"user{user.Id}@example.com" : null,
            Phone = user?.Phone,
            Notes = "",
            DietaryRestrictions = "",
            Statistics = new ReservationStatistics
            {
                ResponseTime = reservation.CreatedOn.ToDateTimeOffset() - (invitation?.CreatedOn.ToDateTimeOffset() ?? DateTimeOffset.Now),
                IsLateResponse = false,
                ModificationCount = 0,
                LastModifiedAt = reservation.UpdatedOn?.ToDateTimeOffset().DateTime
            }
        };

        return View(model);
    }

    // Create Actions
    [HttpGet("create")]
    public async Task<IActionResult> Create(long? invitationId = null, long? userId = null)
    {
        var model = new ReservationFormViewModel
        {
            Reservation = new ReservationFormModel 
            { 
                InvitationId = invitationId ?? 0,
                UserId = userId ?? 0,
                PartySize = 1
            },
            IsNew = true,
            InvitationOptions = await GetInvitationOptionsAsync(),
            UserOptions = await GetUserOptionsAsync()
        };
        return View("Form", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return await HandleFormErrorAsync(model, isNew: true);
        }

        // Validate party size against invitation limits
        var isValidPartySize = await _reservationService.ValidatePartySizeAsync(new ReservationStub
        {
            InvitationId = model.InvitationId,
            UserId = model.UserId,
            PartySize = model.PartySize
        });

        if (!isValidPartySize)
        {
            ModelState.AddModelError(nameof(model.PartySize), "Party size exceeds invitation maximum.");
            return await HandleFormErrorAsync(model, isNew: true);
        }

        try
        {
            var stub = new ReservationStub
            {
                InvitationId = model.InvitationId,
                UserId = model.UserId,
                PartySize = model.PartySize
            };

            var created = await _reservationService.CreateReservationAsync(stub);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/reservations/{created.Id}";
                return PartialView("_SuccessMessage", "Reservation created successfully!");
            }

            TempData["Success"] = "Reservation created successfully!";
            return RedirectToAction("Details", new { id = created.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await HandleFormErrorAsync(model, isNew: true);
        }
    }

    // Edit Actions
    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(long id)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(id);
        if (reservation == null)
            return NotFound();

        var model = new ReservationFormViewModel
        {
            Reservation = new ReservationFormModel
            {
                Id = reservation.Id,
                InvitationId = reservation.InvitationId,
                UserId = reservation.UserId,
                PartySize = reservation.PartySize,
                // Mock data - would come from extended reservation record
                Notes = "",
                DietaryRestrictions = ""
            },
            IsNew = false,
            InvitationOptions = await GetInvitationOptionsAsync(),
            UserOptions = await GetUserOptionsAsync()
        };

        return View("Form", model);
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ReservationFormModel model)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(id);
        if (reservation == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            return await HandleFormErrorAsync(model, isNew: false);
        }

        try
        {
            var updateStub = new ReservationUpdateStub
            {
                PartySize = model.PartySize
            };

            var updated = await _reservationService.UpdateReservationAsync(id, updateStub);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/reservations/{updated.Id}";
                return PartialView("_SuccessMessage", "Reservation updated successfully!");
            }

            TempData["Success"] = "Reservation updated successfully!";
            return RedirectToAction("Details", new { id = updated.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await HandleFormErrorAsync(model, isNew: false);
        }
    }

    // Delete Actions
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            await _reservationService.DeleteReservationAsync(id);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = "/admin/reservations";
                return PartialView("_SuccessMessage", "Reservation deleted successfully!");
            }

            TempData["Success"] = "Reservation deleted successfully!";
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

    // Special Actions
    [HttpPost("{id}/mark-attended")]
    public async Task<IActionResult> MarkAttended(long id)
    {
        try
        {
            await _reservationService.MarkAsAttendedAsync(id);

            if (IsHtmxRequest())
                return PartialView("_SuccessMessage", "Marked as attended!");

            TempData["Success"] = "Marked as attended!";
            return RedirectToAction("Details", new { id });
        }
        catch (InvalidOperationException ex)
        {
            if (IsHtmxRequest())
                return PartialView("_ErrorMessage", ex.Message);
                
            TempData["Error"] = ex.Message;
            return BadRequest();
        }
    }

    [HttpPost("{id}/send-reminder")]
    public async Task<IActionResult> SendReminder(long id)
    {
        try
        {
            await _reservationService.SendReminderAsync(id);

            if (IsHtmxRequest())
                return PartialView("_SuccessMessage", "Reminder sent successfully!");

            TempData["Success"] = "Reminder sent successfully!";
            return RedirectToAction("Details", new { id });
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
                await _reservationService.DeleteReservationAsync(id);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"Reservation ID {id}: {ex.Message}");
            }
        }

        if (successCount > 0)
        {
            TempData["Success"] = $"Successfully deleted {successCount} reservations.";
        }

        if (failures.Any())
        {
            TempData["Error"] = $"Failed to delete {failures.Count} reservations: {string.Join("; ", failures)}";
        }

        return RedirectToAction("Index");
    }

    // Export Actions
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        string? search = null,
        long? eventId = null,
        long? userId = null,
        bool? isAttending = null)
    {
        var request = new ReservationListRequest
        {
            Search = search,
            EventId = eventId,
            UserId = userId,
            IsAttending = isAttending,
            SortBy = "CreatedOn",
            SortOrder = "desc",
            Page = 1,
            PageSize = int.MaxValue
        };

        var reservations = await GetPagedReservationsAsync(request);
        var csv = GenerateReservationsCsv(reservations.Data);

        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"reservations_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        );
    }

    // AJAX endpoints
    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitationsByEvent(long eventId)
    {
        var invitations = await _invitationService.GetEventInvitationsAsync(eventId, 0, 100);
        return Json(invitations.Select(i => new { value = i.Id, text = $"Invitation for Guest {i.Id}" }));
    }

    // Helper Methods
    private async Task<IActionResult> HandleFormErrorAsync(ReservationFormModel model, bool isNew)
    {
        if (IsHtmxRequest())
        {
            return PartialView("_FormErrors", ModelState);
        }

        var viewModel = new ReservationFormViewModel
        {
            Reservation = model,
            IsNew = isNew,
            InvitationOptions = await GetInvitationOptionsAsync(),
            UserOptions = await GetUserOptionsAsync()
        };

        return View("Form", viewModel);
    }

    private async Task<List<SelectListItem>> GetEventOptionsAsync()
    {
        var upcomingEvents = await _eventService.GetUpcomingEventsAsync(0, 100);
        
        var options = new List<SelectListItem>
        {
            new() { Value = "", Text = "All Events" }
        };

        options.AddRange(upcomingEvents.Select(e => new SelectListItem 
        { 
            Value = e.Id.ToString(), 
            Text = e.Title 
        }));

        return options;
    }

    private async Task<List<SelectListItem>> GetInvitationOptionsAsync()
    {
        // In a real implementation, this would query invitations
        return new List<SelectListItem>
        {
            new() { Value = "", Text = "Select Invitation" }
        };
    }

    private async Task<List<SelectListItem>> GetUserOptionsAsync()
    {
        // In a real implementation, this would query users
        return new List<SelectListItem>
        {
            new() { Value = "", Text = "Select User" }
        };
    }

    private async Task<PagedResult<ReservationListItem>> GetPagedReservationsAsync(ReservationListRequest request)
    {
        // This is a simplified implementation - in a real system, this would be in a service
        var allReservations = new List<ReservationRecord>(); // This would come from a repository query
        
        // Apply event filter
        if (request.EventId.HasValue)
        {
            // Would filter by event through invitation relationship
        }

        // Apply user filter
        if (request.UserId.HasValue)
        {
            allReservations = allReservations.Where(r => r.UserId == request.UserId.Value).ToList();
        }

        // Apply attendance filter
        if (request.IsAttending.HasValue)
        {
            if (request.IsAttending.Value)
            {
                allReservations = allReservations.Where(r => r.PartySize > 0).ToList();
            }
            else
            {
                allReservations = allReservations.Where(r => r.PartySize == 0).ToList();
            }
        }

        // Apply sorting
        allReservations = request.SortBy switch
        {
            "PartySize" => request.SortOrder == "asc" 
                ? allReservations.OrderBy(r => r.PartySize).ToList()
                : allReservations.OrderByDescending(r => r.PartySize).ToList(),
            "UserId" => request.SortOrder == "asc" 
                ? allReservations.OrderBy(r => r.UserId).ToList()
                : allReservations.OrderByDescending(r => r.UserId).ToList(),
            _ => request.SortOrder == "asc" 
                ? allReservations.OrderBy(r => r.CreatedOn).ToList()
                : allReservations.OrderByDescending(r => r.CreatedOn).ToList()
        };

        var totalCount = allReservations.Count;
        var pagedReservations = allReservations
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReservationListItem
            {
                Id = r.Id,
                InvitationId = r.InvitationId,
                UserId = r.UserId,
                PartySize = r.PartySize,
                IsAttending = r.IsAttending,
                CreatedOn = r.CreatedOn.ToDateTimeOffset().DateTime,
                // Mock data - would be populated from related entities
                GuestName = $"User {r.UserId}",
                EventTitle = $"Event for Invitation {r.InvitationId}",
                UserName = $"User {r.UserId}",
                Email = $"user{r.UserId}@example.com",
                Phone = "+1 (555) 123-4567"
            })
            .ToList();

        return new PagedResult<ReservationListItem>
        {
            Data = pagedReservations,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    private string GenerateReservationsCsv(IEnumerable<ReservationListItem> reservations)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Guest Name,User Name,Email,Phone,Event,Party Size,Status,Created On");

        foreach (var reservation in reservations)
        {
            csv.AppendLine($"{reservation.GuestName},{reservation.UserName},{reservation.Email},{reservation.Phone},{reservation.EventTitle},{reservation.PartySize},{(reservation.IsAttending ? "Attending" : "Regrets")},{reservation.CreatedOn:yyyy-MM-dd}");
        }

        return csv.ToString();
    }
}