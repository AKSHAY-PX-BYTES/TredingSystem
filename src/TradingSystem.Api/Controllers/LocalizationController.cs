using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[ApiController]
[Route("localization")]
[Produces("application/json")]
public class LocalizationController : ControllerBase
{
    private readonly ILocalizationService _localizationService;

    public LocalizationController(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    [HttpGet("locales")]
    public IActionResult GetLocales()
    {
        return Ok(ApiResponse<List<LocaleInfo>>.Ok(_localizationService.GetSupportedLocales()));
    }

    [HttpGet("translations/{locale}")]
    public IActionResult GetTranslations(string locale)
    {
        var translations = _localizationService.GetTranslations(locale);
        return Ok(ApiResponse<Dictionary<string, string>>.Ok(translations));
    }
}
