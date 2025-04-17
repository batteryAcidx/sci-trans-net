namespace SciTransNet.Models
{
    public class TranslationResponse
    {
        public string Original { get; set; }
        public string Mode { get; set; }
        public string Summary { get; set; }
        public string Explanation { get; set; }
        public List<string> KeyTerms { get; set; }
        
        // Constructor to initialize properties
        public TranslationResponse()
        {
            Summary = string.Empty;
            Explanation = string.Empty;
            KeyTerms = new List<string>();
        }
    }
}