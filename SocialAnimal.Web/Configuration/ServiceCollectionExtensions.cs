using System.Reflection;
using SocialAnimal.Web.Configuration.Modules;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Web.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Discover and register all service modules
        var moduleTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IServiceModule).IsAssignableFrom(t))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => Activator.CreateInstance(t) as IServiceModule)
            .Where(m => m != null)
            .OrderBy(m => m!.Order);
        
        foreach (var module in moduleTypes)
        {
            module!.ConfigureServices(services, configuration);
        }
        
        return services;
    }
    
    public static IServiceCollection AddMessageSubscribers(this IServiceCollection services)
    {
        // Find all message subscriber implementations
        var subscriberInterface = typeof(IMessageSubscriber<>);
        
        var subscriberTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i => 
                i.IsGenericType && 
                i.GetGenericTypeDefinition() == subscriberInterface));
        
        foreach (var subscriberType in subscriberTypes)
        {
            services.AddScoped(subscriberType, subscriberType);
            
            // Register by interface as well
            var interfaces = subscriberType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == subscriberInterface);
            
            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, subscriberType);
            }
        }
        
        return services;
    }
}