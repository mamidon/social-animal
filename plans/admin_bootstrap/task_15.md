# Task 15: Add Admin Portal Services and Controllers

## Objective
Complete the admin portal implementation by creating specialized admin services, finalizing controllers, implementing view models and DTOs, configuring dependency injection, and adding comprehensive error handling middleware. This task ties together all previous components into a cohesive admin system.

## Requirements
- Create admin-specific service layer for complex operations
- Finalize all admin controllers with proper patterns
- Implement comprehensive view models and DTOs
- Configure dependency injection for all components
- Add error handling and logging middleware
- Implement admin-specific business logic
- Add cross-cutting concerns (auth, audit, caching)

## Implementation Steps

### Step 1: Create Admin Service Layer
Location: `/SocialAnimal.Infrastructure/Services/Admin/`

Create admin-specific services for complex operations:

`AdminDashboardService.cs`:
```csharp
public interface IAdminDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<List<ActivityLogDto>> GetRecentActivityAsync(int count = 20);
    Task<Dictionary<string, ChartDataDto>> GetChartsDataAsync(DateRange range);
    Task<List<AlertDto>> GetSystemAlertsAsync();
}

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IEventService _eventService;
    private readonly IUserService _userService;
    private readonly IInvitationService _invitationService;
    private readonly IReservationService _reservationService;
    private readonly IActivityLogService _activityLogService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminDashboardService> _logger;
    
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        return await _cache.GetOrCreateAsync("admin:dashboard:summary", 
            async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);
                
                var tasks = new[]
                {
                    GetEventStatsAsync(),
                    GetUserStatsAsync(),
                    GetInvitationStatsAsync(),
                    GetReservationStatsAsync()
                };
                
                await Task.WhenAll(tasks);
                
                return new DashboardSummaryDto
                {
                    EventStats = tasks[0].Result,
                    UserStats = tasks[1].Result,
                    InvitationStats = tasks[2].Result,
                    ReservationStats = tasks[3].Result,
                    GeneratedAt = DateTime.UtcNow
                };
            });
    }
    
    private async Task<EntityStatsDto> GetEventStatsAsync()
    {
        var total = await _eventService.CountAsync();
        var active = await _eventService.CountActiveAsync();
        var recent = await _eventService.CountCreatedSinceAsync(DateTime.UtcNow.AddDays(-7));
        var previousWeek = await _eventService.CountCreatedBetweenAsync(
            DateTime.UtcNow.AddDays(-14), 
            DateTime.UtcNow.AddDays(-7));
        
        return new EntityStatsDto
        {
            Total = total,
            Active = active,
            RecentlyAdded = recent,
            GrowthRate = CalculateGrowthRate(recent, previousWeek),
            Trend = DetermineTrend(recent, previousWeek)
        };
    }
}
```

`AdminReportService.cs`:
```csharp
public interface IAdminReportService
{
    Task<byte[]> GenerateEventReportAsync(long eventId, ReportFormat format);
    Task<byte[]> GenerateAttendanceReportAsync(DateRange range);
    Task<byte[]> ExportEntityDataAsync(EntityType type, ExportOptions options);
    Task<ReportMetadata> ScheduleReportAsync(ReportRequest request);
}

public class AdminReportService : IAdminReportService
{
    public async Task<byte[]> GenerateEventReportAsync(long eventId, ReportFormat format)
    {
        var eventData = await GatherEventDataAsync(eventId);
        
        return format switch
        {
            ReportFormat.PDF => await GeneratePdfReportAsync(eventData),
            ReportFormat.Excel => await GenerateExcelReportAsync(eventData),
            ReportFormat.CSV => await GenerateCsvReportAsync(eventData),
            _ => throw new NotSupportedException($"Format {format} not supported")
        };
    }
}
```

