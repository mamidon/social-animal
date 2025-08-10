using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Services;
using SocialAnimal.Core.Stubs;
using SocialAnimal.Web.Areas.Admin.Models.ViewModels;

namespace SocialAnimal.Web.Areas.Admin.Controllers;

[Route("admin/invitations")]
public class InvitationsController : AdminControllerBase
{
    private readonly IInvitationService _invitationService;
    private readonly IEventService _eventService;
    private readonly IReservationService _reservationService;

    public InvitationsController(
        IInvitationService invitationService, 
        IEventService eventService,
        IReservationService reservationService)
    {
        _invitationService = invitationService;
        _eventService = eventService;
        _reservationService = reservationService;
    }

    // List Actions
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search = null,
        long? eventId = null,
        bool? isSent = null,
        bool? hasResponse = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20,
        bool includeDeleted = false)
    {
        var request = new InvitationListRequest
        {
            Search = search,
            EventId = eventId,
            IsSent = isSent,
            HasResponse = hasResponse,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            IncludeDeleted = includeDeleted
        };

        var result = await GetPagedInvitationsAsync(request);
        
        var model = new InvitationListViewModel
        {
            Items = result,
            Filters = new InvitationListFilters
            {
                Search = search,
                EventId = eventId,
                IsSent = isSent,
                HasResponse = hasResponse,
                IncludeDeleted = includeDeleted
            },
            CurrentSort = new SortInfo(sortBy, sortOrder),
            CurrentFilters = new Dictionary<string, string?>(),
            EventOptions = await GetEventOptionsAsync()
        };

        // Build current filters for form state
        if (!string.IsNullOrEmpty(search))
            model.CurrentFilters["search"] = search;
        if (eventId.HasValue)
            model.CurrentFilters["eventId"] = eventId.Value.ToString();
        if (isSent.HasValue)
            model.CurrentFilters["isSent"] = isSent.Value.ToString().ToLower();
        if (hasResponse.HasValue)
            model.CurrentFilters["hasResponse"] = hasResponse.Value.ToString().ToLower();
        if (includeDeleted)
            model.CurrentFilters["includeDeleted"] = "true";

        return View(model);
    }

    [HttpGet("list-partial")]
    public async Task<IActionResult> ListPartial(
        string? search = null,
        long? eventId = null,
        bool? isSent = null,
        bool? hasResponse = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20,
        bool includeDeleted = false)
    {
        var request = new InvitationListRequest
        {
            Search = search,
            EventId = eventId,
            IsSent = isSent,
            HasResponse = hasResponse,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            IncludeDeleted = includeDeleted
        };

        var result = await GetPagedInvitationsAsync(request);
        return PartialView("_InvitationList", result);
    }

    // Detail Actions
    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var invitation = await _invitationService.GetInvitationBySlugAsync(slug);
        if (invitation == null)
            return NotFound();

        // Get related event
        var eventRecord = invitation.Event ?? await _eventService.GetEventBySlugAsync(invitation.EventId.ToString());
        
        // Get reservation if exists
        var reservation = await _reservationService.GetReservationByInvitationAsync(invitation.Id);
        
        var model = new InvitationDetailsViewModel
        {
            Invitation = invitation,
            Event = eventRecord,
            Reservation = reservation,
            // Mock data - in real implementation these would come from the invitation record
            GuestName = $"Guest {invitation.Id}",
            Email = $"guest{invitation.Id}@example.com",
            Phone = null,
            MaxPartySize = 2,
            IsSent = false,
            SentAt = null,
            Statistics = new InvitationStatistics
            {
                TimesSent = 0,
                LastSentAt = null,
                ResponseReceived = reservation != null,
                ResponseDate = reservation?.CreatedOn.ToDateTimeOffset().DateTime,
                PartySize = reservation?.PartySize ?? 0
            }
        };

        return View(model);
    }

    // Create Actions
    [HttpGet("create")]
    public async Task<IActionResult> Create(long? eventId = null)
    {
        var model = new InvitationFormViewModel
        {
            Invitation = new InvitationFormModel 
            { 
                EventId = eventId ?? 0,
                MaxPartySize = 2 
            },
            IsNew = true,
            EventOptions = await GetEventOptionsAsync()
        };
        return View("Form", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvitationFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return await HandleFormErrorAsync(model, isNew: true);
        }

        try
        {
            var slug = await _invitationService.GenerateInvitationSlugAsync(model.GuestName, model.EventId);
            var stub = new InvitationStub
            {
                Slug = slug,
                EventId = model.EventId
            };

            var created = await _invitationService.CreateInvitationAsync(stub);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/invitations/{created.Slug}";
                return PartialView("_SuccessMessage", "Invitation created successfully!");
            }

            TempData["Success"] = "Invitation created successfully!";
            return RedirectToAction("Details", new { slug = created.Slug });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await HandleFormErrorAsync(model, isNew: true);
        }
    }

    // Edit Actions
    [HttpGet("{slug}/edit")]
    public async Task<IActionResult> Edit(string slug)
    {
        var invitation = await _invitationService.GetInvitationBySlugAsync(slug);
        if (invitation == null)
            return NotFound();

        var model = new InvitationFormViewModel
        {
            Invitation = new InvitationFormModel
            {
                Id = invitation.Id,
                EventId = invitation.EventId,
                // Mock data - in real implementation these would come from the invitation record
                GuestName = $"Guest {invitation.Id}",
                Email = $"guest{invitation.Id}@example.com",
                Phone = "",
                MaxPartySize = 2
            },
            IsNew = false,
            EventOptions = await GetEventOptionsAsync()
        };

        return View("Form", model);
    }

    [HttpPost("{slug}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string slug, InvitationFormModel model)
    {
        var invitation = await _invitationService.GetInvitationBySlugAsync(slug);
        if (invitation == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            return await HandleFormErrorAsync(model, isNew: false);
        }

        try
        {
            // Note: The current InvitationStub only has basic fields
            // In a real implementation, we would update the stub to include guest details
            var updateStub = new InvitationUpdateStub();
            var updated = await _invitationService.UpdateInvitationAsync(invitation.Id, updateStub);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/invitations/{updated.Slug}";
                return PartialView("_SuccessMessage", "Invitation updated successfully!");
            }

            TempData["Success"] = "Invitation updated successfully!";
            return RedirectToAction("Details", new { slug = updated.Slug });
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
            await _invitationService.DeleteInvitationAsync(id);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = "/admin/invitations";
                return PartialView("_SuccessMessage", "Invitation deleted successfully!");
            }

            TempData["Success"] = "Invitation deleted successfully!";
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
    [HttpPost("{id}/send")]
    public async Task<IActionResult> SendInvitation(long id)
    {
        try
        {
            await _invitationService.SendInvitationAsync(id);

            if (IsHtmxRequest())
                return PartialView("_SuccessMessage", "Invitation sent successfully!");

            TempData["Success"] = "Invitation sent successfully!";
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

    [HttpPost("{id}/resend")]
    public async Task<IActionResult> ResendInvitation(long id)
    {
        try
        {
            await _invitationService.ResendInvitationAsync(id);

            if (IsHtmxRequest())
                return PartialView("_SuccessMessage", "Invitation resent successfully!");

            TempData["Success"] = "Invitation resent successfully!";
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
                await _invitationService.DeleteInvitationAsync(id);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"Invitation ID {id}: {ex.Message}");
            }
        }

        if (successCount > 0)
        {
            TempData["Success"] = $"Successfully deleted {successCount} invitations.";
        }

        if (failures.Any())
        {
            TempData["Error"] = $"Failed to delete {failures.Count} invitations: {string.Join("; ", failures)}";
        }

        return RedirectToAction("Index");
    }

    [HttpPost("bulk-send")]
    public async Task<IActionResult> BulkSend(List<long> ids)
    {
        var successCount = 0;
        var failures = new List<string>();

        foreach (var id in ids)
        {
            try
            {
                await _invitationService.SendInvitationAsync(id);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"Invitation ID {id}: {ex.Message}");
            }
        }

        if (successCount > 0)
        {
            TempData["Success"] = $"Successfully sent {successCount} invitations.";
        }

        if (failures.Any())
        {
            TempData["Error"] = $"Failed to send {failures.Count} invitations: {string.Join("; ", failures)}";
        }

        return RedirectToAction("Index");
    }

    // Export Actions
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        string? search = null,
        long? eventId = null,
        bool? isSent = null,
        bool includeDeleted = false)
    {
        var request = new InvitationListRequest
        {
            Search = search,
            EventId = eventId,
            IsSent = isSent,
            SortBy = "CreatedOn",
            SortOrder = "desc",
            Page = 1,
            PageSize = int.MaxValue,
            IncludeDeleted = includeDeleted
        };

        var invitations = await GetPagedInvitationsAsync(request);
        var csv = GenerateInvitationsCsv(invitations.Data);

        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"invitations_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        );
    }

    // Helper Methods
    private async Task<IActionResult> HandleFormErrorAsync(InvitationFormModel model, bool isNew)
    {
        if (IsHtmxRequest())
        {
            return PartialView("_FormErrors", ModelState);
        }

        var viewModel = new InvitationFormViewModel
        {
            Invitation = model,
            IsNew = isNew,
            EventOptions = await GetEventOptionsAsync()
        };

        return View("Form", viewModel);
    }

    private async Task<List<SelectListItem>> GetEventOptionsAsync()
    {
        var upcomingEvents = await _eventService.GetUpcomingEventsAsync(0, 100);
        
        var options = new List<SelectListItem>
        {
            new() { Value = "", Text = "Select Event" }
        };

        options.AddRange(upcomingEvents.Select(e => new SelectListItem 
        { 
            Value = e.Id.ToString(), 
            Text = e.Title 
        }));

        return options;
    }

    private async Task<PagedResult<InvitationListItem>> GetPagedInvitationsAsync(InvitationListRequest request)
    {
        // This is a simplified implementation - in a real system, this would be in a service
        var allInvitations = new List<InvitationRecord>(); // This would come from a repository query
        
        // Apply event filter
        if (request.EventId.HasValue)
        {
            allInvitations = allInvitations.Where(i => i.EventId == request.EventId.Value).ToList();
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(request.Search))
        {
            // In real implementation, this would search guest names, email, etc.
            allInvitations = allInvitations.Where(i => 
                i.Slug.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Apply deleted filter
        if (!request.IncludeDeleted)
        {
            allInvitations = allInvitations.Where(i => !i.IsDeleted).ToList();
        }

        // Apply sorting
        allInvitations = request.SortBy switch
        {
            "EventId" => request.SortOrder == "asc" 
                ? allInvitations.OrderBy(i => i.EventId).ToList()
                : allInvitations.OrderByDescending(i => i.EventId).ToList(),
            _ => request.SortOrder == "asc" 
                ? allInvitations.OrderBy(i => i.CreatedOn).ToList()
                : allInvitations.OrderByDescending(i => i.CreatedOn).ToList()
        };

        var totalCount = allInvitations.Count;
        var pagedInvitations = allInvitations
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvitationListItem
            {
                Id = i.Id,
                Slug = i.Slug,
                EventId = i.EventId,
                EventTitle = $"Event {i.EventId}", // Would be populated from event data
                // Mock data - in real implementation these would come from the invitation record
                GuestName = $"Guest {i.Id}",
                Email = $"guest{i.Id}@example.com",
                Phone = null,
                MaxPartySize = 2,
                IsSent = false,
                HasResponse = false,
                ResponseStatus = null,
                IsDeleted = i.IsDeleted,
                CreatedOn = i.CreatedOn.ToDateTimeOffset().DateTime
            })
            .ToList();

        return new PagedResult<InvitationListItem>
        {
            Data = pagedInvitations,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    private string GenerateInvitationsCsv(IEnumerable<InvitationListItem> invitations)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Guest Name,Email,Phone,Event,Max Party Size,Sent,Response,Created On,Status");

        foreach (var invitation in invitations)
        {
            csv.AppendLine($"{invitation.GuestName},{invitation.Email},{invitation.Phone ?? ""},{invitation.EventTitle},{invitation.MaxPartySize},{invitation.IsSent},{invitation.ResponseStatus ?? "No Response"},{invitation.CreatedOn:yyyy-MM-dd},{(invitation.IsDeleted ? "Deleted" : "Active")}");
        }

        return csv.ToString();
    }
}