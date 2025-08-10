# Task 12: Create Entity List Views

## Objective
Implement comprehensive list views for all entities (Events, Users, Invitations, Reservations) with pagination, filtering, searching, and sorting capabilities. These views will use HTMX for dynamic updates without full page refreshes.

## Requirements
- Create list views for all four entity types
- Implement server-side pagination
- Add filtering capabilities per entity type
- Implement search functionality
- Add multi-column sorting
- Use HTMX for dynamic updates
- Include bulk actions support
- Ensure responsive table design

## Implementation Steps

### Step 1: Create List Controllers
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/`

Create controllers for each entity:
- `EventsController.cs`
- `UsersController.cs`
- `InvitationsController.cs`
- `ReservationsController.cs`

Each controller should have:
```csharp
public class EventsController : AdminControllerBase
{
    // List action with filtering
    public async Task<IActionResult> Index(
        string search = null,
        string sortBy = "CreatedOn",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 20,
        bool includeDeleted = false)
    
    // Partial update for HTMX
    public async Task<IActionResult> ListPartial(EventListRequest request)
    
    // Export action
    public async Task<IActionResult> Export(EventListRequest request)
    
    // Bulk actions
    public async Task<IActionResult> BulkDelete(List<long> ids)
    public async Task<IActionResult> BulkRestore(List<long> ids)
}
```

### Step 2: Create List View Models
Location: `/SocialAnimal.Web/Areas/Admin/Models/ViewModels/`

Define view models for each entity list:

```csharp
public class EventListViewModel
{
    public PagedResult<EventListItem> Items { get; set; }
    public EventListFilters Filters { get; set; }
    public ListSortOptions SortOptions { get; set; }
    public Dictionary<string, string> CurrentFilters { get; set; }
    public bool CanBulkEdit { get; set; }
}