`AdminBulkOperationService.cs`:
```csharp
public interface IAdminBulkOperationService
{
    Task<BulkOperationResult> BulkDeleteAsync<T>(List<long> ids) where T : BaseRecord;
    Task<BulkOperationResult> BulkUpdateAsync<T>(List<long> ids, Dictionary<string, object> updates) where T : BaseRecord;
    Task<BulkOperationResult> BulkRestoreAsync<T>(List<long> ids) where T : BaseRecord;
    Task<BulkOperationResult> ImportFromCsvAsync<T>(Stream csvStream, ImportOptions options) where T : BaseRecord;
}

public class AdminBulkOperationService : IAdminBulkOperationService
{
    public async Task<BulkOperationResult> BulkDeleteAsync<T>(List<long> ids) where T : BaseRecord
    {
        var result = new BulkOperationResult();
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var id in ids)
            {
                try
                {
                    await DeleteEntityAsync<T>(id);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Failures.Add(new BulkOperationFailure
                    {
                        Id = id,
                        Error = ex.Message
                    });
                }
            }
            
            if (result.SuccessCount > 0 && result.Failures.Count == 0)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
        return result;
    }
}
```

### Step 2: Finalize Admin Controllers
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/`

Complete controller implementations with all actions:

`AdminControllerBase.cs`:
```csharp
[Area("Admin")]
[Route("admin/[controller]")]
public abstract class AdminControllerBase : Controller
{
    protected readonly ILogger _logger;
    protected readonly IActivityLogService _activityLog;
    
    protected AdminControllerBase(ILogger logger, IActivityLogService activityLog)
    {
        _logger = logger;
        _activityLog = activityLog;
    }
    
    protected bool IsHtmxRequest()
    {
        return Request.Headers.ContainsKey("HX-Request");
    }
    
    protected IActionResult HtmxRedirect(string url)
    {
        Response.Headers["HX-Redirect"] = url;
        return Ok();
    }
    
    protected IActionResult PartialOrFull(string viewName, object model)
    {
        if (IsHtmxRequest())
        {
            return PartialView($"_{viewName}Partial", model);
        }
        return View(viewName, model);
    }
    
    protected async Task LogActivityAsync(string action, string entityType, long? entityId = null, object metadata = null)
    {
        await _activityLog.LogAsync(new ActivityLogEntry
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = GetCurrentUserId(),
            Metadata = metadata,
            Timestamp = DateTime.UtcNow
        });
    }
    
    protected void AddSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
        
        if (IsHtmxRequest())
        {
            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new
            {
                showToast = new { message, type = "success" }
            });
        }
    }
    
    protected void AddErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
        
        if (IsHtmxRequest())
        {
            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new
            {
                showToast = new { message, type = "error" }
            });
        }
    }
    
    protected override void OnActionExecuting(ActionExecutingContext context)
    {
        // Add common view data
        ViewBag.CurrentUser = GetCurrentUser();
        ViewBag.Environment = _hostEnvironment.EnvironmentName;
        ViewBag.Version = GetApplicationVersion();
        
        base.OnActionExecuting(context);
    }
}
```

`EventsController.cs` (Complete):
```csharp
[Route("admin/events")]
public class EventsController : AdminControllerBase
{
    private readonly IEventService _eventService;
    private readonly IAdminEventService _adminEventService;
    private readonly IMapper _mapper;
    
    // List Actions
    [HttpGet("")]
    public async Task<IActionResult> Index(EventListRequest request)
    {
        var result = await _adminEventService.GetPagedEventsAsync(request);
        var model = new EventListViewModel
        {
            Items = result,
            Filters = request,
            CurrentSort = new SortInfo(request.SortBy, request.SortOrder)
        };
        
        return View(model);
    }
    
    [HttpGet("list-partial")]
    public async Task<IActionResult> ListPartial(EventListRequest request)
    {
        var result = await _adminEventService.GetPagedEventsAsync(request);
        return PartialView("_EventList", result);
    }
    
    // Detail Actions
    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var eventDetails = await _adminEventService.GetEventDetailsAsync(slug);
        if (eventDetails == null)
            return NotFound();
            
        await LogActivityAsync("ViewDetails", "Event", eventDetails.Event.Id);
        
