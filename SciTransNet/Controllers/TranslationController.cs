using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SciTransNet.Services.Interfaces;

namespace SciTransNet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(ITranslationService translationService, ILogger<TranslationController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        // Improved input validation
        [HttpPost]
        public async Task<IActionResult> Translate([FromBody] string input)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("Received empty or invalid input for translation.");
                return BadRequest(new { error = "Input text cannot be empty." });
            }

            try
            {
                var result = await _translationService.TranslateAsync(input);

                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogWarning("Translation returned no results.");
                    return StatusCode(500, new { error = "Translation failed. Please try again." });
                }

                return Ok(new { original = input, translated = result });
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                _logger.LogError(ex, "Error occurred while translating text.");
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}