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