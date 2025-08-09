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
        
        _logger.LogInformation("User registered: {0} ({1})", created.Id, created.Email);
        
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