using Microsoft.AspNetCore.Mvc;

namespace SocialAnimal.Web.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }
}