        return View(eventDetails);
    }
    
    // Create Actions
    [HttpGet("create")]
    public IActionResult Create()
    {
        var model = new EventFormViewModel
        {
            Event = new EventFormModel { EventDate = DateTime.Now.AddDays(30) },
            States = GetStatesList()
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
            var stub = _mapper.Map<EventStub>(model);
            var created = await _eventService.CreateEventAsync(stub);
            
            await LogActivityAsync("Create", "Event", created.Id, model);
            AddSuccessMessage($"Event '{created.Title}' created successfully!");
            
            return HtmxRedirect($"/admin/events/{created.Slug}");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return HandleFormError(model, isNew: true);
        }
    }
    
    // Edit Actions
    [HttpGet("{slug}/edit")]
    public async Task<IActionResult> Edit(string slug)
    {
        var eventRecord = await _eventService.GetBySlugAsync(slug);
        if (eventRecord == null)
            return NotFound();
            
        var model = new EventFormViewModel
        {
            Event = _mapper.Map<EventFormModel>(eventRecord),
            States = GetStatesList()
        };
        
        return View("Form", model);
    }
    
    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, EventFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return HandleFormError(model, isNew: false);
        }
        
        try
        {
            var stub = _mapper.Map<EventStub>(model);
            var updated = await _eventService.UpdateEventAsync(id, stub);
            
            await LogActivityAsync("Update", "Event", id, model);
            AddSuccessMessage("Event updated successfully!");
            
            return HtmxRedirect($"/admin/events/{updated.Slug}");
        }
        catch (ConcurrencyException)
        {
            AddErrorMessage("The event was modified by another user. Please refresh and try again.");
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
            await LogActivityAsync("Delete", "Event", id);
            
            AddSuccessMessage("Event deleted successfully!");
            return HtmxRedirect("/admin/events");
        }
        catch (BusinessException ex)
        {
            AddErrorMessage(ex.Message);
            return BadRequest();
        }
    }
    
    // Bulk Actions
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete(List<long> ids)
    {
        var result = await _adminEventService.BulkDeleteEventsAsync(ids);
        
        if (result.SuccessCount > 0)
        {
            AddSuccessMessage($"Successfully deleted {result.SuccessCount} events.");
        }
        
        if (result.Failures.Any())
        {
            AddErrorMessage($"Failed to delete {result.Failures.Count} events.");
        }
        
        return PartialView("_EventList", await GetCurrentListAsync());
    }
    
    // Export Actions
    [HttpGet("export")]
    public async Task<IActionResult> Export(EventListRequest request)
    {
        var data = await _adminEventService.ExportEventsAsync(request);
        var csv = CsvSerializer.Serialize(data);
        
        return File(
            Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"events_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        );
    }
    
    // HTMX Partial Updates
    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> RefreshStatistics(long id)
    {
        var stats = await _adminEventService.GetEventStatisticsAsync(id);
        return PartialView("_EventStatistics", stats);
    }
    
    [HttpGet("{id}/invitations")]
    public async Task<IActionResult> InvitationsPartial(long id, int page = 1)
    {
        var invitations = await _adminEventService.GetEventInvitationsAsync(id, page);
        return PartialView("_InvitationsList", invitations);
    }
    
    // Inline Edit
    [HttpPost("{id}/field")]
    public async Task<IActionResult> UpdateField(long id, string field, string value)
    {
        try
        {
            var updated = await _adminEventService.UpdateFieldAsync(id, field, value);
            await LogActivityAsync("UpdateField", "Event", id, new { field, value });
            
            return Json(new { success = true, value = updated });
        }
        catch (ValidationException ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
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
            States = GetStatesList(),
            IsNew = isNew
        };
        
        return View("Form", viewModel);
    }
    
    private SelectList GetStatesList()
    {
        return new SelectList(StateHelper.GetStates(), "Value", "Text");
    }
}
```

### Step 3: Create Admin View Models and DTOs
Location: `/SocialAnimal.Web/Areas/Admin/Models/`

Define comprehensive models:

`ViewModels/BaseListViewModel.cs`:
```csharp
public abstract class BaseListViewModel<T>
{
    public PagedResult<T> Items { get; set; }
    public Dictionary<string, string> CurrentFilters { get; set; }
    public SortInfo CurrentSort { get; set; }
    public bool HasFilters => CurrentFilters?.Any() ?? false;
    public string SearchTerm { get; set; }
    
    public string GetSortUrl(string field)
    {
        var direction = CurrentSort?.Field == field && CurrentSort?.Direction == "asc" 
            ? "desc" : "asc";
        return $"?sortBy={field}&sortOrder={direction}";
    }
    
    public string GetFilterUrl(string key, string value)
    {
        var filters = new Dictionary<string, string>(CurrentFilters ?? new());
        filters[key] = value;
        return "?" + string.Join("&", filters.Select(f => $"{f.Key}={f.Value}"));
    }
}

