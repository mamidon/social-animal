# Task 11: Implement Admin Dashboard

## Objective
Create the main admin dashboard page that provides an overview of the system's current state, including summary statistics, recent activity, and quick navigation to key areas. The dashboard serves as the landing page for admin users.

## Requirements
- Display summary statistics for all entity types
- Show recent activity and trends
- Provide quick action buttons for common tasks
- Implement responsive card-based layout
- Use HTMX for dynamic data updates
- Include data visualizations where appropriate
- Ensure fast initial page load

## Implementation Steps

### Step 1: Create Dashboard Controller
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/DashboardController.cs`

The controller should:
- Inherit from `AdminControllerBase`
- Have an `Index` action for main dashboard
- Support partial updates via HTMX
- Aggregate data from multiple services
- Handle caching for performance

Actions to implement:
- `Index()` - Main dashboard view
- `GetStats()` - Return statistics partial
- `GetRecentActivity()` - Recent changes partial
- `GetUpcomingEvents()` - Upcoming events partial
- `RefreshWidget(string widgetId)` - Refresh specific widget

Include data aggregation for:
- Total counts for each entity
- Recent trends (last 7/30 days)
- System health metrics
- User activity summary

### Step 2: Create Dashboard View Model
Location: `/SocialAnimal.Web/Areas/Admin/Models/ViewModels/DashboardViewModel.cs`

Define view model structure:
```csharp
public class DashboardViewModel
{
    public DashboardStats Statistics { get; set; }
    public List<RecentActivity> RecentActivities { get; set; }
    public List<UpcomingEvent> UpcomingEvents { get; set; }
    public SystemHealth SystemHealth { get; set; }
    public QuickActions QuickActions { get; set; }
}

public class DashboardStats
{
    public EntityStats Events { get; set; }
    public EntityStats Users { get; set; }
    public EntityStats Invitations { get; set; }
    public EntityStats Reservations { get; set; }
}

public class EntityStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Deleted { get; set; }
    public int RecentlyAdded { get; set; } // Last 7 days
    public double GrowthRate { get; set; } // Percentage
    public TrendDirection Trend { get; set; }
}
```

### Step 3: Create Dashboard View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Dashboard/Index.cshtml`

Implement dashboard layout:
- Use CSS Grid or Flexbox for responsive layout
- Create card components for each metric
- Include loading skeletons for async content
- Add refresh buttons for real-time updates

Structure:
```html
@model DashboardViewModel
@{
    ViewData["Title"] = "Admin Dashboard";
}

<div class="dashboard-container">
    <!-- Summary Cards Row -->
    <div class="stats-grid">
        <!-- Event Stats Card -->
        <!-- User Stats Card -->
        <!-- Invitation Stats Card -->
        <!-- Reservation Stats Card -->
    </div>
    
    <!-- Charts Row -->
    <div class="charts-row">
        <!-- Activity Chart -->
        <!-- Attendance Chart -->
    </div>
    
    <!-- Recent Activity & Upcoming Events -->
    <div class="activity-grid">
        <!-- Recent Activity Panel -->
        <!-- Upcoming Events Panel -->
    </div>
    
    <!-- Quick Actions -->
    <div class="quick-actions">
        <!-- Action Buttons -->
    </div>
</div>
```

### Step 4: Create Statistics Cards Component
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/Components/StatsCard.cshtml`

Create reusable stats card:
```html
@model EntityStats
<div class="stats-card" data-entity="@ViewData["EntityName"]">
    <div class="stats-card-header">
        <h3>@ViewData["Title"]</h3>
        <button class="refresh-btn" 
                hx-get="/admin/dashboard/refresh-stats?entity=@ViewData["EntityName"]"
                hx-target="closest .stats-card"
                hx-swap="outerHTML">
            <!-- Refresh Icon -->
        </button>
    </div>
    <div class="stats-card-body">
        <div class="primary-stat">
            <span class="stat-value">@Model.Total</span>
            <span class="stat-label">Total</span>
        </div>
        <div class="secondary-stats">
            <div class="stat-item">
                <span class="label">Active:</span>
                <span class="value">@Model.Active</span>
            </div>
            <div class="stat-item">
                <span class="label">This Week:</span>
                <span class="value">+@Model.RecentlyAdded</span>
            </div>
        </div>
        <div class="trend-indicator @Model.Trend.ToString().ToLower()">
            <!-- Trend Arrow -->
            <span>@Model.GrowthRate%</span>
        </div>
    </div>
