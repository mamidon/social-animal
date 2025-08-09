Create minimal API controllers and configuration files

This task creates the initial API controllers following REST conventions and naming patterns from CLAUDE.md, along with the necessary configuration files to run the application.

## Work to be Done

### Base API Controller
Create `BaseApiController.cs` in `SocialAnimal.Web/Controllers/BaseApiController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace SocialAnimal.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult HandleResult<T>(T? result)
    {
        if (result == null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }
    
    protected IActionResult Created<T>(string routeName, object routeValues, T result)
    {
        return CreatedAtRoute(routeName, routeValues, result);
    }
    
    protected new IActionResult ValidationProblem()
    {
        return BadRequest(ModelState);
    }
}
```

### Health Controller
Create `HealthController.cs` in `SocialAnimal.Web/Controllers/HealthController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NodaTime;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IClockPortal _clock;
    private readonly IConfigurationPortal _configuration;
    
    public HealthController(
        HealthCheckService healthCheckService,
        IClockPortal clock,
        IConfigurationPortal configuration)
    {
        _healthCheckService = healthCheckService;
        _clock = clock;
        _configuration = configuration;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        
        var response = new
        {
            Status = report.Status.ToString(),
            Timestamp = _clock.Now.ToString(),
            Environment = _configuration.EnvironmentName,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration.TotalMilliseconds,
                Description = e.Value.Description,
                Exception = e.Value.Exception?.Message
            })
        };
        
        return report.Status == HealthStatus.Healthy ? Ok(response) : StatusCode(503, response);
    }
    
    [HttpGet("ready")]
    public IActionResult Ready()
    {
        // Simple readiness check
        return Ok(new { Ready = true, Timestamp = _clock.Now.ToString() });
    }
    
    [HttpGet("live")]
    public IActionResult Live()
    {
        // Simple liveness check
        return Ok(new { Alive = true, Timestamp = _clock.Now.ToString() });
    }
}
```

### Users Controller Stub
Create `UsersController.cs` in `SocialAnimal.Web/Controllers/UsersController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Web.Controllers;

public class UsersController : BaseApiController
{
    private readonly IUserRepo _userRepo;
    private readonly ILoggerPortal _logger;
    private readonly IClockPortal _clock;
    
    public UsersController(
        IUserRepo userRepo,
        ILoggerPortal logger,
        IClockPortal clock)
    {
        _userRepo = userRepo;
        _logger = logger;
        _clock = clock;
    }
    
    [HttpGet("{id:long}", Name = "GetUserById")]
    public async Task<IActionResult> GetById(long id)
    {
        var user = await _userRepo.Users.FindByIdAsync(id);
        return HandleResult(user);
    }
    
    [HttpGet("handle/{handle}", Name = "GetUserByHandle")]
    public async Task<IActionResult> GetByHandle(string handle)
    {
        var user = await _userRepo.FindByHandleAsync(handle);
        return HandleResult(user);
    }
    
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UserRegistrationStub stub)
    {
        // Validate uniqueness
        if (!await _userRepo.IsEmailUniqueAsync(stub.Email))
        {
            ModelState.AddModelError(nameof(stub.Email), "Email is already in use");
        }
        
        if (!await _userRepo.IsHandleUniqueAsync(stub.Handle))
        {
            ModelState.AddModelError(nameof(stub.Handle), "Handle is already taken");
        }
        
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }
        
        // Create user record
        var userRecord = new UserRecord
        {
            Id = 0, // Will be set by database
            Handle = stub.Handle,
            Email = stub.Email,
            FirstName = stub.FirstName,
            LastName = stub.LastName,
            Reference = $"user_{Guid.NewGuid():N}",
            IsActive = true,
            IsEmailVerified = false,
            CreatedOn = _clock.Now,
            UpdatedOn = null,
            ConcurrencyToken = null
        };
        
        var created = await _userRepo.CreateAsync(userRecord);
        
        _logger.LogInformation("User registered: {UserId} ({Email})", created.Id, created.Email);
        
        return Created("GetUserById", new { id = created.Id }, created);
    }
}

public record UserRegistrationStub
{
    public required string Handle { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Password { get; init; }
}
```

