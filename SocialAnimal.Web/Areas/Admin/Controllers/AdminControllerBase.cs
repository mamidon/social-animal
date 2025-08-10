using Microsoft.AspNetCore.Mvc;

namespace SocialAnimal.Web.Areas.Admin.Controllers;

[Area("Admin")]
public abstract class AdminControllerBase : Controller
{
    /// <summary>
    /// Checks if the current request is an HTMX request
    /// </summary>
    protected bool IsHtmxRequest()
    {
        return Request.Headers.ContainsKey("HX-Request");
    }
    
    /// <summary>
    /// Returns a redirect that works with HTMX
    /// </summary>
    protected IActionResult HtmxRedirect(string url)
    {
        if (IsHtmxRequest())
        {
            // For HTMX requests, use HX-Redirect header
            Response.Headers["HX-Redirect"] = url;
            return NoContent();
        }
        
        return Redirect(url);
    }
    
    /// <summary>
    /// Returns either a partial view (for HTMX) or full view (for regular requests)
    /// </summary>
    protected IActionResult PartialOrFull(string viewName, object? model = null)
    {
        if (IsHtmxRequest())
        {
            return PartialView(viewName, model);
        }
        
        return View(viewName, model);
    }
    
    /// <summary>
    /// Sets up common view data for all admin views
    /// </summary>
    protected void SetupViewData()
    {
        ViewData["IsHtmxRequest"] = IsHtmxRequest();
        ViewData["ControllerName"] = ControllerContext.ActionDescriptor.ControllerName;
        ViewData["ActionName"] = ControllerContext.ActionDescriptor.ActionName;
    }
    
    /// <summary>
    /// Handles errors in a consistent way for both HTMX and regular requests
    /// </summary>
    protected IActionResult HandleError(Exception exception, string userMessage = "An error occurred")
    {
        // Log the exception
        // In a real application, you would inject ILogger here
        
        if (IsHtmxRequest())
        {
            // For HTMX requests, return a partial error view
            return PartialView("_Error", userMessage);
        }
        
        // For regular requests, return full error view
        return View("Error", userMessage);
    }
    
    /// <summary>
    /// Returns a success response for HTMX requests
    /// </summary>
    protected IActionResult HtmxSuccess(string? message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            TempData["SuccessMessage"] = message;
        }
        
        return NoContent();
    }
    
    /// <summary>
    /// Override OnActionExecuting to setup common view data
    /// </summary>
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        SetupViewData();
        base.OnActionExecuting(context);
    }
}