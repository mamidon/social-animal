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