using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Services;
using SocialAnimal.Core.Stubs;
using SocialAnimal.Web.Areas.Admin.Models.ViewModels;

namespace SocialAnimal.Web.Areas.Admin.Controllers;

[Route("admin/users")]
public class UsersController : AdminControllerBase
{
    private readonly IUserService _userService;
    private readonly IReservationService _reservationService;

    public UsersController(
        IUserService userService, 
        IReservationService reservationService)
    {
        _userService = userService;
        _reservationService = reservationService;
    }

    // List Actions
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20,
        bool includeDeleted = false)
    {
        var request = new UserListRequest
        {
            Search = search,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            IncludeDeleted = includeDeleted
        };

        var result = await GetPagedUsersAsync(request);
        
        var model = new UserListViewModel
        {
            Items = result,
            Filters = new UserListFilters
            {
                Search = search,
                IncludeDeleted = includeDeleted
            },
            CurrentSort = new SortInfo(sortBy, sortOrder),
            CurrentFilters = new Dictionary<string, string?>()
        };

        if (!string.IsNullOrEmpty(search))
            model.CurrentFilters["search"] = search;
        if (includeDeleted)
            model.CurrentFilters["includeDeleted"] = "true";

        return View(model);
    }

    [HttpGet("list-partial")]
    public async Task<IActionResult> ListPartial(
        string? search = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20,
        bool includeDeleted = false)
    {
        var request = new UserListRequest
        {
            Search = search,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            IncludeDeleted = includeDeleted
        };

        var result = await GetPagedUsersAsync(request);
        return PartialView("_UserList", result);
    }

    // Detail Actions
    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var user = await _userService.GetUserBySlugAsync(slug);
        if (user == null)
            return NotFound();

        var reservations = await _reservationService.GetUserReservationsAsync(user.Id);
        
        var model = new UserDetailsViewModel
        {
            User = user,
            Reservations = reservations.ToList(),
            Statistics = new UserStatistics
            {
                TotalReservations = reservations.Count(),
                AttendedEvents = reservations.Count(r => r.PartySize > 0),
                RegretEvents = reservations.Count(r => r.PartySize == 0),
                TotalPartySize = reservations.Sum(r => r.PartySize)
            }
        };

        return View(model);
    }

    // Create Actions
    [HttpGet("create")]
    public IActionResult Create()
    {
        var model = new UserFormViewModel
        {
            User = new UserFormModel(),
            IsNew = true
        };
        return View("Form", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return HandleFormError(model, isNew: true);
        }

        try
        {
            var slug = await _userService.GenerateUserSlugAsync($"{model.FirstName} {model.LastName}");
            var stub = new UserStub
            {
                Slug = slug,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone
            };

            var created = await _userService.CreateUserAsync(stub);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/users/{created.Slug}";
                return PartialView("_SuccessMessage", "User created successfully!");
            }

            TempData["Success"] = "User created successfully!";
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
        var user = await _userService.GetUserBySlugAsync(slug);
        if (user == null)
            return NotFound();

        var model = new UserFormViewModel
        {
            User = new UserFormModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone
            },
            IsNew = false
        };

        return View("Form", model);
    }

    [HttpPost("{slug}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string slug, UserFormModel model)
    {
        var user = await _userService.GetUserBySlugAsync(slug);
        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            return HandleFormError(model, isNew: false);
        }

        try
        {
            var newSlug = await _userService.GenerateUserSlugAsync($"{model.FirstName} {model.LastName}");
            var stub = new UserStub
            {
                Slug = newSlug,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone
            };

            var updated = await _userService.UpdateUserAsync(user.Id, stub);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/users/{updated.Slug}";
                return PartialView("_SuccessMessage", "User updated successfully!");
            }

            TempData["Success"] = "User updated successfully!";
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
            await _userService.DeleteUserAsync(id);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = "/admin/users";
                return PartialView("_SuccessMessage", "User deleted successfully!");
            }

            TempData["Success"] = "User deleted successfully!";
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
            await _userService.RestoreUserAsync(id);

            if (IsHtmxRequest())
                return PartialView("_SuccessMessage", "User restored successfully!");

            TempData["Success"] = "User restored successfully!";
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
                await _userService.DeleteUserAsync(id);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"User ID {id}: {ex.Message}");
            }
        }

        if (successCount > 0)
        {
            TempData["Success"] = $"Successfully deleted {successCount} users.";
        }

        if (failures.Any())
        {
            TempData["Error"] = $"Failed to delete {failures.Count} users: {string.Join("; ", failures)}";
        }

        return RedirectToAction("Index");
    }

    // Export Actions
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        string? search = null,
        bool includeDeleted = false)
    {
        var request = new UserListRequest
        {
            Search = search,
            SortBy = "CreatedOn",
            SortOrder = "desc",
            Page = 1,
            PageSize = int.MaxValue,
            IncludeDeleted = includeDeleted
        };

        var users = await GetPagedUsersAsync(request);
        var csv = GenerateUsersCsv(users.Data);

        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"users_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        );
    }

    // Helper Methods
    private bool IsHtmxRequest()
    {
        return Request.Headers.ContainsKey("HX-Request");
    }

    private IActionResult HandleFormError(UserFormModel model, bool isNew)
    {
        if (IsHtmxRequest())
        {
            return PartialView("_FormErrors", ModelState);
        }

        var viewModel = new UserFormViewModel
        {
            User = model,
            IsNew = isNew
        };

        return View("Form", viewModel);
    }

    private async Task<PagedResult<UserListItem>> GetPagedUsersAsync(UserListRequest request)
    {
        // This is a simplified implementation - in a real system, this would be in a service
        // For now, we'll create a basic implementation
        var allUsers = new List<UserRecord>(); // This would come from a repository query
        
        // Apply search filter
        if (!string.IsNullOrEmpty(request.Search))
        {
            allUsers = allUsers.Where(u => 
                u.FirstName.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                u.Phone.Contains(request.Search)).ToList();
        }

        // Apply deleted filter
        if (!request.IncludeDeleted)
        {
            allUsers = allUsers.Where(u => !u.IsDeleted).ToList();
        }

        // Apply sorting
        allUsers = request.SortBy switch
        {
            "FirstName" => request.SortOrder == "asc" 
                ? allUsers.OrderBy(u => u.FirstName).ToList()
                : allUsers.OrderByDescending(u => u.FirstName).ToList(),
            "LastName" => request.SortOrder == "asc" 
                ? allUsers.OrderBy(u => u.LastName).ToList()
                : allUsers.OrderByDescending(u => u.LastName).ToList(),
            "Phone" => request.SortOrder == "asc" 
                ? allUsers.OrderBy(u => u.Phone).ToList()
                : allUsers.OrderByDescending(u => u.Phone).ToList(),
            _ => request.SortOrder == "asc" 
                ? allUsers.OrderBy(u => u.CreatedOn).ToList()
                : allUsers.OrderByDescending(u => u.CreatedOn).ToList()
        };

        var totalCount = allUsers.Count;
        var pagedUsers = allUsers
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                Slug = u.Slug,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                Phone = u.Phone,
                IsDeleted = u.IsDeleted,
                CreatedOn = u.CreatedOn.ToDateTimeOffset().DateTime,
                ReservationCount = 0 // Would be calculated from reservations
            })
            .ToList();

        return new PagedResult<UserListItem>
        {
            Data = pagedUsers,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }

    private string GenerateUsersCsv(IEnumerable<UserListItem> users)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("First Name,Last Name,Phone,Created On,Reservations,Status");

        foreach (var user in users)
        {
            csv.AppendLine($"{user.FirstName},{user.LastName},{user.Phone},{user.CreatedOn:yyyy-MM-dd},{user.ReservationCount},{(user.IsDeleted ? "Deleted" : "Active")}");
        }

        return csv.ToString();
    }
}