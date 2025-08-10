using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;

namespace SocialAnimal.Web.Configuration.Modules;

public class WebServicesModule : IServiceModule
{
    public int Order => 3; // After Core and Infrastructure modules
    
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add MVC with Areas support
        services.AddControllersWithViews(options =>
        {
            // Configure global filters if needed
        })
        .AddRazorOptions(options =>
        {
            // Add area view locations
            options.ViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
            options.ViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
            
            // Enable view compilation in development
            if (configuration.GetValue<bool>("EnableRazorRuntimeCompilation", false))
            {
                options.ViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
            }
        });
        
        // Configure Razor Pages (optional)
        services.AddRazorPages(options =>
        {
            // Configure page conventions for admin area if needed
        });
        
        // Add anti-forgery services
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "RequestVerificationToken";
            options.SuppressXFrameOptionsHeader = false;
        });
        
        // Configure session (if needed for admin)
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Name = "SocialAnimal.Session";
        });
        
        // Configure response compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });
        
        // Development-only services would require Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation package
        // For now, we'll skip runtime compilation to keep dependencies minimal
        
        // Configure model binding options if needed in the future
    }
}