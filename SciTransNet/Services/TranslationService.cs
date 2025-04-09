using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SciTransNet.Services.Interfaces;

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
            _apiKey = configuration["HuggingFace:ApiKey"];

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentException("Missing HuggingFace API key in configuration.");
            }
        }

        public async Task<TranslationResponse> TranslateAsync(string inputText, string mode)
        {
            var prompt = BuildPrompt(inputText, mode);

            var payload = new
            {
                inputs = prompt
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/HuggingFaceH4/zephyr-7b-beta", content
                );

                _logger.LogInformation($"HuggingFace API Response: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API error: {error}");
                    return new TranslationResponse
                    {
                        Original = inputText,
                        Mode = mode,
                        Summary = $"API error: {response.StatusCode} - {error}"
                    };
                }

                var resultJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(resultJson);
                var fullText = doc.RootElement[0].GetProperty("generated_text").GetString();

                return ParseStructuredResponse(inputText, mode, fullText ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Translation failed");
                return new TranslationResponse
                {
                    Original = inputText,
                    Mode = mode,
                    Summary = $"Exception: {ex.Message}"
                };
            }
        }

        private string BuildPrompt(string input, string mode)
        {
            return mode switch
            {
                "simplify" => $"You are a science tutor explaining to a high school student. Rewrite the following in very simple English. Keep it short, friendly, and easy to understand:\n\n\"{input}\"\n\nYour response should include:\n1. A short summary\n2. A simple explanation\n3. A list of key terms\nFormat the output in JSON with keys: summary, explanation, keyTerms.",
                "academic" => $"You are an academic researcher. Rewrite the following sentence with technical precision and scholarly tone, elaborating on relevant terms and theories. Keep it under 300 words.\n\n\"{input}\"\n\nReturn the response as a JSON object with: summary, explanation, keyTerms.",
                "concept" => $"Break down this concept by identifying its main components, their relationships, and real-world examples. Structure the response with labeled sections and format it as JSON:\n\n\"{input}\"\n\nYour response should include:\n1. summary\n2. explanation\n3. keyTerms",
                _ => $"Explain this clearly:\n\"{input}\""
            };
        }

        private TranslationResponse ParseStructuredResponse(string original, string mode, string rawResponse)
        {
            try
            {
                var result = JsonSerializer.Deserialize<TranslationResponse>(rawResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                    throw new Exception("Deserialization returned null.");

                result.Original = original;
                result.Mode = mode;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to parse structured response. Returning fallback format.");
                return new TranslationResponse
                {
                    Original = original,
                    Mode = mode,
                    Summary = rawResponse
                };
            }
        }
    }
}