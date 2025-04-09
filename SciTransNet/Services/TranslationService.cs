using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SciTransNet.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace SciTransNet.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<TranslationService> _logger;

        public TranslationService(HttpClient httpClient, IConfiguration configuration, ILogger<TranslationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Check if the API Key is present in the configuration
            _apiKey = configuration["HuggingFace:ApiKey"];

            // If the API Key is null or empty, throw an exception
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentException("HuggingFace API Key is missing or invalid in the configuration.");
            }
        }

        public async Task<string> TranslateAsync(string inputText)
        {
            var payload = new
            {
                inputs = inputText
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                // Perform POST request to HuggingFace API
                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/Helsinki-NLP/opus-mt-en-es", // Model for translation to Spanish
                    content
                );

                // Log the response status code
                _logger.LogInformation($"HuggingFace API Response: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    // Get the error message from the response
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error from HuggingFace API: {error}");
                    return $"API request failed. Status code: {response.StatusCode}, Error: {error}";
                }

                // Read and parse the result from the response
                var resultJson = await response.Content.ReadAsStringAsync();

                // Attempt to parse the response and extract the translated text
                using (JsonDocument doc = JsonDocument.Parse(resultJson))
                {
                    var translationText = doc.RootElement[0].GetProperty("translation_text").GetString();
                    return translationText ?? "No translation found.";
                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                _logger.LogError($"Exception occurred: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }
    }
}