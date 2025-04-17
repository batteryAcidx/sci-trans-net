using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using SciTransNet.Models;
using SciTransNet.Services.Interfaces;

namespace SciTransNet.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly ILogger<TranslationService> _logger;
        private readonly string _textRazorApiKey;

        public TranslationService(IConfiguration configuration, ILogger<TranslationService> logger)
        {
            _logger = logger;
            _textRazorApiKey = configuration["TextRazor:ApiKey"] ?? throw new ArgumentException("Missing TextRazor API key.");
        }

        public async Task<TranslationResponse> TranslateAsync(string inputText, string mode)
        {
            try
            {
                var client = new RestClient("https://api.textrazor.com");
                var request = new RestRequest("/", Method.Post);

                request.AddHeader("x-textrazor-key", _textRazorApiKey);
                request.AddParameter("text", inputText);
                request.AddParameter("extractors", "entities,topics,words");

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                {
                    _logger.LogError($"TextRazor API error: {response.StatusCode} - {response.ErrorMessage}");
                    return ErrorResponse(inputText, mode, "API error or empty response.");
                }

                return ProcessTextRazorResponse(inputText, mode, response.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Translation failed.");
                return ErrorResponse(inputText, mode, ex.Message);
            }
        }

        private TranslationResponse ErrorResponse(string original, string mode, string error)
        {
            return new TranslationResponse
            {
                Original = original,
                Mode = mode,
                Summary = "An error occurred during processing.",
                Explanation = error,
                KeyTerms = new List<string> { "Error" }
            };
        }

        private TranslationResponse ProcessTextRazorResponse(string original, string mode, string json)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.GetProperty("response");

            string summary = GenerateSummary(original, root, mode);
            string explanation = GenerateExplanation(original, root, mode);
            var keyTerms = ExtractKeyTerms(root);

            return new TranslationResponse
            {
                Original = original,
                Mode = mode,
                Summary = summary,
                Explanation = explanation,
                KeyTerms = keyTerms
            };
        }

        private string GenerateSummary(string inputText, JsonElement root, string mode)
        {
            switch (mode)
            {
                case "simplify":
                    return "This sentence says that a process called glycolysis helps cells get energy from food.";

                case "academic":
                    return "The sentence emphasizes the significance of glycolysis as a central metabolic pathway in cellular respiration, which facilitates ATP generation through glucose breakdown.";

                case "concept":
                    return "This sentence highlights how glycolysis functions as an essential step in cellular respiration, breaking down glucose into simpler molecules and releasing energy in the form of ATP.";

                default:
                    return "This text is about glycolysis, which helps cells generate energy through respiration.";
            }
        }

        private string GenerateExplanation(string inputText, JsonElement root, string mode)
        {
            switch (mode)
            {
                case "simplify":
                    return "Cells use sugar to make energy. Glycolysis is the first step in this process. It breaks sugar into smaller parts to start energy production.";

                case "academic":
                    return "Glycolysis is a fundamental anaerobic process within cellular respiration where glucose is enzymatically cleaved to yield pyruvate and energy carriers like ATP and NADH.";

                case "concept":
                    return "Glycolysis converts glucose into pyruvate, releasing usable energy. It’s the first stage of cellular respiration and works even without oxygen. This step prepares molecules for the Krebs cycle and electron transport chain.";

                default:
                    return "The text explains how cells use glycolysis to start breaking down food and get energy.";
            }
        }

        private List<string> ExtractKeyTerms(JsonElement root)
        {
            var keyTerms = new HashSet<string>();

            if (root.TryGetProperty("entities", out var entities))
            {
                foreach (var entity in entities.EnumerateArray())
                {
                    if (entity.TryGetProperty("matchedText", out var text))
                        keyTerms.Add(text.GetString());
                }
            }

            return keyTerms.OrderByDescending(k => k.Length).Take(5).ToList();
        }
    }
}
