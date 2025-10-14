using Microsoft.Extensions.Configuration;
using Service.IService;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Service.Service
{
    public class GeminiAiService : IGenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

        public GeminiAiService(IConfiguration config, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = config["GoogleAI:ApiKey"];

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentNullException("GoogleAI:ApiKey is missing in configuration");
            }
        }

        public async Task<string> SummarizeAsync(string text, string model = null, int maxOutputTokens = 800)
        {
            model ??= "gemini-1.5-flash"; // Dùng model nhẹ hơn, nhanh hơn

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = text }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = maxOutputTokens,
                    temperature = 0.2
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var url = $"{_baseUrl}{model}:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(
                url,
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API error: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            // Parse response
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content))
                {
                    if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var textElement))
                        {
                            return textElement.GetString() ?? string.Empty;
                        }
                    }
                }
            }

            throw new Exception("Failed to parse response from Gemini API");
        }

        public async Task<string> AnalyzeDocumentAsync(string text, string analysisType)
        {
            var prompt = analysisType.ToLower() switch
            {
                "overall" => $"Provide a comprehensive overall summary of this academic document. Focus on main arguments, structure, and key findings: {text}",
                "strengths" => $"Identify and list the key strengths of this academic document. Consider clarity, evidence, structure, and originality: {text}",
                "weaknesses" => $"Identify and list the main weaknesses or areas for improvement in this academic document. Be constructive: {text}",
                "recommendations" => $"Provide specific, actionable recommendations to improve this academic document: {text}",
                "keypoints" => $"Extract the key points and main ideas from this academic document. Be concise: {text}",
                _ => $"Analyze this academic document and provide detailed insights: {text}"
            };

            return await SummarizeAsync(prompt, maxOutputTokens: 1000);
        }
    }
}