</div>
```

### Step 5: Implement Recent Activity Feed
Location: `/SocialAnimal.Web/Areas/Admin/Views/Dashboard/_RecentActivity.cshtml`

Create activity feed partial:
- Show last 10-20 activities
- Include entity type, action, timestamp
- Link to entity details
- Auto-refresh every 30 seconds
- Group by date

Activity types to track:
- Event created/updated/deleted
- User registered/updated
- Invitation sent
- Reservation created/updated
- Bulk operations

HTMX implementation:
```html
<div class="activity-feed" 
     hx-get="/admin/dashboard/recent-activity"
     hx-trigger="every 30s"
     hx-swap="innerHTML">
    @foreach(var activity in Model.RecentActivities)
    {
        <div class="activity-item">
            <div class="activity-icon">
                <!-- Icon based on type -->
            </div>
            <div class="activity-content">
                <p class="activity-description">
                    @activity.Description
                </p>
                <time class="activity-time" 
                      datetime="@activity.Timestamp">
                    @activity.RelativeTime
                </time>
            </div>
            <a href="@activity.EntityUrl" 
               class="activity-link"
               hx-boost="true">
                View
            </a>
        </div>
    }
</div>
```

### Step 6: Create Upcoming Events Widget
Location: `/SocialAnimal.Web/Areas/Admin/Views/Dashboard/_UpcomingEvents.cshtml`

Display upcoming events:
- Next 5-10 events
- Show date, title, attendance
- Quick link to event details
- Color code by proximity
- Include RSVP statistics

### Step 7: Implement Quick Actions Panel
Location: `/SocialAnimal.Web/Areas/Admin/Views/Dashboard/_QuickActions.cshtml`

Common actions to include:
- Create New Event
- Add User
- Send Invitations
- Export Reports
- View All Reservations
- System Settings (future)

Style as prominent buttons:
```html
<div class="quick-actions-panel">
    <h3>Quick Actions</h3>
    <div class="action-buttons">
        <a href="/admin/events/create" 
           class="btn btn-primary"
           hx-boost="true">
            <i class="icon-plus"></i>
            Create Event
        </a>
        <a href="/admin/users/create" 
           class="btn btn-secondary"
           hx-boost="true">
            <i class="icon-user-plus"></i>
            Add User
        </a>
        <!-- More actions -->
    </div>
</div>
```

### Step 8: Add Data Visualization
Location: `/SocialAnimal.Web/wwwroot/js/admin/charts.js`

Implement charts using lightweight library:
- Chart.js or ApexCharts
- D3.js for complex visualizations
- CSS-only charts for simple metrics

Charts to implement:
- Activity timeline (line chart)
- Entity distribution (pie/donut chart)
- RSVP trends (bar chart)
- Geographic distribution (map, optional)

Initialize charts on page load:
```javascript
document.addEventListener('DOMContentLoaded', function() {
    initializeActivityChart();
    initializeDistributionChart();
    initializeRSVPChart();
});

function initializeActivityChart() {
    const ctx = document.getElementById('activityChart');
    // Chart implementation
}
```

### Step 9: Implement Dashboard Service
Location: `/SocialAnimal.Infrastructure/Services/DashboardService.cs`

Create service for dashboard data:
```csharp
public class DashboardService : IDashboardService
{
    private readonly IEventService _eventService;
    private readonly IUserService _userService;
    private readonly IInvitationService _invitationService;
    private readonly IReservationService _reservationService;
    private readonly IMemoryCache _cache;
    
    public async Task<DashboardStats> GetStatisticsAsync()
    {
        return await _cache.GetOrCreateAsync("dashboard_stats", 
            async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);
                // Aggregate statistics
            });
    }
    
    public async Task<List<RecentActivity>> GetRecentActivityAsync(int count = 20)
    {
        // Fetch recent activities across all entities
    }
    
    public async Task<List<UpcomingEvent>> GetUpcomingEventsAsync(int count = 10)
    {
        // Fetch upcoming events with RSVP counts
    }
}
```

### Step 10: Add Dashboard Styling
Location: `/SocialAnimal.Web/wwwroot/css/admin/dashboard.css`

Create dashboard-specific styles:
```css
.dashboard-container {
    padding: var(--spacing-lg);
    max-width: 1400px;
    margin: 0 auto;
}

.stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: var(--spacing-md);
    margin-bottom: var(--spacing-xl);
}

