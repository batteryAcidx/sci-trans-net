using System.Threading.Tasks;

namespace SciTransNet.Services.Interfaces
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string inputText);
    }
}