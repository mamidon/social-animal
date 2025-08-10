# Task 9: Setup MVC Infrastructure for Admin Portal

## Objective
Configure the ASP.NET Core MVC infrastructure to support a server-side rendered admin portal with HTMX for progressive enhancement. This includes setting up routing, areas, Razor Pages structure, and client-side dependencies.

## Requirements
- Create dedicated Admin area for isolation
- Configure routing for admin-specific routes
- Setup Razor Pages with proper folder structure
- Configure static file serving for CSS/JS assets
- Integrate HTMX library for progressive enhancement
- Ensure proper middleware ordering
- Configure view compilation and hot reload for development

## Implementation Steps

### Step 1: Create Admin Area Structure
Location: `/SocialAnimal.Web/Areas/Admin/`

Create the following directory structure:
```
Areas/
└── Admin/
    ├── Controllers/
    ├── Views/
    │   ├── Shared/
    │   │   ├── _Layout.cshtml
    │   │   ├── _ViewStart.cshtml
    │   │   └── _ViewImports.cshtml
    │   ├── Dashboard/
    │   ├── Events/
    │   ├── Invitations/
    │   ├── Reservations/
    │   └── Users/
    ├── Pages/           (if using Razor Pages)
    └── Models/
        ├── ViewModels/
        └── PageModels/
```

Each folder serves:
- Controllers: Admin-specific MVC controllers
- Views: Razor views organized by controller
- Pages: Razor Pages alternative to MVC
- Models: View models and page models
- Shared: Layout and shared components

### Step 2: Configure Area Registration
Location: `/SocialAnimal.Web/Program.cs` or `Startup.cs`

Update the application configuration:
- Add MVC with Areas support
- Configure Razor Pages if using
- Setup view location formats
- Configure route conventions
- Add area authorization policies

Configuration should include:
```csharp
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options => 
    {
        // Add area view locations
        // Configure view compilation
    });

builder.Services.AddRazorPages(options =>
{
    // Configure page conventions
    // Setup area routes
});
```

### Step 3: Setup Admin Routing
Location: `/SocialAnimal.Web/Program.cs` or routing configuration

Configure routing patterns:
- Admin area route: `/admin/{controller}/{action}`
- Default admin route: `/admin` → Dashboard
- Entity routes: `/admin/entities/{entity}/{action}`
- API routes: `/admin/api/{controller}/{action}`

Implement route constraints:
- Area constraint for admin routes
- Slug constraints for entity identifiers
- ID constraints for numeric identifiers

Setup route precedence:
- Admin routes before default routes
- Specific routes before generic
- API routes with version prefix

### Step 4: Install and Configure HTMX
Location: `/SocialAnimal.Web/wwwroot/lib/htmx/`

Install HTMX library:
- Download HTMX distribution files
- Place in wwwroot/lib/htmx directory
- Include minified and debug versions
- Add HTMX extensions if needed

HTMX files to include:
- `htmx.min.js` - Core HTMX library
- `htmx.js` - Debug version for development
- Extensions:
  - `json-enc.js` - JSON encoding
  - `loading-states.js` - Loading indicators
  - `alpine-morph.js` - Alpine.js integration if used

Create HTMX configuration:
Location: `/SocialAnimal.Web/wwwroot/js/admin/htmx-config.js`
- Configure default headers
- Setup CSRF token handling
- Configure error handling
- Setup loading indicators
- Configure default swap strategies

### Step 5: Configure Static File Serving
Location: `/SocialAnimal.Web/Program.cs`

Setup static file middleware:
- Enable static file serving
- Configure cache headers
- Setup file providers
- Configure MIME types
- Enable directory browsing for development

Create directory structure:
```
wwwroot/
├── css/
│   ├── admin/
│   │   ├── layout.css
│   │   ├── components.css
│   │   └── utilities.css
│   └── vendor/
├── js/
│   ├── admin/
│   │   ├── app.js
│   │   ├── htmx-config.js
│   │   └── components/
│   └── vendor/
├── lib/
│   ├── htmx/
│   ├── bootstrap/ (or chosen CSS framework)
│   └── alpinejs/ (if using)
└── images/
    └── admin/
```

### Step 6: Setup View Imports and Start Files
Location: `/SocialAnimal.Web/Areas/Admin/Views/_ViewImports.cshtml`

Configure view imports:
- Add necessary using statements
- Import tag helpers
- Import view models namespace
- Add common namespaces
- Configure custom tag helpers

Include:
```
@using SocialAnimal.Web
@using SocialAnimal.Web.Areas.Admin.Models
@using SocialAnimal.Core.Domain
@using Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, SocialAnimal.Web
```

Location: `/SocialAnimal.Web/Areas/Admin/Views/_ViewStart.cshtml`

Set default layout:
```
@{
    Layout = "_Layout";
}
```

### Step 7: Configure Development Tools
Location: `/SocialAnimal.Web/Program.cs`

Setup development features:
- Enable Razor runtime compilation
- Configure hot reload
- Setup browser refresh on change
- Enable detailed error pages
- Configure developer exception page

Add development-specific middleware:
- Request logging
- Response timing
- Database query logging
- HTMX request debugging