.stats-card {
    background: white;
    border-radius: var(--border-radius);
    padding: var(--spacing-md);
    box-shadow: var(--shadow-sm);
    transition: transform 0.2s, box-shadow 0.2s;
}

.stats-card:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-md);
}

.activity-feed {
    max-height: 400px;
    overflow-y: auto;
}

.chart-container {
    background: white;
    border-radius: var(--border-radius);
    padding: var(--spacing-md);
    height: 300px;
}
```

### Step 11: Implement Loading States
Location: `/SocialAnimal.Web/Areas/Admin/Views/Dashboard/_LoadingSkeleton.cshtml`

Create skeleton screens:
```html
<div class="skeleton-card">
    <div class="skeleton-header"></div>
    <div class="skeleton-body">
        <div class="skeleton-line"></div>
        <div class="skeleton-line short"></div>
    </div>
</div>
```

Use for initial load:
- Show skeletons immediately
- Load actual data via HTMX
- Smooth transition when data arrives

### Step 12: Add Responsive Design
Ensure dashboard works on all devices:

Mobile (< 640px):
- Stack cards vertically
- Hide secondary stats
- Simplify charts
- Reduce activity feed items

Tablet (640px - 1024px):
- 2-column grid for stats
- Adjusted chart sizes
- Collapsible panels

Desktop (> 1024px):
- Full 4-column grid
- All features visible
- Side-by-side panels

### Step 13: Implement Real-time Updates (Optional)
Location: `/SocialAnimal.Web/wwwroot/js/admin/dashboard-realtime.js`

Add real-time capabilities:
- Use SignalR for WebSocket connection
- Update stats when changes occur
- Show notification for new activities
- Animate number changes

Or use HTMX polling:
```html
<div hx-get="/admin/dashboard/stats"
     hx-trigger="every 10s"
     hx-swap="outerHTML">
    <!-- Stats content -->
</div>
```

### Step 14: Add Export Functionality
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/DashboardController.cs`

Add export actions:
- `ExportStats()` - Export as CSV/Excel
- `ExportReport()` - Generate PDF report
- `GetAnalytics()` - Detailed analytics view

## Testing Checklist

- [ ] Dashboard loads within 2 seconds
- [ ] All statistics calculate correctly
- [ ] Recent activity shows accurate data
- [ ] HTMX updates work without page refresh
- [ ] Charts render properly
- [ ] Responsive design works on all devices
- [ ] Loading states display correctly
- [ ] Error states handle gracefully
- [ ] Quick actions navigate correctly
- [ ] Auto-refresh doesn't cause flicker
- [ ] Caching improves performance
- [ ] Export functions work correctly

## Performance Metrics

Target performance:
- Initial load: < 2 seconds
- Subsequent loads: < 500ms (cached)
- Widget refresh: < 300ms
- Chart render: < 500ms
- Activity feed update: < 200ms

Optimization techniques:
1. Cache aggregated data
2. Use database indexes
3. Implement pagination
4. Lazy load charts
5. Compress responses
6. Use CDN for assets

## Accessibility Requirements

1. **Semantic Structure**: Use proper headings
2. **ARIA Labels**: Label all interactive elements
3. **Keyboard Navigation**: Ensure tab order
4. **Screen Reader**: Test with NVDA/JAWS
5. **Color Contrast**: Meet WCAG standards
6. **Focus Indicators**: Visible focus states
7. **Alternative Text**: Describe charts/images

## Security Considerations

1. **Authorization**: Verify admin access
2. **Data Sanitization**: Escape user content
3. **Rate Limiting**: Limit refresh requests
4. **Audit Logging**: Log dashboard access
5. **Data Privacy**: Respect user privacy
6. **CSRF Protection**: Include tokens

## Dependencies

This task depends on:
- Task 8: Services for data aggregation
- Task 9: MVC infrastructure
- Task 10: Admin layout

This task must be completed before:
- Can be done in parallel with Tasks 12-14

## Notes

- Keep dashboard lightweight and fast
- Use progressive enhancement
- Consider adding customization options
- Plan for widget architecture
- Document performance baselines
- Create dashboard variations for roles
- Consider mobile-first approach
- Add helpful tooltips
- Include contextual help
- Plan for internationalization
- Use consistent date/time formats
- Consider adding search functionality
- Implement proper error boundaries
- Add print styles for reports
- Consider dashboard templates