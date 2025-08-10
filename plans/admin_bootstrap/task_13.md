# Task 13: Create Entity Detail Views

## Objective
Implement detailed view pages for individual entities (Events, Users, Invitations, Reservations) that display all entity properties, show related data, provide navigation between related entities, and support inline editing with HTMX.

## Requirements
- Create detail views for all four entity types
- Display all entity properties in organized sections
- Show related entities with navigation links
- Implement HTMX-based inline editing
- Add action buttons for entity operations
- Include audit trail/history information
- Ensure responsive layout for all screen sizes

## Implementation Steps

### Step 1: Create Detail Controllers Actions
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/`

Add detail actions to each controller:
```csharp
public class EventsController : AdminControllerBase
{
    // Detail view action
    public async Task<IActionResult> Details(string slug)
    {
        var eventDetails = await _eventService.GetEventDetailsAsync(slug);
        if (eventDetails == null)
            return NotFound();
            
        var model = new EventDetailsViewModel
        {
            Event = eventDetails,
            Invitations = await _invitationService.GetEventInvitationsAsync(eventDetails.Id),
            Statistics = await _eventService.GetEventStatisticsAsync(eventDetails.Id),
            AuditLog = await _auditService.GetEntityAuditLogAsync("Event", eventDetails.Id)
        };
        
        return View(model);
    }
    
    // Inline edit actions
    public async Task<IActionResult> EditField(long id, string field, string value)
    {
        var result = await _eventService.UpdateFieldAsync(id, field, value);
        if (result.Success)
            return PartialView("_FieldDisplay", result.Data);
        
        return BadRequest(result.Error);
    }
    