public class EventListViewModel : BaseListViewModel<EventListItemDto>
{
    public EventListFilters Filters { get; set; }
    public List<SelectListItem> StateOptions { get; set; }
    public List<SelectListItem> StatusOptions { get; set; }
}
```

`DTOs/AdminDtos.cs`:
```csharp
public class EventListItemDto
{
    public long Id { get; set; }
    public string Slug { get; set; }
    public string Title { get; set; }
    public string Location { get; set; }
    public DateTime? EventDate { get; set; }
    public EventStatus Status { get; set; }
    public int InvitationCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int PendingCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedOn { get; set; }
    
    public string StatusClass => Status switch
    {
        EventStatus.Upcoming => "badge-success",
        EventStatus.InProgress => "badge-primary",
        EventStatus.Completed => "badge-secondary",
        EventStatus.Cancelled => "badge-danger",
        _ => "badge-light"
    };
}

public class DashboardSummaryDto
{
    public EntityStatsDto EventStats { get; set; }
    public EntityStatsDto UserStats { get; set; }
    public EntityStatsDto InvitationStats { get; set; }
    public EntityStatsDto ReservationStats { get; set; }
    public List<AlertDto> SystemAlerts { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class BulkOperationResult
{
    public int RequestedCount { get; set; }
    public int SuccessCount { get; set; }
    public List<BulkOperationFailure> Failures { get; set; } = new();
    public TimeSpan Duration { get; set; }
    
    public bool IsComplete => SuccessCount == RequestedCount;
    public bool HasFailures => Failures.Any();
    public double SuccessRate => RequestedCount > 0 
        ? (double)SuccessCount / RequestedCount : 0;
}
```

### Step 4: Configure Dependency Injection
Location: `/SocialAnimal.Web/Configuration/Modules/AdminModule.cs`

Create admin-specific DI configuration:
```csharp
public class AdminModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Admin Services
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminReportService, AdminReportService>();
        services.AddScoped<IAdminBulkOperationService, AdminBulkOperationService>();
        services.AddScoped<IAdminEventService, AdminEventService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminInvitationService, AdminInvitationService>();
        services.AddScoped<IAdminReservationService, AdminReservationService>();
        
        // Activity Logging
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IAuditService, AuditService>();
        
        // Caching
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "SocialAnimalAdmin";
        });
        
        // Background Services
        services.AddHostedService<CacheWarmupService>();
        services.AddHostedService<ReportSchedulerService>();
        
        // AutoMapper
        services.AddAutoMapper(typeof(AdminMappingProfile));
        
        // MediatR for complex operations
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(AdminModule).Assembly));
        
        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<EventFormValidator>();
        
        // Admin-specific options
        services.Configure<AdminOptions>(configuration.GetSection("Admin"));
    }
}
```

Update `Program.cs`:
```csharp
// Register all modules
builder.Services.RegisterModules(
    typeof(CoreModule),
    typeof(InfrastructureModule),
    typeof(RepositoryModule),
    typeof(ServiceModule),
    typeof(AdminModule)
);

// Configure admin area
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AdminExceptionFilter>();
    options.Filters.Add<AdminActivityLogFilter>();
})
.AddRazorOptions(options =>
{
    options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
    options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new InstantJsonConverter());
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));
    
    options.AddPolicy("AdminRead", policy =>
        policy.RequireRole("Admin", "SuperAdmin", "ReadOnlyAdmin"));
});
```

### Step 5: Implement Error Handling Middleware
Location: `/SocialAnimal.Web/Infrastructure/Middleware/AdminErrorHandlingMiddleware.cs`

Create comprehensive error handling:
```csharp
public class AdminErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in admin area");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var isHtmx = context.Request.Headers.ContainsKey("HX-Request");
        var errorId = Guid.NewGuid().ToString("N");
        
        var (statusCode, message) = exception switch
        {
            NotFoundException => (404, "The requested resource was not found."),
            UnauthorizedException => (401, "You are not authorized to access this resource."),
            ValidationException => (400, exception.Message),
            BusinessException => (400, exception.Message),
            ConcurrencyException => (409, "The resource was modified by another user."),
            _ => (500, "An unexpected error occurred.")
        };
        
        context.Response.StatusCode = statusCode;
        
        if (isHtmx)
        {
            context.Response.Headers["HX-Retarget"] = "#error-container";
            context.Response.Headers["HX-Reswap"] = "innerHTML";
            
            await context.Response.WriteAsync(
                $"<div class='alert alert-danger'>Error {errorId}: {message}</div>"
            );
        }
        else if (context.Request.Path.StartsWithSegments("/admin"))
        {
            context.Response.Redirect($"/admin/error?id={errorId}&code={statusCode}");
        }
        else
        {
            await WriteJsonErrorResponse(context, statusCode, message, errorId);
        }
        
        // Log detailed error
        LogError(exception, errorId, context);
    }
    
    private void LogError(Exception exception, string errorId, HttpContext context)
    {
        _logger.LogError(exception, 
            "Error {ErrorId} occurred. Path: {Path}, User: {User}", 
            errorId,
            context.Request.Path,
            context.User?.Identity?.Name ?? "Anonymous");
    }
}
```

### Step 6: Create Admin Filters
Location: `/SocialAnimal.Web/Infrastructure/Filters/`

`AdminExceptionFilter.cs`:
```csharp
public class AdminExceptionFilter : IExceptionFilter
{
    private readonly ILogger<AdminExceptionFilter> _logger;
    
