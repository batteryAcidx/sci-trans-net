using Microsoft.AspNetCore.Http;

namespace SciTransNet.Services.Interfaces
{
    public interface IFileParserService
    {
        Task<string> ExtractTextAsync(IFormFile file);
    }
}