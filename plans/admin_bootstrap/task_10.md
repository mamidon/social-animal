# Task 10: Create Admin Layout and Navigation

## Objective
Design and implement the master layout template for the admin portal, including navigation menu, header, footer, and common UI components. This layout will serve as the foundation for all admin pages and establish the visual design system.

## Requirements
- Create responsive master layout template
- Implement navigation menu with entity sections
- Setup CSS framework integration
- Configure HTMX default behaviors for the layout
- Create reusable partial views for common components
- Ensure accessibility standards are met
- Implement loading states and indicators

## Implementation Steps

### Step 1: Create Master Layout Template
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml`

The layout should include:
- HTML5 document structure
- Meta tags for responsive design
- CSS bundle references
- JavaScript bundle references
- HTMX configuration script
- Navigation component
- Main content area with proper structure
- Footer component
- Toast/notification container
- Loading indicator container

Layout sections to define:
- `@RenderSection("Styles", required: false)` - Page-specific CSS
- `@RenderSection("Scripts", required: false)` - Page-specific JS
- `@RenderSection("PageHeader", required: false)` - Page title area
- `@RenderSection("Breadcrumbs", required: false)` - Navigation breadcrumbs
- `@RenderBody()` - Main content area

HTML structure pattern:
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- Meta tags, title, CSS -->
</head>
<body>
    <div class="admin-wrapper">
        <!-- Navigation -->
        <main class="admin-content">
            <!-- Page content -->
        </main>
        <!-- Footer -->
    </div>
    <!-- Scripts, HTMX config -->
</body>
</html>
```

### Step 2: Implement Navigation Component
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_Navigation.cshtml`

Create navigation with sections for:
- Dashboard link (home)
- Events management
  - List all events
  - Create new event
  - Deleted events
- Users management
  - List all users
  - Create new user
  - Deleted users
- Invitations management
  - List all invitations
  - Create new invitation
  - Bulk create invitations
- Reservations management
  - List all reservations
  - Attendance report
  - Pending RSVPs

Navigation features:
- Active state highlighting
- Collapsible sections for mobile
- Icons for each section
- Badge indicators for counts
- User menu (future)
- Search box (future)

Implement responsive behavior:
- Desktop: Sidebar navigation
- Tablet: Collapsible sidebar
- Mobile: Hamburger menu with overlay

HTMX enhancements:
- Prefetch on hover using `hx-boost`
- Update active state without refresh
- Load badge counts asynchronously

### Step 3: Create Header Component
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_Header.cshtml`

Header should include:
- Application logo/title
- Current page title
- Breadcrumb navigation
- Quick actions toolbar
- User profile menu (placeholder)
- Environment indicator (dev/staging/prod)

Implement as ViewComponent:
Location: `/SocialAnimal.Web/Areas/Admin/ViewComponents/HeaderViewComponent.cs`
- Accept page title parameter
- Build breadcrumb from route
- Inject environment info
- Support custom actions

### Step 4: Create Footer Component
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_Footer.cshtml`

Footer should include:
- Copyright information
- Version number
- Quick links
- Support contact
- Last deployment timestamp
- Database connection status indicator

Make footer sticky:
- Always visible on desktop
- Hidden on mobile scroll
- Contains useful debugging info in dev

### Step 5: Setup CSS Framework Structure
Location: `/SocialAnimal.Web/wwwroot/css/admin/layout.css`

Define CSS structure for:
- Grid system for layout
- Flexbox utilities
- Spacing system (margins, padding)
- Color variables
- Typography scale
- Shadow system
- Border radius tokens
- Transition definitions

CSS custom properties:
```css
:root {
  /* Colors */
  --color-primary: ...;
  --color-secondary: ...;
  --color-success: ...;
  --color-danger: ...;
  --color-warning: ...;
  --color-info: ...;
  
  /* Spacing */
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  --spacing-lg: 1.5rem;
  --spacing-xl: 2rem;
  
  /* Typography */
  --font-family-base: ...;
  --font-size-base: ...;
  --line-height-base: ...;
  
  /* Layout */
  --sidebar-width: 250px;
  --header-height: 60px;
  --footer-height: 40px;
}
```

### Step 6: Configure HTMX Default Behaviors
Location: `/SocialAnimal.Web/wwwroot/js/admin/htmx-setup.js`

Configure HTMX defaults:
- Default swap strategy (innerHTML vs outerHTML)
- Default target for responses
- Error handling behavior
- Loading indicator triggers
- Request timeout settings
- History handling

Setup HTMX event handlers:
```javascript
// Configure before requests
document.body.addEventListener('htmx:beforeRequest', function(evt) {
    // Show loading indicator
    // Add CSRF token
    // Log request
});

// Configure after requests
document.body.addEventListener('htmx:afterRequest', function(evt) {
    // Hide loading indicator
    // Handle errors
    // Update navigation state
});
```

### Step 7: Create Loading Indicators
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_LoadingIndicator.cshtml`

Create multiple loading states:
- Inline spinner for buttons
- Full-page overlay loader
- Skeleton screens for content
- Progress bars for long operations
- Toast notifications for background tasks

Implement HTMX indicator system:
- Use `hx-indicator` attribute
- CSS classes for showing/hiding
- Smooth transitions
- Accessible ARIA states

