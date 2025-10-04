using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Service.IService;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Service.Service
{
    public class GeminiAiService : IGenAIService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly GoogleCredential _googleCredential;
        private readonly string _projectId;
        private readonly string _location;
        private readonly string _modelId;

        public GeminiAiService(IConfiguration config, HttpClient http)
        {
            _http = http;
            _config = config;
            _projectId = config["GCloud:ProjectId"];
            _location = config["GCloud:Location"] ?? "us-central1";
            _modelId = config["GCloud:ModelId"] ?? "models/gemini-1.5-pro"; // adjust

            var saJson = config["GCloud:ServiceAccountJson"];
            if (string.IsNullOrEmpty(saJson))
                throw new ArgumentNullException("GCloud:ServiceAccountJson is missing");

            _googleCredential = GoogleCredential.FromJson(saJson).CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        }

        private async Task<string> GetAccessTokenAsync()
        {
            return await _googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }

        public async Task<string> SummarizeAsync(string text, string model = null, int maxOutputTokens = 800)
        {
            model ??= _modelId;
            var token = await GetAccessTokenAsync();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // NOTE: payload shape must match Vertex AI Generative API for the model version you use.
            // This is a simple generic payload; you should adapt to actual API in production.
            var endpoint = $"https://{_location}-aiplatform.googleapis.com/v1/projects/{_projectId}/locations/{_location}/models/{model}:predict";

            var payload = new
            {
                instances = new[] {
                    new {
                        // depending on API, the field name may differ ("content" or "input" etc)
                        content = text
                    }
                },
                parameters = new
                {
                    maxOutputTokens = maxOutputTokens,
                    temperature = 0.2
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var resp = await _http.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            var respJson = await resp.Content.ReadAsStringAsync();

            // Parse heuristically; you must check actual response structure and adjust
            using var doc = JsonDocument.Parse(respJson);
            // try common path: predictions[0].content
            if (doc.RootElement.TryGetProperty("predictions", out var preds))
            {
                var first = preds[0];
                if (first.TryGetProperty("content", out var contentEl))
                {
                    return contentEl.GetString() ?? string.Empty;
                }
                // alternative
                if (first.TryGetProperty("output", out var outEl))
                {
                    return outEl.ToString();
                }
            }

            // fallback: return whole response
            return respJson;
        }
    }
}
