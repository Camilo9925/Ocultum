using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ocultum.Core.Validation;
using Ocultum.Web.ViewModels;

namespace Ocultum.Web.Controllers.Pages;

public sealed class ExtractPageController(IOptions<LimitsOptions> limits) : Controller
{
    [HttpGet("/extract")]
    public IActionResult Index() =>
        View(new ExtractPageViewModel(limits.Value.MaxCoverBytes, limits.Value.MaxPassphraseLength));
}
