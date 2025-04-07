using Microsoft.AspNetCore.Mvc;

namespace SciTransNet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        [HttpPost("test")]
        public IActionResult Test([FromBody] string input)
        {
            return Ok(new
            {
                Original = input,
                Translated = "This is a test response from SciTransNet backend 🚀"
            });
        }
    }
}