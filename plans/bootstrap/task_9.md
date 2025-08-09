Configure dependency injection and service registration

This task sets up the dependency injection container in the Web project following the convention-based service discovery pattern from CLAUDE.md, including module-based registration and proper scoping for different service types.

## Work to be Done

### Service Registration Module Base
Create `IServiceModule.cs` in `SocialAnimal.Web/Configuration/Modules/IServiceModule.cs`:

```csharp
namespace SocialAnimal.Web.Configuration.Modules;

public interface IServiceModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    int Order => 0; // Allows ordering of module registration
}
```

### Core Services Module
Create `CoreServicesModule.cs` in `SocialAnimal.Web/Configuration/Modules/CoreServicesModule.cs`:

```csharp
using System.Reflection;
using NodaTime;
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
```

### Infrastructure Services Module
Create `InfrastructureServicesModule.cs` in `SocialAnimal.Web/Configuration/Modules/InfrastructureServicesModule.cs`:

```csharp
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
    }
    
    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
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
        var assembly = typeof(ConsoleLoggerPortal).Assembly; // Infrastructure assembly
        
        // Register portal implementations based on naming convention
        var portalTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Portal"))
            .Where(t => t.IsClass && !t.IsAbstract);
        
        foreach (var portalType in portalTypes)
        {
            var interfaces = portalType.GetInterfaces()
                .Where(i => i.Namespace?.Contains("Core.Portals") == true);
            
            foreach (var interfaceType in interfaces)
            {
                // Dispatcher should be singleton, others scoped
                if (interfaceType.Name.Contains("Dispatcher"))
                {
                    services.AddSingleton(interfaceType, portalType);
                }
                else
                {
                    services.AddScoped(interfaceType, portalType);
                }
            }
        }
        
        // Special case: Message portal implements multiple interfaces
        services.AddSingleton<InMemoryMessagePortal>();
        services.AddSingleton<IMessagePublisher>(provider => 
            provider.GetRequiredService<InMemoryMessagePortal>());
        services.AddSingleton<IMessageDispatcher>(provider => 
            provider.GetRequiredService<InMemoryMessagePortal>());
        
        // Logger portal with component name
        services.AddScoped<ILoggerPortal>(provider => 
            new ConsoleLoggerPortal("SocialAnimal.Web"));
        
        // Configuration portal with environment
        services.AddSingleton<IConfigurationPortal>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            return new ConfigurationPortal(configuration, environment.EnvironmentName);
        });
    }
}
```

### API Services Module
Create `ApiServicesModule.cs` in `SocialAnimal.Web/Configuration/Modules/ApiServicesModule.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

namespace SocialAnimal.Web.Configuration.Modules;

public class ApiServicesModule : IServiceModule
{
    public int Order => 3;
    
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure controllers
        ConfigureControllers(services);
        
        // Configure authentication
        ConfigureAuthentication(services, configuration);
        
        // Configure Swagger/OpenAPI
        ConfigureSwagger(services);
        
        // Configure CORS
        ConfigureCors(services, configuration);
        
        // Configure health checks
        ConfigureHealthChecks(services);
    }
    
    private static void ConfigureControllers(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Configure JSON serialization
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
                
                // Add NodaTime serialization support
                options.JsonSerializerOptions.Converters.Add(new NodaTime.Serialization.SystemTextJson.NodaConverters.InstantConverter());
                options.JsonSerializerOptions.Converters.Add(new NodaTime.Serialization.SystemTextJson.NodaConverters.LocalDateConverter());
            });
        
        services.AddEndpointsApiExplorer();
    }
    
    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });
        
        services.AddAuthorization();
    }
    
    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SocialAnimal API",
                Version = "v1",
                Description = "API for managing social events"
            });
            
            // Add JWT authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
    
    private static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }
    
    private static void ConfigureHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationContext>("database");
    }
}
```

### Service Registration Extensions
Create `ServiceCollectionExtensions.cs` in `SocialAnimal.Web/Configuration/ServiceCollectionExtensions.cs`:

```csharp
using System.Reflection;
using SocialAnimal.Web.Configuration.Modules;

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
```

### Program.cs Configuration
Create updated `Program.cs` in `SocialAnimal.Web/Program.cs`:

```csharp
using Serilog;
using SocialAnimal.Core.Portals;
using SocialAnimal.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services using module pattern
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddMessageSubscribers();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SocialAnimal API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Start message dispatcher
var messageDispatcher = app.Services.GetRequiredService<IMessageDispatcher>();
await messageDispatcher.StartAsync();

// Ensure graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(async () =>
{
    await messageDispatcher.StopAsync();
});

app.Run();
```

## Relevant Patterns from CLAUDE.md

- **Service Organization**: Module pattern for DI registration
- **Convention-based Discovery**: Auto-register services ending with "Service", "Repo", "Portal"
- **Service Registration Patterns**: Different scopes based on service type (Singleton for Dispatcher, Scoped for others)
- **Module Pattern**: Static module classes for organized DI registration
- **Message-Driven Architecture**: Auto-discovery and registration of message subscribers

## Deliverables

1. `IServiceModule.cs` - Base interface for service modules
2. `CoreServicesModule.cs` - Core layer service registration
3. `InfrastructureServicesModule.cs` - Infrastructure layer registration including EF and portals
4. `ApiServicesModule.cs` - Web API specific services (auth, swagger, CORS)
5. `ServiceCollectionExtensions.cs` - Extension methods for service registration
6. `Program.cs` - Updated program entry point with module-based registration

## Acceptance Criteria

- All services are registered following naming conventions
- Convention-based discovery works for *Service, *Repo, *Portal classes
- Proper scoping is applied (Singleton for dispatchers, Scoped for most services)
- Database context is properly configured with PostgreSQL and NodaTime
- JWT authentication is configured
- Swagger documentation is available in development
- Health checks are configured
- Message dispatcher starts with application
- Graceful shutdown is implemented
- All modules load in correct order