using System.Threading.Tasks;
using SciTransNet.Models;

namespace SciTransNet.Services.Interfaces
{
    public interface ITranslationService
    {
        Task<TranslationResponse> TranslateAsync(string inputText, string mode);
    }
}