### Events Controller Stub
Create `EventsController.cs` in `SocialAnimal.Web/Controllers/EventsController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Web.Controllers;

[Authorize]
public class EventsController : BaseApiController
{
    private readonly ILoggerPortal _logger;
    private readonly IClockPortal _clock;
    
    public EventsController(
        ILoggerPortal logger,
        IClockPortal clock)
    {
        _logger = logger;
        _clock = clock;
    }
    
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetUpcoming()
    {
        // Placeholder implementation
        var events = new[]
        {
            new
            {
                Id = 1,
                Title = "Sample Event",
                Description = "This is a placeholder event",
                StartsOn = _clock.Now.Plus(Duration.FromDays(7)),
                Location = "Virtual"
            }
        };
        
        _logger.LogInformation("Retrieved {Count} upcoming events", events.Length);
        
        return Ok(events);
    }
    
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public IActionResult GetById(long id)
    {
        // Placeholder implementation
        var eventData = new
        {
            Id = id,
            Title = "Sample Event",
            Description = "This is a placeholder event",
            StartsOn = _clock.Now.Plus(Duration.FromDays(7)),
            Location = "Virtual",
            Organizer = new { Id = 1, Name = "John Doe" }
        };
        
        return Ok(eventData);
    }
    
    [HttpPost]
    public IActionResult CreateEvent([FromBody] CreateEventStub stub)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }
        
        // Placeholder implementation
        var created = new
        {
            Id = 1,
            Handle = $"event-{Guid.NewGuid():N}".Substring(0, 12),
            Title = stub.Title,
            Description = stub.Description,
            StartsOn = stub.StartsOn,
            EndsOn = stub.EndsOn,
            Location = stub.Location,
            CreatedOn = _clock.Now
        };
        
        _logger.LogInformation("Event created: {EventId} ({Title})", created.Id, created.Title);
        
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}

public record CreateEventStub
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required Instant StartsOn { get; init; }
    public required Instant EndsOn { get; init; }
    public string? Location { get; init; }
    public int? MaxAttendees { get; init; }
}
```

### Application Settings
Create `appsettings.json` in `SocialAnimal.Web/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=socialanimal;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "ThisIsATemporarySecretKeyForDevelopmentOnly-ChangeInProduction",
    "Issuer": "SocialAnimal",
    "Audience": "SocialAnimalUsers",
    "ExpirationHours": 24
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173"
    ]
  },
  "AllowedHosts": "*"
}
```

### Development Settings
Create `appsettings.Development.json` in `SocialAnimal.Web/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.EntityFrameworkCore": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=socialanimal_dev;Username=postgres;Password=postgres"
  }
}
```

### Launch Settings
Create `launchSettings.json` in `SocialAnimal.Web/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:5000",
      "sslPort": 5001
    }
  },
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Git Ignore File
Create `.gitignore` in the solution root:

```gitignore
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# User-specific files (MonoDevelop/Xamarin Studio)
*.userprefs

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio 2015/2017 cache/options directory
.vs/

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/

# Files built by Visual Studio
*.obj
*.pdb
*.exe
*.dll

# NuGet Packages
*.nupkg
*.snupkg
# The packages folder can be ignored because of Package Restore
**/[Pp]ackages/*
# except build/, which is used as an MSBuild target.
!**/[Pp]ackages/build/
# NuGet Symbol Packages
*.nuget.props
*.nuget.targets

# Entity Framework
*.edmx.diagram
*.edmx.sql

# Database files
*.mdf
*.ldf
*.ndf

# Rider
.idea/
*.sln.iml

# Visual Studio Code
.vscode/

# Mac
.DS_Store

# Environment files
*.env
appsettings.Production.json
appsettings.Staging.json

# Test Results
TestResults/
```

## Relevant Patterns from CLAUDE.md

- **Naming Conventions**: *Controller suffix for MVC controllers
- **Suffix-Based Classification**: *Stub types for creation/update DTOs
- **Identifier Patterns**: Prefixed external IDs (user_*, event_*), long database IDs
- **Time and Date Handling**: NodaTime Instant for timestamps
- **Configuration Management**: Environment-based configuration files
- **Error Handling**: Structured error responses with validation

## Deliverables

1. `BaseApiController.cs` - Base controller with common functionality
2. `HealthController.cs` - Health check endpoints
3. `UsersController.cs` - User registration and retrieval endpoints
4. `EventsController.cs` - Event management endpoints (placeholder)
5. `appsettings.json` - Base application configuration
6. `appsettings.Development.json` - Development-specific settings
7. `launchSettings.json` - Launch profiles for development
8. `.gitignore` - Git ignore file for .NET projects

## Acceptance Criteria

- All controllers follow REST conventions and naming patterns
- Health check endpoints return proper status codes
- User registration validates uniqueness constraints
- Controllers use portal interfaces for logging and time
- Configuration files support different environments
- JWT settings are configured (but implementation deferred)
- Swagger UI is accessible at /swagger in development
- Application runs on http://localhost:5000 and https://localhost:5001
- Sensitive configuration is excluded from source control