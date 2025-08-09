using System.Reflection;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Web.Configuration.Modules;

public class CoreServicesModule : IServiceModule
{
    public int Order => 1;
    
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register NodaTime clock
        services.AddSingleton<IClock>(SystemClock.Instance);
        
        // Convention-based service discovery for *Service classes
        RegisterServicesByConvention(services);
        
        // Register repository interfaces
        RegisterRepositories(services);
        
        // Register portal interfaces (will be implemented by Infrastructure module)
        RegisterPortalInterfaces(services);
    }
    
    private static void RegisterServicesByConvention(IServiceCollection services)
    {
        var assembly = typeof(BaseRecord).Assembly; // Core assembly
        
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Service"))
            .Where(t => t.IsClass && !t.IsAbstract);
        
        foreach (var type in serviceTypes)
        {
            services.AddScoped(type, type);
            
            // Also register by interface if one exists
            var interfaceType = type.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{type.Name}");
            
            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, type);
            }
        }
    }
    
    private static void RegisterRepositories(IServiceCollection services)
    {
        var assembly = typeof(ICrudRepo).Assembly; // Core assembly
        
        var repoInterfaces = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Repo"))
            .Where(t => t.IsInterface);
        
        // Repository implementations will be registered by Infrastructure module
        // This just ensures the interfaces are known
        foreach (var repoInterface in repoInterfaces)
        {
            // Mark for later implementation binding
        }
    }
    
    private static void RegisterPortalInterfaces(IServiceCollection services)
    {
        // Portal interfaces will be implemented by Infrastructure module
        // Listed here for documentation
        // - ILoggerPortal
        // - IMessagePublisher, IMessageDispatcher
        // - IMetricsPortal
        // - IClockPortal
        // - IConfigurationPortal
        // - ICachePortal
    }
}