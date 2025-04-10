using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SciTransNet.Services.Interfaces;
using SciTransNet.Models;

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

            var payload = new { inputs = prompt };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/mistralai/Mixtral-8x7B-Instruct-v0.1", content
                );

                _logger.LogInformation($"HuggingFace API Response: {response.StatusCode}");

                var resultJson = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw model output: " + resultJson);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API error: " + resultJson);
                    return new TranslationResponse
                    {
                        Original = inputText,
                        Mode = mode,
                        Summary = $"API error: {response.StatusCode} - {resultJson}"
                    };
                }

                return ParseStructuredResponse(inputText, mode, resultJson);
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
                "simplify" =>
                    $"Please respond ONLY in this JSON format: {{\"summary\":\"...\",\"explanation\":\"...\",\"keyTerms\":[...]}}.\n" +
                    $"Input: \"{input}\".\n" +
                    $"Simplify it in friendly, easy language for a high school student. Max 100 words per field. Max 5 key terms.",

                "academic" =>
                    $"You are an academic researcher. Rewrite the following sentence with technical precision and scholarly tone. Keep it under 150 words. " +
                    $"Return only this JSON format: {{\"summary\":\"...\",\"explanation\":\"...\",\"keyTerms\":[...]}}:\n\n\"{input}\"",

                "concept" =>
                    $"Break this concept into a JSON structure: {{\"summary\":\"...\",\"explanation\":\"...\",\"keyTerms\":[...],\"components\":{{...}},\"examples\":{{...}}}}.\n\n" +
                    $"Input:\n\"{input}\"",

                _ =>
                    $"Explain clearly: \"{input}\""
            };
        }

        private TranslationResponse ParseStructuredResponse(string original, string mode, string rawResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawResponse);

                if (doc.RootElement.ValueKind == JsonValueKind.Array &&
                    doc.RootElement.GetArrayLength() > 0 &&
                    doc.RootElement[0].TryGetProperty("generated_text", out var gen))
                {
                    var innerText = gen.GetString();

                    if (!string.IsNullOrWhiteSpace(innerText))
                    {
                        int startIndex = innerText.IndexOf('{');
                        int endIndex = innerText.LastIndexOf('}');
                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            var jsonPayload = innerText.Substring(startIndex, endIndex - startIndex + 1);

                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };

                            var parsed = JsonSerializer.Deserialize<TranslationResponse>(jsonPayload, options);
                            if (parsed != null)
                            {
                                parsed.Original = original;
                                parsed.Mode = mode;
                                return parsed;
                            }
                        }
                    }
                }

                throw new Exception("Could not extract structured JSON.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Fallback due to parsing error: " + ex.Message);
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