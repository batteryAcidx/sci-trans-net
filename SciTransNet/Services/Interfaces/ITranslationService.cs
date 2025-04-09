using System.Threading.Tasks;

namespace SciTransNet.Services.Interfaces
{
    public interface ITranslationService
    {
        Task<TranslationResponse> TranslateAsync(string inputText, string mode);
    }

    public class TranslationResponse
    {
        public string Original { get; set; } = string.Empty;
        public string Mode { get; set; } = "";
        public string Summary { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public List<string> KeyTerms { get; set; } = new();
    }
}