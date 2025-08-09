using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Portals;
using SocialAnimal.Infrastructure.Repositories;

namespace SocialAnimal.Web.Configuration.Modules;

public class InfrastructureServicesModule : IServiceModule
{
    public int Order => 2;
    
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure Entity Framework
        ConfigureDatabase(services, configuration);
        
        // Register repository implementations
        RegisterRepositoryImplementations(services);
        
        // Register portal implementations
        RegisterPortalImplementations(services);
        
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Configure database health checks
        ConfigureDatabaseHealthChecks(services, configuration);
    }
    
    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<ApplicationContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.UseNodaTime();
                });
                
                options.UseSnakeCaseNamingConvention();
                
                // Add logging in development
                var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                if (env.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });
            
            // Register context factory for repositories
            services.AddScoped<Func<ApplicationContext>>(serviceProvider => 
                () => serviceProvider.GetRequiredService<ApplicationContext>());
        }
        else
        {
            // Use in-memory database for testing if no connection string
            services.AddDbContext<ApplicationContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
                options.UseSnakeCaseNamingConvention();
            });
            
            // Register context factory for repositories
            services.AddScoped<Func<ApplicationContext>>(serviceProvider => 
                () => serviceProvider.GetRequiredService<ApplicationContext>());
        }
    }
    
    private static void RegisterRepositoryImplementations(IServiceCollection services)
    {
        var assembly = typeof(CrudRepo).Assembly; // Infrastructure assembly
        
        // Register base CRUD repository
        services.AddScoped<ICrudRepo, CrudRepo>();
        
        // Convention-based repository registration
        var repoTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Repo"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t != typeof(CrudRepo)); // Already registered
        
        foreach (var repoType in repoTypes)
        {
            // Find matching interface
            var interfaceType = repoType.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{repoType.Name}");
            
            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, repoType);
            }
            
            // Also register the concrete type
            services.AddScoped(repoType, repoType);
        }
    }
    
    private static void RegisterPortalImplementations(IServiceCollection services)
    {
        // Register logger portal as singleton (needed by singleton InMemoryMessagePortal)
        services.AddSingleton<ILoggerPortal>(provider => 
            new ConsoleLoggerPortal("SocialAnimal.Web"));
        
        // Register configuration portal with environment
        services.AddSingleton<IConfigurationPortal>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            return new ConfigurationPortal(configuration, environment.EnvironmentName);
        });
        
        // Register NodaTime clock portal
        services.AddSingleton<IClockPortal>(provider =>
            new SystemClockPortal(provider.GetRequiredService<IClock>()));
        
        // Register metrics portal
        services.AddSingleton<IMetricsPortal>(provider =>
            new InMemoryMetricsPortal(
                provider.GetRequiredService<IClock>(),
                provider.GetRequiredService<ILoggerPortal>()));
        
        // Register message portal as singleton - it implements multiple interfaces
        services.AddSingleton<InMemoryMessagePortal>(provider =>
            new InMemoryMessagePortal(
                provider.GetRequiredService<IClock>(),
                provider.GetRequiredService<ILoggerPortal>()));
        
        services.AddSingleton<IMessagePublisher>(provider => 
            provider.GetRequiredService<InMemoryMessagePortal>());
        services.AddSingleton<IMessageDispatcher>(provider => 
            provider.GetRequiredService<InMemoryMessagePortal>());
    }
    
    private static void ConfigureDatabaseHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        // Database health checks are configured in the ApiServicesModule
        // This method is reserved for future database-specific health checks
    }
}