using Microsoft.AspNetCore.Mvc;

namespace Ocultum.Web.Controllers.Pages;

public sealed class AboutPageController : Controller
{
    [HttpGet("/about")]
    public IActionResult Index() => View();
}
