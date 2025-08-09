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
        
        _logger.LogInformation("Retrieved {0} upcoming events", events.Length);
        
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
        
        _logger.LogInformation("Event created: {0} ({1})", created.Id, created.Title);
        
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