    public void OnException(ExceptionContext context)
    {
        if (!context.HttpContext.Request.Path.StartsWithSegments("/admin"))
            return;
            
        _logger.LogError(context.Exception, "Admin area exception");
        
        if (context.HttpContext.Request.Headers.ContainsKey("HX-Request"))
        {
            context.Result = new PartialViewResult
            {
                ViewName = "_Error",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState)
                {
                    Model = new ErrorViewModel
                    {
                        Message = GetUserFriendlyMessage(context.Exception),
                        RequestId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier
                    }
                }
            };
            context.ExceptionHandled = true;
        }
    }
}
```

`AdminActivityLogFilter.cs`:
```csharp
public class AdminActivityLogFilter : IAsyncActionFilter
{
    private readonly IActivityLogService _activityLog;
    
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, 
        ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var result = await next();
        
        stopwatch.Stop();
        
        if (context.HttpContext.Request.Path.StartsWithSegments("/admin"))
        {
            await _activityLog.LogAsync(new ActivityLogEntry
            {
                Action = context.ActionDescriptor.DisplayName,
                Controller = context.Controller.GetType().Name,
                UserId = context.HttpContext.User?.GetUserId(),
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                Duration = stopwatch.ElapsedMilliseconds,
                Timestamp = DateTime.UtcNow,
                Success = result.Exception == null
            });
        }
    }
}
```

### Step 7: Create Admin Options Configuration
Location: `/SocialAnimal.Web/Configuration/AdminOptions.cs`

Define configuration options:
```csharp
public class AdminOptions
{
    public int PageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
    public int CacheExpirationMinutes { get; set; } = 5;
    public bool EnableActivityLogging { get; set; } = true;
    public bool EnableAutoSave { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    public List<string> AllowedFileExtensions { get; set; } = new() { ".csv", ".xlsx", ".pdf" };
    public long MaxFileUploadSize { get; set; } = 10_485_760; // 10MB
    public Dictionary<string, int> RateLimits { get; set; } = new()
    {
        ["BulkOperations"] = 10,
        ["Exports"] = 20,
        ["Reports"] = 5
    };
}
```

Add to `appsettings.json`:
```json
{
  "Admin": {
    "PageSize": 20,
    "MaxPageSize": 100,
    "CacheExpirationMinutes": 5,
    "EnableActivityLogging": true,
    "EnableAutoSave": true,
    "AutoSaveIntervalSeconds": 30,
    "RateLimits": {
      "BulkOperations": 10,
      "Exports": 20,
      "Reports": 5
    }
  }
}
```

### Step 8: Implement Caching Strategy
Location: `/SocialAnimal.Infrastructure/Caching/AdminCacheService.cs`

Create caching service:
```csharp
public interface IAdminCacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    Task InvalidateAsync(string pattern);
    Task InvalidateEntityAsync(string entityType, long? entityId = null);
}

