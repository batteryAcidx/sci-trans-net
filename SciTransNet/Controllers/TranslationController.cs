using Microsoft.AspNetCore.Mvc;
using SciTransNet.Models;
using SciTransNet.Services.Interfaces;

namespace SciTransNet.Controllers
{
    [Route("api/translate")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;

        public TranslationController(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        [HttpPost("translate")]
        public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OriginalText) || string.IsNullOrWhiteSpace(request.Mode))
            {
                return BadRequest(new { message = "Original text and mode are required." });
            }

            try
            {
                var response = await _translationService.TranslateAsync(request.OriginalText, request.Mode);

                if (response == null)
                {
                    return BadRequest(new { message = "Translation failed." });
                }

                return Ok(new
                {
                    original = response.Original,
                    mode = response.Mode,
                    summary = response.Summary,
                    explanation = response.Explanation,
                    keyTerms = response.KeyTerms
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during translation.", error = ex.Message });
            }
        }
    }
}