### Step 8: Implement Toast Notifications
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_Toasts.cshtml`

Create toast system for:
- Success messages
- Error messages
- Warning messages
- Info messages
- Long-running operation updates

Features:
- Auto-dismiss after timeout
- Manual dismiss option
- Stack multiple toasts
- Persist important messages
- HTMX trigger support

Create JavaScript module:
Location: `/SocialAnimal.Web/wwwroot/js/admin/toasts.js`
- `showToast(message, type, duration)`
- `clearToasts()`
- `showErrorToast(message)`
- `showSuccessToast(message)`

### Step 9: Create Common Partial Views
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/Partials/`

Create reusable partials:
- `_Pagination.cshtml` - Pagination controls
- `_SearchBox.cshtml` - Search input component
- `_FilterPanel.cshtml` - Filter sidebar
- `_DataTable.cshtml` - Table wrapper
- `_EmptyState.cshtml` - No data message
- `_ErrorAlert.cshtml` - Error display
- `_ConfirmDialog.cshtml` - Confirmation modal
- `_FormActions.cshtml` - Form button group

Each partial should:
- Accept model/parameters
- Support HTMX attributes
- Be self-contained
- Follow naming conventions
- Include documentation comments

### Step 10: Setup Icon System
Location: `/SocialAnimal.Web/wwwroot/lib/icons/`

Choose and implement icon system:
- Bootstrap Icons
- Font Awesome
- Heroicons
- Custom SVG sprites

Create icon helper:
Location: `/SocialAnimal.Web/Infrastructure/HtmlHelpers/IconHelper.cs`
- `@Html.Icon("name", "class")`
- Support for different sizes
- Accessible labels
- Consistent usage

### Step 11: Implement Responsive Grid System
Location: `/SocialAnimal.Web/wwwroot/css/admin/grid.css`

Define grid classes:
- Container widths
- Column system (12-column)
- Responsive breakpoints
- Gap utilities
- Alignment utilities

Breakpoints:
- Mobile: < 640px
- Tablet: 640px - 1024px
- Desktop: 1024px - 1280px
- Wide: > 1280px

### Step 12: Create Theme Switcher (Optional)
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/_ThemeSwitcher.cshtml`

Implement theme support:
- Light theme (default)
- Dark theme
- System preference detection
- User preference persistence
- Smooth transitions

Store preference in:
- Local storage
- Cookie for server-side
- Database (future)

### Step 13: Implement Breadcrumb Builder
Location: `/SocialAnimal.Web/Infrastructure/Services/BreadcrumbBuilder.cs`

Create service to generate breadcrumbs:
- Parse current route
- Build hierarchy
- Generate links
- Support custom items
- Handle special cases

Use in ViewComponent:
- Inject breadcrumb builder
- Generate from request path
- Support manual override
- Cache for performance

### Step 14: Create Layout Variants
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/`

Create layout variations:
- `_AdminLayoutFull.cshtml` - Full width, no sidebar
- `_AdminLayoutMinimal.cshtml` - No navigation
- `_AdminLayoutPrint.cshtml` - Print-friendly

Each variant for specific use cases:
- Full: Data tables, reports
- Minimal: Login, errors
- Print: Reports, invoices

## Testing Checklist

- [ ] Layout renders correctly in all browsers
- [ ] Navigation menu works on all screen sizes
- [ ] Active navigation state updates correctly
- [ ] HTMX requests use correct layouts
- [ ] Loading indicators appear/disappear properly
- [ ] Toast notifications display correctly
- [ ] Theme switcher works (if implemented)
- [ ] Breadcrumbs generate accurately
- [ ] All partials render without errors
- [ ] Icons display properly
- [ ] Grid system responsive at all breakpoints
- [ ] Accessibility standards met (WCAG 2.1 AA)

## Responsive Design Verification

Test at these viewports:
1. Mobile (320px, 375px, 414px)
2. Tablet (768px, 834px)
3. Desktop (1024px, 1280px, 1440px)
4. Wide (1920px, 2560px)

Verify:
- Navigation adapts appropriately
- Content remains readable
- Touch targets adequate size
- No horizontal scrolling
- Images scale properly

## Accessibility Requirements

1. **Semantic HTML**: Use proper heading hierarchy
2. **ARIA Labels**: Add where needed
3. **Keyboard Navigation**: Ensure all interactive elements accessible
4. **Focus States**: Visible focus indicators
5. **Color Contrast**: Meet WCAG AA standards
6. **Screen Reader**: Test with screen readers
7. **Skip Links**: Provide skip navigation links

## Performance Considerations

1. **CSS Optimization**: 
   - Minimize CSS bundle size
   - Use CSS custom properties for theming
   - Avoid expensive selectors

2. **JavaScript Optimization**:
   - Lazy load non-critical scripts
   - Use event delegation
   - Minimize DOM manipulation

3. **Image Optimization**:
   - Use appropriate formats
   - Implement lazy loading
   - Provide responsive images

4. **Caching Strategy**:
   - Cache static assets
   - Use versioning for cache busting
   - Implement service worker (optional)

## Browser Support

Ensure compatibility with:
- Chrome (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Edge (latest 2 versions)
- Mobile Safari (iOS 14+)
- Chrome Mobile (Android 10+)

## Dependencies

This task depends on:
- Task 9: MVC infrastructure must be setup

This task must be completed before:
- Task 11-14: All page implementations need layout

## Notes

- Keep layout modular and maintainable
- Use CSS custom properties for easy theming
- Ensure consistent spacing throughout
- Document component usage patterns
- Consider print styles from the start
- Plan for RTL language support if needed
- Use semantic HTML for better SEO
- Implement proper meta tags
- Consider Open Graph tags for sharing
- Add favicon and app icons
- Setup error boundary for JavaScript
- Use progressive enhancement approach
- Test with slow network connections
- Validate HTML markup
- Run accessibility audit tools