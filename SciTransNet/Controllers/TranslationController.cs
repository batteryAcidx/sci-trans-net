using Microsoft.AspNetCore.Mvc;
using SciTransNet.Services.Interfaces;

namespace SciTransNet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;

        public TranslationController(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        [HttpPost]
        public async Task<IActionResult> Translate([FromBody] string input)
        {
            var result = await _translationService.TranslateAsync(input);
            return Ok(new { original = input, translated = result });
        }
    }
}