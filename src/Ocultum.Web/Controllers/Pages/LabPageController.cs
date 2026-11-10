using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ocultum.Core.Validation;
using Ocultum.Web.ViewModels;

namespace Ocultum.Web.Controllers.Pages;

public sealed class LabPageController(IOptions<LimitsOptions> limits) : Controller
{
    [HttpGet("/lab")]
    public IActionResult Index() =>
        View(new LabPageViewModel(limits.Value.MaxCoverBytes, limits.Value.MaxPassphraseLength));
}
