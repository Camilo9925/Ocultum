using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ocultum.Core.Validation;
using Ocultum.Web.ViewModels;

namespace Ocultum.Web.Controllers.Pages;

public sealed class EmbedPageController(IOptions<LimitsOptions> limits) : Controller
{
    [HttpGet("/embed")]
    public IActionResult Index()
    {
        var value = limits.Value;
        return View(new EmbedPageViewModel(value.MaxCoverBytes, value.MaxPayloadBytes, value.MaxPayloadFiles, value.MaxPassphraseLength));
    }
}
