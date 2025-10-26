using BussinessObject.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GeminiAiService> _logger;
        private readonly string _apiKey = "AIzaSyBfzfx2RpGUOiFduvSDFNiw_tqh2ttjqX0";
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

        public GeminiAiService(HttpClient httpClient, ILogger<GeminiAiService> logger)
        {
            _httpClient = httpClient;

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentNullException("GoogleAI:ApiKey is missing in configuration");
            }
        }

        public async Task<string> SummarizeAsync(string text, string model = null, int maxOutputTokens = 800)
        {
            model ??= "gemini-2.0-flash"; // Dùng model nhẹ hơn, nhanh hơn

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
        public async Task<string> AnalyzeDocumentWithContextAsync(string text, string analysisType, string context)
        {
            var prompt = analysisType.ToLower() switch
            {
                "overall" =>
                    $"{context}\n\n" +
                    $"Based on the above assignment context and rubric, provide a comprehensive overall summary and evaluation of this student submission. " +
                    $"Focus on how well it meets the assignment requirements and rubric criteria:\n\n{text}",

                "strengths" =>
                    $"{context}\n\n" +
                    $"Based on the above assignment context and rubric, identify and list the key strengths of this student submission. " +
                    $"Focus on areas where it excels according to the rubric criteria:\n\n{text}",

                "weaknesses" =>
                    $"{context}\n\n" +
                    $"Based on the above assignment context and rubric, identify and list the main weaknesses or areas for improvement in this student submission. " +
                    $"Be constructive and specific about how to improve according to the rubric:\n\n{text}",

                "recommendations" =>
                    $"{context}\n\n" +
                    $"Based on the above assignment context and rubric, provide specific, actionable recommendations to improve this student submission. " +
                    $"Focus on concrete steps the student can take to better meet the assignment requirements:\n\n{text}",

                "keypoints" =>
                    $"{context}\n\n" +
                    $"Based on the above assignment context, extract and evaluate the key points and main ideas from this student submission. " +
                    $"Assess how well they address the assignment requirements:\n\n{text}",

                _ =>
                    $"{context}\n\n" +
                    $"Based on the above assignment context and rubric, analyze this student submission and provide detailed insights:\n\n{text}"
            };

            return await SummarizeAsync(prompt, maxOutputTokens: 1200);
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
        public async Task<string> GenerateReviewAsync(string documentText, string context)
        {
            var prompt = $@"**UNIVERSAL DOCUMENT REVIEW**

                    CONTEXT INFORMATION:
                    {context}

                    DOCUMENT CONTENT:
                    {documentText}

                    **REVIEW REQUIREMENTS:**
                    Provide a balanced, field-agnostic evaluation that works for any discipline (business, marketing, communications, engineering, etc.). Focus on universal academic and professional standards.

                    **REVIEW STRUCTURE - Use exactly these sections:**

                    **OVERALL EVALUATION**
                    [Provide 2-3 concise sentences giving a balanced overall assessment]

                    **CRITERIA ASSESSMENT**
                    [Rate each criterion from context using 1-5 stars. Example:
                    - Content Quality: ★★★★☆
                    - Organization: ★★★☆☆
                    - Analysis: ★★★★★]

                    **KEY STRENGTHS**
                    • [Most significant strength]
                    • [Second key strength] 
                    • [Third notable strength]

                    **IMPROVEMENT OPPORTUNITIES**
                    • [Most important area for improvement]
                    • [Second area to develop]
                    • [Third suggestion for growth]

                    **RECOMMENDATIONS**
                    • [Most actionable recommendation]
                    • [Second practical suggestion]

                    **GUIDELINES:**
                    - Be objective and evidence-based
                    - Use simple, clear language
                    - Focus on what's actually in the document
                    - Balance positive and constructive feedback
                    - Keep total under 300 words
                    - Use bullet points for clarity";

            return await SummarizeAsync(prompt, maxOutputTokens: 600);
        }
        public async Task<string> GenerateEnhancedReviewAsync(string documentText, string context, List<Criteria> criteria)
        {
            var criteriaSection = "";
            if (criteria != null && criteria.Any())
            {
                criteriaSection = "\n\n**DETAILED CRITERIA ANALYSIS:**\n";
                foreach (var criterion in criteria)
                {
                    criteriaSection += $"- {criterion.Title}: [AI will analyze this specific criterion]\n";
                }
            }

            var prompt = $@"**ENHANCED DOCUMENT REVIEW**

            CONTEXT INFORMATION:
            {context}

            DOCUMENT CONTENT:
            {documentText}

            **REVIEW STRUCTURE - Provide analysis in this exact format:**

            **OVERALL ASSESSMENT**
            [2-3 sentences giving balanced overall evaluation]

            **CRITERIA-BASED EVALUATION**
            {criteriaSection}

            **KEY STRENGTHS**
            • [Most significant strength]
            • [Second key strength] 
            • [Third notable strength]

            **AREAS FOR IMPROVEMENT**
            • [Most important improvement area]
            • [Second area to develop]

            **RECOMMENDATIONS**
            • [Most actionable recommendation]
            • [Second practical suggestion]

            **SCORING INSIGHTS**
            [Brief notes on how this might translate to rubric scoring]

            **GUIDELINES:**
            - Be objective and evidence-based
            - Reference specific criteria from the context
            - Balance positive and constructive feedback
            - Keep total under 400 words";

            return await SummarizeAsync(prompt, maxOutputTokens: 800);
        }
        public async Task<string> GenerateCriteriaReviewAsync(string documentText, Criteria criteria, string context)
        {
            var prompt = $@"**CRITERIA-SPECIFIC REVIEW: {criteria.Title}**

            CONTEXT:
            {context}

            CRITERIA DETAILS:
            - Title: {criteria.Title}
            - Description: {criteria.Description}
            - Weight: {criteria.Weight}%
            - Max Score: {criteria.MaxScore}

            DOCUMENT CONTENT:
            {documentText}

            **REQUIREMENTS:**
            Provide focused analysis specifically for this criterion. Evaluate how well the document addresses this particular aspect.

            **ANALYSIS FORMAT:**
            - Performance Level: [Excellent/Good/Fair/Poor]
            - Key Observations: [2-3 specific points]
            - Evidence: [Quotes or examples from document]
            - Suggestions: [1-2 specific improvements]

            **KEEP UNDER 150 WORDS**";

            return await SummarizeAsync(prompt, maxOutputTokens: 300);
        }
    }
}