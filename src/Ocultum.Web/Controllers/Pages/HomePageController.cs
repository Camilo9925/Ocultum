using Microsoft.AspNetCore.Mvc;

namespace Ocultum.Web.Controllers.Pages;

public sealed class HomePageController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();
}