    // Related data partials
    public async Task<IActionResult> InvitationsPartial(long eventId, int page = 1)
    {
        var invitations = await _invitationService.GetPagedEventInvitationsAsync(eventId, page);
        return PartialView("_InvitationsList", invitations);
    }
}
```

### Step 2: Create Detail View Models
Location: `/SocialAnimal.Web/Areas/Admin/Models/ViewModels/`

Define comprehensive view models:
```csharp
public class EventDetailsViewModel
{
    public EventRecord Event { get; set; }
    public EventStatistics Statistics { get; set; }
    public List<InvitationSummary> RecentInvitations { get; set; }
    public List<AuditLogEntry> AuditLog { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class EventStatistics
{
    public int TotalInvitations { get; set; }
    public int SentInvitations { get; set; }
    public int TotalReservations { get; set; }
    public int ConfirmedAttendees { get; set; }
    public int Regrets { get; set; }
    public int PendingResponses { get; set; }
    public double ResponseRate { get; set; }
    public Dictionary<string, int> AttendeesByDay { get; set; }
}

public class InvitationSummary
{
    public long Id { get; set; }
    public string Slug { get; set; }
    public string GuestName { get; set; }
    public string ContactInfo { get; set; }
    public int MaxPartySize { get; set; }
    public bool IsSent { get; set; }
    public ReservationStatus? ResponseStatus { get; set; }
    public int? ActualPartySize { get; set; }
}
```

### Step 3: Create Event Details View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Events/Details.cshtml`

Implement comprehensive detail view:
```html
@model EventDetailsViewModel
@{
    ViewData["Title"] = Model.Event.Title;
}

<div class="detail-page">
    <!-- Page Header -->
    <div class="detail-header">
        <div class="breadcrumb">
            <a href="/admin">Dashboard</a> /
            <a href="/admin/events">Events</a> /
            <span>@Model.Event.Title</span>
        </div>
        
        <div class="detail-title-row">
            <h1>
                <span class="editable-field"
                      data-field="Title"
                      data-type="text"
                      hx-trigger="dblclick"
                      hx-get="/admin/events/@Model.Event.Id/edit-field?field=Title">
                    @Model.Event.Title
                </span>
            </h1>
            
            <div class="detail-actions">
                <a href="/admin/events/@Model.Event.Slug/edit" 
                   class="btn btn-primary">
                    <i class="icon-edit"></i> Edit
                </a>
                <button class="btn btn-secondary"
                        hx-post="/admin/events/@Model.Event.Id/duplicate">
                    <i class="icon-copy"></i> Duplicate
                </button>
                <button class="btn btn-danger"
                        hx-delete="/admin/events/@Model.Event.Id"
                        hx-confirm="Delete this event?">
                    <i class="icon-trash"></i> Delete
                </button>
            </div>
        </div>
        
        <!-- Status Badges -->
        <div class="status-badges">
            @if(Model.Event.DeletedAt.HasValue)
            {
                <span class="badge badge-danger">Deleted</span>
            }
            else if(Model.Event.EventDate < DateTime.Now)
            {
                <span class="badge badge-secondary">Past Event</span>
            }
            else
            {
                <span class="badge badge-success">Upcoming</span>
            }
            
            <span class="badge badge-info">
                @Model.Statistics.ConfirmedAttendees attendees
            </span>
        </div>
    </div>
    
    <!-- Main Content Grid -->
    <div class="detail-content">
        <!-- Left Column - Main Info -->
        <div class="detail-main">
            <!-- Event Information Card -->
            <div class="detail-card">
                <div class="card-header">
                    <h2>Event Information</h2>
                    <button class="btn-icon"
                            hx-get="/admin/events/@Model.Event.Id/edit-section?section=info"
                            hx-target="#event-info">
                        <i class="icon-edit"></i>
                    </button>
                </div>
                <div class="card-body" id="event-info">
                    <dl class="detail-list">
                        <dt>Event Date</dt>
                        <dd>
                            <span class="editable-field"
                                  data-field="EventDate"
                                  data-type="datetime">
                                @Model.Event.EventDate?.ToString("MMMM dd, yyyy h:mm tt")
                            </span>
                        </dd>
                        
                        <dt>Slug</dt>
                        <dd>
                            <code>@Model.Event.Slug</code>
                            <button class="btn-icon btn-sm"
                                    onclick="copyToClipboard('@Model.Event.Slug')">
                                <i class="icon-copy"></i>
                            </button>
                        </dd>
                        
                        <dt>Created</dt>
                        <dd>
                            <time datetime="@Model.Event.CreatedOn">
                                @Model.Event.CreatedOn.ToString("MMM dd, yyyy")
                            </time>
                        </dd>
                        
                        <dt>Last Modified</dt>
                        <dd>
                            <time datetime="@Model.Event.UpdatedOn">
                                @Model.Event.UpdatedOn?.ToString("MMM dd, yyyy") ?? "Never"
                            </time>
                        </dd>
                    </dl>
                </div>
            </div>
            
            <!-- Location Card -->
            <div class="detail-card">
                <div class="card-header">
                    <h2>Location</h2>
                </div>
                <div class="card-body">
                    <address class="event-address">
                        <div class="editable-field" 
                             data-field="AddressLine1">
                            @Model.Event.AddressLine1
                        </div>
                        @if(!string.IsNullOrEmpty(Model.Event.AddressLine2))
                        {
                            <div class="editable-field" 
                                 data-field="AddressLine2">
                                @Model.Event.AddressLine2
                            </div>
                        }
                        <div>
                            <span class="editable-field" data-field="City">@Model.Event.City</span>,
                            <span class="editable-field" data-field="State">@Model.Event.State</span>
                            <span class="editable-field" data-field="Postal">@Model.Event.Postal</span>
                        </div>
                    </address>
                    
                    <div class="map-preview" id="map">
                        <!-- Map component or image -->
                    </div>
                    
                    <a href="#" class="btn btn-link">
                        <i class="icon-map"></i> View on Map
                    </a>
                </div>
            </div>
            
            <!-- Invitations Card -->
            <div class="detail-card">
                <div class="card-header">
                    <h2>Invitations (@Model.Statistics.TotalInvitations)</h2>
                    <div class="card-actions">
                        <a href="/admin/invitations/create?eventId=@Model.Event.Id" 
                           class="btn btn-sm btn-primary">
                            Add Invitation
                        </a>
                        <a href="/admin/invitations?eventId=@Model.Event.Id" 
                           class="btn btn-sm btn-secondary">
                            View All
                        </a>
                    </div>
                </div>
                <div class="card-body" 
                     id="invitations-list"
                     hx-get="/admin/events/@Model.Event.Id/invitations-partial"
                     hx-trigger="load">
                    <div class="loading-skeleton">Loading invitations...</div>
                </div>
            </div>
        </div>
        
        <!-- Right Column - Statistics & Actions -->
        <div class="detail-sidebar">
            <!-- Statistics Card -->
            <div class="detail-card">
                <div class="card-header">
                    <h2>Statistics</h2>
                    <button class="btn-icon"
                            hx-get="/admin/events/@Model.Event.Id/refresh-stats"
                            hx-target="#event-stats">
                        <i class="icon-refresh"></i>
                    </button>
                </div>
                <div class="card-body" id="event-stats">
                    <div class="stat-grid">
                        <div class="stat-item">
                            <span class="stat-value">@Model.Statistics.TotalInvitations</span>
                            <span class="stat-label">Invitations</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-value">@Model.Statistics.SentInvitations</span>
                            <span class="stat-label">Sent</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-value">@Model.Statistics.ConfirmedAttendees</span>
                            <span class="stat-label">Attending</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-value">@Model.Statistics.Regrets</span>
                            <span class="stat-label">Regrets</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-value">@Model.Statistics.PendingResponses</span>
                            <span class="stat-label">Pending</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-value">@Model.Statistics.ResponseRate.ToString("P0")</span>
                            <span class="stat-label">Response Rate</span>
                        </div>
                    </div>
                    
                    <!-- Mini Chart -->
                    <div class="mini-chart" id="response-chart">
                        <canvas id="responseChart"></canvas>
                    </div>
                </div>
            </div>
            
            <!-- Quick Actions Card -->
            <div class="detail-card">
                <div class="card-header">
                    <h2>Quick Actions</h2>
                </div>
                <div class="card-body">
                    <div class="quick-actions-list">
                        <button class="action-item"
                                hx-post="/admin/events/@Model.Event.Id/send-invitations">
                            <i class="icon-send"></i>
                            Send All Invitations
                        </button>
                        <a href="/admin/reports/event/@Model.Event.Id" 
                           class="action-item">
                            <i class="icon-chart"></i>
                            View Report
                        </a>
                        <button class="action-item"
                                onclick="exportGuestList(@Model.Event.Id)">
                            <i class="icon-download"></i>
                            Export Guest List
                        </button>
                        <button class="action-item"
                                hx-post="/admin/events/@Model.Event.Id/send-reminders">
                            <i class="icon-bell"></i>
                            Send Reminders
                        </button>
                    </div>
                </div>
            </div>
            
            <!-- Audit Log Card -->
            <div class="detail-card">
                <div class="card-header">
                    <h2>Activity Log</h2>
                </div>
                <div class="card-body">
                    <div class="audit-log">
                        @foreach(var entry in Model.AuditLog.Take(10))
                        {
                            <div class="audit-entry">
                                <div class="audit-icon">
                                    <i class="icon-@entry.Action.ToLower()"></i>
                                </div>
                                <div class="audit-content">
                                    <p>@entry.Description</p>
                                    <time>@entry.Timestamp.ToString("MMM dd, h:mm tt")</time>
                                </div>
                            </div>
                        }
                    </div>
                    <a href="/admin/audit/event/@Model.Event.Id" class="btn btn-link">
                        View Full History
                    </a>
                </div>
            </div>
        </div>
    </div>
</div>
```

### Step 4: Create User Details View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Users/Details.cshtml`

User-specific detail sections:
- Profile information (name, slug, phone)
- Registration details
- Reservation history
- Event attendance record
- Contact preferences
- Account status

Include user-specific actions:
- Merge with another user
- Send notification
- Export user data
- View as user (impersonation)

### Step 5: Create Invitation Details View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Invitations/Details.cshtml`

Invitation-specific sections:
- Guest information
- Event details (linked)
- Contact information
- Invitation status (sent/pending)
- RSVP response if exists
- Send history
- Custom message if any

Invitation actions:
- Send/Resend invitation
- Edit guest info
- Change max party size
- Create reservation
- View/Edit RSVP

### Step 6: Create Reservation Details View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Reservations/Details.cshtml`

Reservation-specific sections:
- Guest information (from invitation)
- Event details (linked)
- Response details (party size, notes)
- User association if exists
- Attendance status
- Modification history

Reservation actions:
- Update party size
- Mark as attended
- Send confirmation
- Cancel reservation
- Add notes

### Step 7: Implement Inline Editing
Location: `/SocialAnimal.Web/wwwroot/js/admin/inline-edit.js`

Create inline editing functionality:
```javascript
// Initialize inline editing
document.addEventListener('DOMContentLoaded', function() {
    initializeInlineEditing();
});

function initializeInlineEditing() {
    // Add double-click handlers to editable fields
    document.querySelectorAll('.editable-field').forEach(field => {
        field.addEventListener('dblclick', function() {
            startInlineEdit(this);
        });
    });
}

function startInlineEdit(element) {
    const field = element.dataset.field;
    const type = element.dataset.type || 'text';
    const value = element.textContent.trim();
    
    // Create input based on type
    let input;
    switch(type) {
        case 'text':
            input = createTextInput(value);
            break;
        case 'select':
            input = createSelectInput(value, element.dataset.options);
            break;
        case 'datetime':
            input = createDateTimeInput(value);
            break;
        default:
            input = createTextInput(value);
    }
    
    // Replace element with input
    input.dataset.originalValue = value;
    input.dataset.field = field;
    element.replaceWith(input);
    input.focus();
    input.select();
    
    // Handle save/cancel
    input.addEventListener('blur', function() {
        saveInlineEdit(this);
    });
    
    input.addEventListener('keydown', function(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            saveInlineEdit(this);
        } else if (e.key === 'Escape') {
            cancelInlineEdit(this);
        }
    });
}

function saveInlineEdit(input) {
    const field = input.dataset.field;
    const value = input.value;
    const originalValue = input.dataset.originalValue;
    
    if (value === originalValue) {
        restoreDisplay(input, originalValue);
        return;
    }
    
    // Show loading state
    const loading = document.createElement('span');
    loading.className = 'inline-loading';
    loading.textContent = 'Saving...';
    input.replaceWith(loading);
    
    // Send update via HTMX
    htmx.ajax('POST', `/admin/events/${eventId}/edit-field`, {
        values: { field: field, value: value },
        target: loading,
        swap: 'outerHTML'
    });
}

function cancelInlineEdit(input) {
    restoreDisplay(input, input.dataset.originalValue);
}

function restoreDisplay(input, value) {
    const span = document.createElement('span');
    span.className = 'editable-field';
    span.dataset.field = input.dataset.field;
    span.dataset.type = input.dataset.type;
    span.textContent = value;
    input.replaceWith(span);
    initializeInlineEditing();
}
```

### Step 8: Create Related Data Partials
Location: `/SocialAnimal.Web/Areas/Admin/Views/Events/_InvitationsList.cshtml`

Create partial for related data sections:
```html
@model PagedResult<InvitationSummary>

<div class="related-list">
    @if(Model.Data.Any())
    {
        <table class="compact-table">
            <thead>
                <tr>
                    <th>Guest</th>
                    <th>Contact</th>
                    <th>Status</th>
                    <th>Response</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                @foreach(var invitation in Model.Data)
                {
                    <tr>
                        <td>
                            <a href="/admin/invitations/@invitation.Slug">
                                @invitation.GuestName
                            </a>
                        </td>
                        <td class="text-muted">@invitation.ContactInfo</td>
                        <td>
                            @if(invitation.IsSent)
                            {
                                <span class="badge badge-success">Sent</span>
                            }
                            else
                            {
                                <span class="badge badge-warning">Pending</span>
                            }
                        </td>
                        <td>
                            @if(invitation.ResponseStatus.HasValue)
                            {
                                if(invitation.ActualPartySize > 0)
                                {
                                    <span class="text-success">
                                        ✓ @invitation.ActualPartySize attending
                                    </span>
                                }
                                else
                                {
                                    <span class="text-danger">✗ Regrets</span>
                                }
                            }
                            else
                            {
                                <span class="text-muted">No response</span>
                            }
                        </td>
                        <td class="text-right">
                            <div class="action-buttons">
                                <button class="btn-icon btn-sm"
                                        hx-post="/admin/invitations/@invitation.Id/send"
                                        title="Send Invitation">
                                    <i class="icon-send"></i>
                                </button>
                                <a href="/admin/invitations/@invitation.Slug/edit"
                                   class="btn-icon btn-sm"
                                   title="Edit">
                                    <i class="icon-edit"></i>
                                </a>
                            </div>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
        
        @if(Model.TotalPages > 1)
        {
            <div class="compact-pagination">
                <!-- Pagination controls -->
            </div>
        }
    }
    else
    {
        <div class="empty-state-small">
            <p>No invitations yet</p>
            <a href="/admin/invitations/create?eventId=@ViewBag.EventId" 
               class="btn btn-sm btn-primary">
                Create First Invitation
            </a>
        </div>
    }
</div>
```

### Step 9: Add Detail View Styling
Location: `/SocialAnimal.Web/wwwroot/css/admin/details.css`

Style the detail pages:
```css
.detail-page {
    max-width: 1400px;
    margin: 0 auto;
    padding: var(--spacing-lg);
}

.detail-header {
    margin-bottom: var(--spacing-xl);
}

.detail-title-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin: var(--spacing-md) 0;
}

.detail-content {
    display: grid;
    grid-template-columns: 1fr 400px;
    gap: var(--spacing-lg);
}

.detail-card {
    background: white;
    border-radius: var(--border-radius);
    box-shadow: var(--shadow-sm);
    margin-bottom: var(--spacing-lg);
}

.card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: var(--spacing-md);
    border-bottom: 1px solid var(--color-gray-200);
}

.card-body {
    padding: var(--spacing-md);
}

.detail-list {
    display: grid;
    grid-template-columns: 140px 1fr;
    gap: var(--spacing-sm);
}

.detail-list dt {
    font-weight: 600;
    color: var(--color-gray-600);
}

.detail-list dd {
    color: var(--color-gray-800);
    margin: 0;
}

.editable-field {
    padding: 2px 4px;
    border-radius: 3px;
    transition: background-color 0.2s;
    cursor: pointer;
}

.editable-field:hover {
    background-color: var(--color-gray-50);
}

.editable-field:focus {
    outline: 2px solid var(--color-primary);
    outline-offset: 2px;
}

.stat-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: var(--spacing-md);
}

.stat-item {
    text-align: center;
}

.stat-value {
    display: block;
    font-size: 1.5rem;
    font-weight: 600;
    color: var(--color-primary);
}

.stat-label {
    display: block;
    font-size: 0.875rem;
    color: var(--color-gray-600);
    margin-top: var(--spacing-xs);
}

.audit-log {
    max-height: 300px;
    overflow-y: auto;
}

.audit-entry {
    display: flex;
    gap: var(--spacing-sm);
    padding: var(--spacing-sm) 0;
    border-bottom: 1px solid var(--color-gray-100);
}

.audit-entry:last-child {
    border-bottom: none;
}

/* Responsive */
@media (max-width: 1024px) {
    .detail-content {
        grid-template-columns: 1fr;
    }
    
    .detail-sidebar {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
        gap: var(--spacing-lg);
    }
}

@media (max-width: 640px) {
    .detail-title-row {
        flex-direction: column;
        align-items: flex-start;
    }
    
    .detail-actions {
        margin-top: var(--spacing-md);
    }
    
    .stat-grid {
        grid-template-columns: repeat(3, 1fr);
    }
}
```

### Step 10: Implement Detail Services
Location: `/SocialAnimal.Infrastructure/Services/DetailServices/`

Create services for detail page data:
```csharp
public class EventDetailService : IEventDetailService
{
    public async Task<EventDetailsDto> GetEventDetailsAsync(string slug)
    {
        var eventEntity = await _eventRepo.GetBySlugAsync(slug);
        if (eventEntity == null)
            return null;
            
        var details = new EventDetailsDto
        {
            Event = eventEntity,
            Statistics = await CalculateStatisticsAsync(eventEntity.Id),
            RecentActivity = await GetRecentActivityAsync(eventEntity.Id, 10)
        };
        
        return details;
    }
    
    private async Task<EventStatistics> CalculateStatisticsAsync(long eventId)
    {
        var invitations = await _invitationRepo.GetByEventAsync(eventId);
        var reservations = await _reservationRepo.GetByEventAsync(eventId);
        
        return new EventStatistics
        {
            TotalInvitations = invitations.Count(),
            SentInvitations = invitations.Count(i => i.IsSent),
            TotalReservations = reservations.Count(),
            ConfirmedAttendees = reservations.Sum(r => r.PartySize),
            Regrets = reservations.Count(r => r.PartySize == 0),
            PendingResponses = invitations.Count(i => !reservations.Any(r => r.InvitationId == i.Id)),
            ResponseRate = invitations.Any() 
                ? (double)reservations.Count() / invitations.Count() 
                : 0
        };
    }
}
```

### Step 11: Add Print Styles
Location: `/SocialAnimal.Web/wwwroot/css/admin/print.css`

Create print-friendly styles:
```css
@media print {
    .detail-actions,
    .card-actions,
    .btn-icon,
    .action-buttons,
    .quick-actions-list {
        display: none !important;
    }
    
    .detail-content {
        grid-template-columns: 1fr;
    }
    
    .detail-card {
        break-inside: avoid;
        box-shadow: none;
        border: 1px solid #ddd;
    }
    
    .editable-field {
        cursor: default;
    }
}
```

## Testing Checklist

- [ ] All detail views load correctly
- [ ] Navigation between related entities works
- [ ] Inline editing saves properly
- [ ] Validation errors display correctly
- [ ] HTMX updates work without refresh
- [ ] Audit logs display accurately
- [ ] Statistics calculate correctly
- [ ] Related data loads asynchronously
- [ ] Actions execute successfully
- [ ] Responsive layout works on mobile
- [ ] Print view formats correctly
- [ ] Performance acceptable for large datasets

## Performance Considerations

1. **Data Loading**:
   - Use eager loading for related data
   - Implement caching for statistics
   - Lazy load audit logs
   - Use projections for summary data

2. **Frontend**:
   - Lazy load charts
   - Debounce inline edit saves
   - Cache rendered partials
   - Use skeleton screens

## Accessibility Requirements

1. **Navigation**: Clear breadcrumbs and landmarks
2. **Inline Editing**: Keyboard support and ARIA labels
3. **Status Updates**: Screen reader announcements
4. **Focus Management**: Maintain focus after updates
5. **Color Coding**: Don't rely solely on color
6. **Alternative Text**: Describe all visual elements

## Dependencies

This task depends on:
- Task 8: Services for data retrieval
- Task 12: List views link to details

This task must be completed before:
- Task 14: Edit forms may use detail layout

## Notes

- Consider adding version history
- Implement activity timeline visualization
- Add commenting system
- Support custom fields
- Consider adding tags/categories
- Implement related entity suggestions
- Add comparison view for similar entities
- Support entity cloning
- Add QR codes for events
- Consider implementing webhooks
- Support entity archiving
- Add entity templates
- Implement change notifications
- Support entity locking
- Add full-text search within entity