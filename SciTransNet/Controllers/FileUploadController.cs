using Microsoft.AspNetCore.Mvc;
using SciTransNet.Services.Interfaces;
using SciTransNet.Models;

namespace SciTransNet.Controllers
{
    [ApiController]
    [Route("api/translate")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileParserService _fileParser;
        private readonly ITranslationService _translationService;

        public FileUploadController(IFileParserService fileParser, ITranslationService translationService)
        {
            _fileParser = fileParser;
            _translationService = translationService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> ExtractAndTranslate(IFormFile file, [FromQuery] string mode)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (string.IsNullOrWhiteSpace(mode))
                return BadRequest("Translation mode is required (e.g., simplify, academic, concept).");

            var text = await _fileParser.ExtractTextAsync(file);

            var result = await _translationService.TranslateAsync(text, mode);

            return Ok(new
            {
                fileName = file.FileName,
                originalText = text,
                translation = new
                {
                    result.Summary,
                    result.Explanation,
                    result.KeyTerms
                }
            });
        }
    }
}