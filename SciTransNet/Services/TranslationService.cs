using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SciTransNet.Services.Interfaces;

namespace SciTransNet.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public TranslationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["HuggingFace:ApiKey"];
        }

        public async Task<string> TranslateAsync(string inputText)
        {
            try
            {
                var payload = new
                {
                    inputs = "Translate to simple English: " + inputText
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/google/flan-t5-small",
                    content
                );

                response.EnsureSuccessStatusCode();  // Throws an exception if the response code is not successful

                var resultJson = await response.Content.ReadAsStringAsync();

                // Deserialize response to extract generated text
                var jsonResponse = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(resultJson);
                string simplifiedText = jsonResponse?.FirstOrDefault()?["generated_text"];
                
                return simplifiedText ?? "No simplification found.";  // Fallback if the result is empty
            }
            catch (HttpRequestException ex)
            {
                // Handle network errors
                return $"Request error: {ex.Message}";
            }
            catch (Exception ex)
            {
                // Handle other errors (like deserialization failures)
                return $"Error: {ex.Message}";
            }
        }
    }
}