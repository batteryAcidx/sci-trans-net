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
                inputs = "Translate to simple English: " + inputText
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            int retries = 3;
            while (retries > 0)
            {
                try
                {
                    var response = await _httpClient.PostAsync(
                        "https://api-inference.huggingface.co/models/google/flan-t5-small", content
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var resultJson = await response.Content.ReadAsStringAsync();
                        return resultJson;
                    }
                    else
                    {
                        retries--;
                        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && retries > 0)
                        {
                            await Task.Delay(1000);
                        }
                        else
                        {
                            return $"Request error: {response.StatusCode}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (retries > 0)
                    {
                        await Task.Delay(1000);
                    }
                    else
                    {
                        return $"Exception: {ex.Message}";
                    }
                }
            }

            return "Failed after retries.";
        }
    }
}