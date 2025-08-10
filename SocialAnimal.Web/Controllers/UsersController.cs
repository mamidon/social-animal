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
    private readonly ICrudRepo _crudRepo;
    private readonly ILoggerPortal _logger;
    private readonly IClockPortal _clock;
    
    public UsersController(
        IUserRepo userRepo,
        ICrudRepo crudRepo,
        ILoggerPortal logger,
        IClockPortal clock)
    {
        _userRepo = userRepo;
        _crudRepo = crudRepo;
        _logger = logger;
        _clock = clock;
    }
    
    [HttpGet("{id:long}", Name = "GetUserById")]
    public async Task<IActionResult> GetById(long id)
    {
        var user = await _userRepo.Users.FindByIdAsync(id);
        return HandleResult(user);
    }
    
    [HttpGet("slug/{slug}", Name = "GetUserBySlug")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var user = await _userRepo.GetBySlugAsync(slug);
        return HandleResult(user);
    }
    
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UserRegistrationStub stub)
    {
        // Validate uniqueness
        if (await _userRepo.GetByPhoneAsync(stub.Phone) != null)
        {
            ModelState.AddModelError(nameof(stub.Phone), "Phone number is already in use");
        }
        
        if (await _userRepo.SlugExistsAsync(stub.Slug))
        {
            ModelState.AddModelError(nameof(stub.Slug), "Slug is already taken");
        }
        
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }
        
        // Create user record
        var userRecord = new UserRecord
        {
            Id = 0, // Will be set by database
            Slug = stub.Slug,
            Phone = stub.Phone,
            FirstName = stub.FirstName,
            LastName = stub.LastName,
            DeletedAt = null,
            CreatedOn = _clock.Now,
            UpdatedOn = null,
            ConcurrencyToken = null
        };
        
        var created = await _crudRepo.CreateAsync(userRecord);
        
        _logger.LogInformation("User registered: {0} ({1})", created.Id, created.Phone);
        
        return Created("GetUserById", new { id = created.Id }, created);
    }
}

public record UserRegistrationStub
{
    public required string Slug { get; init; }
    public required string Phone { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}