public class EventListItem
{
    public long Id { get; set; }
    public string Slug { get; set; }
    public string Title { get; set; }
    public string Location { get; set; } // City, State
    public DateTime? EventDate { get; set; }
    public int InvitationCount { get; set; }
    public int AttendeeCount { get; set; }
    public string Status { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class EventListFilters
{
    public string State { get; set; }
    public string City { get; set; }
    public DateRange DateRange { get; set; }
    public EventStatus? Status { get; set; }
    public bool IncludeDeleted { get; set; }
}
```

### Step 3: Create Events List View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Events/Index.cshtml`

Implement the events list page:
```html
@model EventListViewModel
@{
    ViewData["Title"] = "Events";
}

<div class="page-header">
    <h1>Events</h1>
    <div class="page-actions">
        <a href="/admin/events/create" class="btn btn-primary">
            <i class="icon-plus"></i> Create Event
        </a>
        <button onclick="exportList()" class="btn btn-secondary">
            <i class="icon-download"></i> Export
        </button>
    </div>
</div>

<div class="list-container">
    <!-- Filters Panel -->
    <aside class="filters-panel" id="filters">
        @await Html.PartialAsync("_EventFilters", Model.Filters)
    </aside>
    
    <!-- Main List Area -->
    <main class="list-content">
        <!-- Search Bar -->
        <div class="search-bar">
            <input type="text" 
                   placeholder="Search events..."
                   name="search"
                   hx-get="/admin/events/list-partial"
                   hx-trigger="keyup changed delay:500ms"
                   hx-target="#event-list"
                   hx-include="#filters">
        </div>
        
        <!-- Bulk Actions Bar -->
        <div class="bulk-actions" style="display:none;">
            <span class="selected-count">0 selected</span>
            <button onclick="bulkDelete()" class="btn btn-danger btn-sm">
                Delete Selected
            </button>
            <button onclick="bulkExport()" class="btn btn-secondary btn-sm">
                Export Selected
            </button>
        </div>
        
        <!-- Results Table -->
        <div id="event-list">
            @await Html.PartialAsync("_EventList", Model)
        </div>
    </main>
</div>
```

### Step 4: Create List Partial View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Events/_EventList.cshtml`

Create the table partial that HTMX will update:
```html
@model EventListViewModel

<div class="table-container">
    <table class="data-table">
        <thead>
            <tr>
                <th class="checkbox-column">
                    <input type="checkbox" id="select-all">
                </th>
                <th class="sortable" 
                    hx-get="/admin/events/list-partial?sortBy=Title"
                    hx-target="#event-list"
                    hx-include="#filters">
                    Title
                    @if(Model.SortOptions.SortBy == "Title")
                    {
                        <i class="sort-icon @Model.SortOptions.Direction"></i>
                    }
                </th>
                <th>Location</th>
                <th class="sortable"
                    hx-get="/admin/events/list-partial?sortBy=EventDate"
                    hx-target="#event-list"
                    hx-include="#filters">
                    Date
                </th>
                <th>Invitations</th>
                <th>Attendees</th>
                <th>Status</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach(var item in Model.Items.Data)
            {
                <tr class="@(item.IsDeleted ? "deleted-row" : "")"
                    data-id="@item.Id">
                    <td>
                        <input type="checkbox" 
                               class="row-select" 
                               value="@item.Id">
                    </td>
                    <td>
                        <a href="/admin/events/@item.Slug"
                           hx-boost="true">
                            @item.Title
                        </a>
                    </td>
                    <td>@item.Location</td>
                    <td>
                        <time datetime="@item.EventDate">
                            @item.EventDate?.ToString("MMM dd, yyyy")
                        </time>
                    </td>
                    <td>
                        <span class="badge">@item.InvitationCount</span>
                    </td>
                    <td>
                        <span class="badge badge-success">@item.AttendeeCount</span>
                    </td>
                    <td>
                        <span class="status-badge @item.Status.ToLower()">
                            @item.Status
                        </span>
                    </td>
                    <td class="actions">
                        <div class="action-menu">
                            <a href="/admin/events/@item.Slug/edit"
                               class="btn-icon"
                               title="Edit">
                                <i class="icon-edit"></i>
                            </a>
                            <button class="btn-icon"
                                    hx-delete="/admin/events/@item.Id"
                                    hx-confirm="Delete this event?"
                                    title="Delete">
                                <i class="icon-trash"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            }
        </tbody>
    </table>
    
    <!-- Pagination -->
    @await Html.PartialAsync("_Pagination", Model.Items)
</div>
```

### Step 5: Create Filter Components
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_EventFilters.cshtml`

Implement filter panel:
```html
@model EventListFilters

<form id="filter-form" 
      hx-get="/admin/events/list-partial"
      hx-target="#event-list"
      hx-trigger="change">
    
    <div class="filter-group">
        <label>Status</label>
        <select name="status">
            <option value="">All</option>
            <option value="upcoming">Upcoming</option>
            <option value="past">Past</option>
            <option value="cancelled">Cancelled</option>
        </select>
    </div>
    
    <div class="filter-group">
        <label>State</label>
        <select name="state">
            <option value="">All States</option>
            @foreach(var state in ViewBag.States)
            {
                <option value="@state">@state</option>
            }
        </select>
    </div>
    
    <div class="filter-group">
        <label>Date Range</label>
        <input type="date" name="dateFrom" value="@Model.DateRange?.From">
        <input type="date" name="dateTo" value="@Model.DateRange?.To">
    </div>
    
    <div class="filter-group">
        <label>
            <input type="checkbox" 
                   name="includeDeleted" 
                   value="true"
                   @(Model.IncludeDeleted ? "checked" : "")>
            Include Deleted
        </label>
    </div>
    
    <button type="button" 
            onclick="clearFilters()"
            class="btn btn-link">
        Clear Filters
    </button>
</form>
```

### Step 6: Create Pagination Component
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_Pagination.cshtml`

Create reusable pagination:
```html
@model PagedResult<T>

<div class="pagination-container">
    <div class="pagination-info">
        Showing @Model.StartItem - @Model.EndItem of @Model.TotalCount
    </div>
    
    <nav class="pagination">
        @if(Model.HasPrevious)
        {
            <a href="#"
               hx-get="@Url.Action("ListPartial", new { page = Model.Page - 1 })"
               hx-target="#event-list"
               hx-include="#filters"
               class="page-link">
                Previous
            </a>
        }
        
        @for(int i = Model.StartPage; i <= Model.EndPage; i++)
        {
            <a href="#"
               hx-get="@Url.Action("ListPartial", new { page = i })"
               hx-target="#event-list"
               hx-include="#filters"
               class="page-link @(i == Model.Page ? "active" : "")">
                @i
            </a>
        }
        
        @if(Model.HasNext)
        {
            <a href="#"
               hx-get="@Url.Action("ListPartial", new { page = Model.Page + 1 })"
               hx-target="#event-list"
               hx-include="#filters"
               class="page-link">
                Next
            </a>
        }
    </nav>
    
    <div class="page-size-selector">
        <label>Show:</label>
        <select hx-get="/admin/events/list-partial"
                hx-target="#event-list"
                hx-include="#filters"
                name="pageSize">
            <option value="10">10</option>
            <option value="20" selected>20</option>
            <option value="50">50</option>
            <option value="100">100</option>
        </select>
    </div>
</div>
```

### Step 7: Create Users List View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Users/Index.cshtml`

Similar structure to events but with user-specific fields:
- Name (with slug)
- Phone number
- Registration date
- Reservation count
- Last activity
- Status (active/deleted)

User-specific filters:
- Has reservations
- Registration date range
- Phone area code
- Activity level

### Step 8: Create Invitations List View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Invitations/Index.cshtml`

Invitation-specific columns:
- Guest name
- Event title (linked)
- Contact info (email/phone)
- Max party size
- Sent status
- RSVP status
- Created date

Invitation-specific filters:
- Event (dropdown)
- Sent/Unsent
- Has RSVP
- Contact type (email/phone)

### Step 9: Create Reservations List View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Reservations/Index.cshtml`

Reservation-specific columns:
- Guest name (from invitation)
- Event title (linked)
- Party size
- Status (attending/regrets)
- Created date
- Notes

Reservation-specific filters:
- Event (dropdown)
- Attending/Regrets
- Party size range
- Date range

### Step 10: Implement List Services
Location: `/SocialAnimal.Infrastructure/Services/ListServices/`

Create service for complex list queries:
```csharp
public class EventListService : IEventListService
{
    public async Task<PagedResult<EventListItem>> GetPagedEventsAsync(
        EventListRequest request)
    {
        var query = BuildQuery(request);
        var totalCount = await query.CountAsync();
        
        var items = await query
            .OrderBy(request.SortBy, request.SortDirection)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EventListItem
            {
                // Map properties
            })
            .ToListAsync();
            
        return new PagedResult<EventListItem>
        {
            Data = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
    
    private IQueryable<Event> BuildQuery(EventListRequest request)
    {
        var query = _context.Events.AsQueryable();
        
        if (!request.IncludeDeleted)
            query = query.Where(e => e.DeletedAt == null);
            
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(e => 
                e.Title.Contains(request.Search) ||
                e.City.Contains(request.Search));
                
        // Apply other filters
        
        return query;
    }
}
```

### Step 11: Add List JavaScript
Location: `/SocialAnimal.Web/wwwroot/js/admin/lists.js`

Implement list interactions:
```javascript
// Bulk selection
document.addEventListener('DOMContentLoaded', function() {
    // Select all checkbox
    const selectAll = document.getElementById('select-all');
    selectAll?.addEventListener('change', function() {
        const checkboxes = document.querySelectorAll('.row-select');
        checkboxes.forEach(cb => cb.checked = this.checked);
        updateBulkActions();
    });
    
    // Individual checkboxes
    document.addEventListener('change', function(e) {
        if (e.target.classList.contains('row-select')) {
            updateBulkActions();
        }
    });
});

function updateBulkActions() {
    const selected = document.querySelectorAll('.row-select:checked');
    const bulkActions = document.querySelector('.bulk-actions');
    const count = document.querySelector('.selected-count');
    
    if (selected.length > 0) {
        bulkActions.style.display = 'flex';
        count.textContent = `${selected.length} selected`;
    } else {
        bulkActions.style.display = 'none';
    }
}

function bulkDelete() {
    const selected = Array.from(document.querySelectorAll('.row-select:checked'))
        .map(cb => cb.value);
        
    if (confirm(`Delete ${selected.length} items?`)) {
        // Send HTMX request
        htmx.ajax('POST', '/admin/events/bulk-delete', {
            values: { ids: selected },
            target: '#event-list'
        });
    }
}

function clearFilters() {
    document.getElementById('filter-form').reset();
    htmx.trigger('#filter-form', 'submit');
}

function exportList() {
    const filters = new FormData(document.getElementById('filter-form'));
    const params = new URLSearchParams(filters);
    window.location.href = `/admin/events/export?${params}`;
}
```

### Step 12: Add List Styling
Location: `/SocialAnimal.Web/wwwroot/css/admin/lists.css`

Style the list views:
```css
.list-container {
    display: grid;
    grid-template-columns: 250px 1fr;
    gap: var(--spacing-lg);
}

.filters-panel {
    background: white;
    border-radius: var(--border-radius);
    padding: var(--spacing-md);
    height: fit-content;
    position: sticky;
    top: var(--spacing-md);
}

.data-table {
    width: 100%;
    border-collapse: collapse;
    background: white;
    border-radius: var(--border-radius);
    overflow: hidden;
}

.data-table th {
    background: var(--color-gray-50);
    padding: var(--spacing-sm) var(--spacing-md);
    text-align: left;
    font-weight: 600;
    color: var(--color-gray-700);
    border-bottom: 2px solid var(--color-gray-200);
}

.data-table td {
    padding: var(--spacing-sm) var(--spacing-md);
    border-bottom: 1px solid var(--color-gray-100);
}

.sortable {
    cursor: pointer;
    user-select: none;
}

.sortable:hover {
    background: var(--color-gray-100);
}

.deleted-row {
    opacity: 0.6;
    background: var(--color-gray-50);
}

.bulk-actions {
    display: flex;
    align-items: center;
    gap: var(--spacing-md);
    padding: var(--spacing-sm);
    background: var(--color-info-50);
    border-radius: var(--border-radius);
    margin-bottom: var(--spacing-md);
}

/* Responsive */
@media (max-width: 768px) {
    .list-container {
        grid-template-columns: 1fr;
    }
    
    .filters-panel {
        position: relative;
    }
    
    .data-table {
        font-size: 0.875rem;
    }
    
    .checkbox-column,
    .actions {
        display: none;
    }
}
```

### Step 13: Implement Empty States
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_EmptyState.cshtml`

Create empty state component:
```html
@model EmptyStateViewModel

<div class="empty-state">
    <div class="empty-state-icon">
        <i class="@Model.Icon"></i>
    </div>
    <h3>@Model.Title</h3>
    <p>@Model.Description</p>
    @if(!string.IsNullOrEmpty(Model.ActionUrl))
    {
        <a href="@Model.ActionUrl" class="btn btn-primary">
            @Model.ActionText
        </a>
    }
</div>
```

### Step 14: Add Export Functionality
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/EventsController.cs`

Implement export actions:
```csharp
public async Task<IActionResult> Export(EventListRequest request)
{
    var data = await _eventListService.GetExportDataAsync(request);
    
    var csv = GenerateCsv(data);
    var bytes = Encoding.UTF8.GetBytes(csv);
    
    return File(bytes, "text/csv", $"events_{DateTime.Now:yyyyMMdd}.csv");
}
```

## Testing Checklist

- [ ] All list views load correctly
- [ ] Pagination works properly
- [ ] Sorting updates without page refresh
- [ ] Filters apply correctly
- [ ] Search works across relevant fields
- [ ] Bulk selection functions properly
- [ ] Bulk actions execute correctly
- [ ] Export generates valid files
- [ ] Empty states display when appropriate
- [ ] Responsive design works on mobile
- [ ] Performance acceptable with large datasets
- [ ] HTMX updates maintain scroll position

## Performance Optimization

1. **Database Queries**:
   - Use indexes on sortable columns
   - Optimize search queries
   - Use projection for list items
   - Implement query result caching

2. **Frontend**:
   - Virtual scrolling for large lists
   - Debounce search input
   - Lazy load images if present
   - Use CSS containment

3. **Caching**:
   - Cache filter options
   - Cache count queries
   - Use ETags for list results

## Accessibility Requirements

1. **Table Structure**: Use proper table markup
2. **Sort Indicators**: Clear visual and ARIA labels
3. **Keyboard Navigation**: Support keyboard shortcuts
4. **Screen Readers**: Announce updates
5. **Focus Management**: Maintain focus after updates
6. **Loading States**: Announce loading status

## Dependencies

This task depends on:
- Task 8: Services for data retrieval
- Task 9: MVC infrastructure
- Task 10: Admin layout

This task must be completed before:
- Task 13: Detail views link from lists

## Notes

- Consider implementing saved filters
- Add column visibility toggle
- Support CSV and Excel exports
- Consider implementing infinite scroll
- Add quick edit in place
- Implement undo for bulk actions
- Consider adding list view preferences
- Support keyboard shortcuts
- Add print-friendly view
- Consider implementing faceted search
- Support deep linking to filtered states
- Add recently viewed items
- Consider implementing favorites/bookmarks
- Support batch operations queue
- Add audit log for bulk operations