public class AdminCacheService : IAdminCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AdminCacheService> _logger;
    
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        // Try memory cache first
        if (_memoryCache.TryGetValue(key, out T cached))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cached;
        }
        
        // Try distributed cache
        var distributedValue = await _distributedCache.GetAsync(key);
        if (distributedValue != null)
        {
            var deserialized = JsonSerializer.Deserialize<T>(distributedValue);
            _memoryCache.Set(key, deserialized, TimeSpan.FromMinutes(1));
            return deserialized;
        }
        
        // Generate value
        var value = await factory();
        
        // Store in both caches
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiration ?? TimeSpan.FromMinutes(5)
        };
        _memoryCache.Set(key, value, options);
        
        await _distributedCache.SetAsync(
            key,
            JsonSerializer.SerializeToUtf8Bytes(value),
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = expiration ?? TimeSpan.FromMinutes(5)
            });
        
        return value;
    }
    
    public async Task InvalidateEntityAsync(string entityType, long? entityId = null)
    {
        var pattern = entityId.HasValue 
            ? $"admin:{entityType.ToLower()}:{entityId}:*"
            : $"admin:{entityType.ToLower()}:*";
            
        await InvalidateAsync(pattern);
    }
}
```

### Step 9: Add Telemetry and Monitoring
Location: `/SocialAnimal.Web/Infrastructure/Telemetry/AdminTelemetry.cs`

Implement telemetry:
```csharp
public class AdminTelemetry
{
    private readonly ILogger<AdminTelemetry> _logger;
    private readonly IMetrics _metrics;
    
    public void RecordPageView(string page, string user)
    {
        _metrics.Measure.Counter.Increment("admin.page_views", new { page, user });
    }
    
    public void RecordOperation(string operation, bool success, long duration)
    {
        _metrics.Measure.Counter.Increment("admin.operations", new { operation, success });
        _metrics.Measure.Histogram.Update("admin.operation_duration", duration, new { operation });
    }
    
    public void RecordError(string area, Exception exception)
    {
        _metrics.Measure.Counter.Increment("admin.errors", new { area, type = exception.GetType().Name });
        _logger.LogError(exception, "Admin error in {Area}", area);
    }
}
```

### Step 10: Create Admin API Endpoints
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/Api/`

Add API controllers for AJAX/HTMX:
```csharp
[ApiController]
[Area("Admin")]
[Route("admin/api/[controller]")]
public class StatsApiController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;
    
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _dashboardService.GetDashboardSummaryAsync();
        return Ok(summary);
    }
    
    [HttpGet("chart/{type}")]
    public async Task<IActionResult> GetChartData(string type, [FromQuery] DateRange range)
    {
        var data = await _dashboardService.GetChartDataAsync(type, range);
        return Ok(data);
    }
}
```

## Testing Checklist

- [ ] All controllers handle requests correctly
- [ ] Services execute business logic properly
- [ ] View models map correctly
- [ ] DTOs serialize/deserialize properly
- [ ] Dependency injection resolves all services
- [ ] Error handling catches all exceptions
- [ ] Logging captures relevant information
- [ ] Caching improves performance
- [ ] Activity logging tracks operations
- [ ] Authorization enforces access control
- [ ] Validation works at all layers
- [ ] Telemetry collects metrics

## Performance Optimization

1. **Caching Strategy**:
   - Cache dashboard data
   - Cache list queries
   - Invalidate on updates
   - Use distributed cache for scale

2. **Query Optimization**:
   - Use projections
   - Implement pagination
   - Add database indexes
   - Use compiled queries

3. **Response Optimization**:
   - Compress responses
   - Minimize payload size
   - Use CDN for assets
   - Implement ETags

## Security Considerations

1. **Authorization**: Enforce role-based access
2. **Input Validation**: Validate all inputs
3. **CSRF Protection**: Use anti-forgery tokens
4. **Rate Limiting**: Limit expensive operations
5. **Audit Logging**: Log all admin actions
6. **Data Privacy**: Respect PII handling

## Dependencies

This task depends on:
- Tasks 8-14: All previous implementations

This task completes the admin portal implementation.

## Notes

- Consider implementing feature flags
- Add A/B testing capabilities
- Implement admin notifications
- Add real-time updates with SignalR
- Consider implementing webhooks
- Add integration with external services
- Implement backup/restore functionality
- Add data archival processes
- Consider implementing approval workflows
- Add support for plugins/extensions
- Implement multi-tenancy if needed
- Add support for custom dashboards
- Consider implementing SSO
- Add API documentation with Swagger
- Implement health checks