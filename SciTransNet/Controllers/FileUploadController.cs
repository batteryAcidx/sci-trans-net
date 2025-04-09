using Microsoft.AspNetCore.Mvc;
using SciTransNet.Services.Interfaces;

namespace SciTransNet.Controllers
{
    [ApiController]
    [Route("api/translate")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileParserService _fileParser;

        public FileUploadController(IFileParserService fileParser)
        {
            _fileParser = fileParser;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> ExtractText(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var text = await _fileParser.ExtractTextAsync(file);

            return Ok(new { fileName = file.FileName, content = text });
        }
    }
}