### Step 8: Create Base Controller for Admin
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/AdminControllerBase.cs`

Create base controller with:
- Common action filters
- Shared view data setup
- Error handling
- Logging configuration
- HTMX request detection
- Common response helpers

Methods to include:
- `IsHtmxRequest()` - Detect HTMX requests
- `HtmxRedirect(url)` - HTMX-aware redirects
- `PartialOrFull(view, model)` - Return partial or full view
- `SetupViewData()` - Common view data
- `HandleError(exception)` - Consistent error handling

### Step 9: Setup CSS Framework
Location: `/SocialAnimal.Web/wwwroot/lib/`

Choose and install CSS framework:
- Bootstrap 5.x for traditional styling
- Tailwind CSS for utility-first approach
- Bulma for modern, lightweight option

Configure framework:
- Install via npm or download directly
- Place in wwwroot/lib directory
- Create custom theme variables
- Setup build process if needed
- Configure PurgeCSS for production

Create admin-specific styles:
Location: `/SocialAnimal.Web/wwwroot/css/admin/`
- `variables.css` - CSS custom properties
- `layout.css` - Layout and structure
- `components.css` - Reusable components
- `utilities.css` - Utility classes
- `htmx.css` - HTMX-specific styles

### Step 10: Configure Bundle and Minification
Location: `/SocialAnimal.Web/bundleconfig.json`

Setup bundling configuration:
- Create CSS bundles for admin area
- Create JavaScript bundles
- Configure minification settings
- Setup source maps for debugging

Define bundles:
- `admin-vendor.css` - Third-party CSS
- `admin-app.css` - Application CSS
- `admin-vendor.js` - Third-party JS
- `admin-app.js` - Application JS

### Step 11: Create HTMX Helpers
Location: `/SocialAnimal.Web/Infrastructure/HtmlHelpers/HtmxHelpers.cs`

Create HTML helpers for HTMX:
- `HxGet(url)` - Generate hx-get attribute
- `HxPost(url)` - Generate hx-post attribute
- `HxTarget(selector)` - Generate hx-target
- `HxSwap(strategy)` - Generate hx-swap
- `HxTrigger(event)` - Generate hx-trigger
- `HxIndicator(selector)` - Loading indicator

Create Tag Helpers:
Location: `/SocialAnimal.Web/Infrastructure/TagHelpers/`
- `HtmxFormTagHelper` - HTMX-enabled forms
- `HtmxLinkTagHelper` - HTMX-enabled links
- `HtmxButtonTagHelper` - HTMX-enabled buttons

### Step 12: Setup Error Handling
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/`

Create error views:
- `Error.cshtml` - General error page
- `404.cshtml` - Not found page
- `403.cshtml` - Forbidden page
- `500.cshtml` - Server error page
- `_ErrorPartial.cshtml` - HTMX error responses

Configure error handling middleware:
- Custom error page middleware
- HTMX-aware error responses
- Logging of errors
- User-friendly error messages

### Step 13: Configure Security Headers
Location: `/SocialAnimal.Web/Infrastructure/Middleware/`

Implement security middleware:
- Content Security Policy (CSP)
- X-Frame-Options
- X-Content-Type-Options
- X-XSS-Protection
- HSTS headers

Consider HTMX requirements:
- Allow inline event handlers if needed
- Configure nonce for inline scripts
- Setup trusted sources for HTMX

### Step 14: Create Development Seed Data
Location: `/SocialAnimal.Web/Infrastructure/Seeding/AdminSeeder.cs`

Create seed data for development:
- Sample users
- Sample events
- Sample invitations
- Sample reservations

This helps with:
- Testing pagination
- Testing filters
- UI development
- Demo purposes

## Testing Checklist

- [ ] Admin area accessible at /admin
- [ ] Routing works for all admin routes
- [ ] Static files served correctly
- [ ] HTMX library loads properly
- [ ] CSS framework loads and styles apply
- [ ] Hot reload works in development
- [ ] Error pages display correctly
- [ ] HTMX requests detected properly
- [ ] Bundles generate correctly
- [ ] Security headers present
- [ ] View compilation works
- [ ] Tag helpers function properly

## Configuration Verification

1. **Middleware Pipeline Order**:
   - Exception handling
   - HSTS (production)
   - HTTPS redirection
   - Static files
   - Routing
   - Authentication (future)
   - Authorization (future)
   - MVC/Razor Pages

2. **Route Testing**:
   - `/admin` → Dashboard
   - `/admin/events` → Events list
   - `/admin/events/create` → Create event
   - `/admin/api/events` → API endpoint

3. **HTMX Configuration**:
   - Requests include HX-Request header
   - Responses return appropriate content
   - Swapping strategies work correctly
   - Error handling returns proper status codes

## Performance Considerations

1. **Static File Caching**: Configure appropriate cache headers
2. **Bundle Optimization**: Minify and compress assets
3. **CDN Usage**: Consider CDN for vendor libraries
4. **Lazy Loading**: Implement for non-critical resources
5. **Compression**: Enable response compression

## Security Considerations

1. **CSRF Protection**: Ensure anti-forgery tokens
2. **CSP Headers**: Configure Content Security Policy
3. **HTTPS**: Enforce HTTPS in production
4. **Input Validation**: Client and server validation
5. **XSS Prevention**: Proper encoding and sanitization

## Dependencies

This task depends on:
- Basic project structure existing
- .NET 9 Web project configured

This task must be completed before:
- Task 10: Layout needs infrastructure
- Task 11-15: All UI tasks need this setup

## Notes

- Choose between MVC Controllers or Razor Pages (or hybrid)
- Consider using ViewComponents for reusable UI
- HTMX works well with partial views
- Keep JavaScript minimal, leverage HTMX
- Use CSS custom properties for theming
- Consider accessibility from the start
- Plan for internationalization if needed
- Document HTMX patterns for team
- Setup browser testing tools
- Configure logging for debugging