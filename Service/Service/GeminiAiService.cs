using BussinessObject.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Service.IService;
using Service.RequestAndResponse.Response.AISummary;
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
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

        public GeminiAiService(HttpClient httpClient, ILogger<GeminiAiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _apiKey = configuration["Authentication:GoogleAI:ApiKey"] ?? throw new ArgumentNullException("Authentication:GoogleAI:ApiKey is missing in configuration");
        }

        public async Task<string> SummarizeAsync(string text, string model = null, int maxOutputTokens = 800)
        {
            model ??= "gemma-3-27b-it";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = text } } } },
                generationConfig = new { maxOutputTokens = maxOutputTokens, temperature = 0.2 }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var url = $"{_baseUrl}{model}:generateContent?key={_apiKey}";

            int maxRetries = 3;
            int currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];
                        if (firstCandidate.TryGetProperty("content", out var content))
                        {
                            if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                            {
                                return parts[0].GetProperty("text").GetString() ?? string.Empty;
                            }
                        }
                    }
                    return string.Empty;
                }
                else if ((int)response.StatusCode == 429)
                {
                    currentRetry++;
                    _logger.LogWarning($"Gemini API Rate Limit hit. Retrying in 5 seconds... (Attempt {currentRetry}/{maxRetries})");
                    await Task.Delay(5000);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Gemini API error: {response.StatusCode} - {errorContent}");
                }
            }

            throw new Exception("Gemini API Rate Limit exceeded after multiple retries.");
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

        public async Task<List<AICriteriaFeedbackItem>> GenerateBulkCriteriaFeedbackAsync(string documentText, List<Criteria> criteria, string context)
        {
            var criteriaListText = string.Join("\n", criteria.Select(c =>
                $"- ID:{c.CriteriaId} | Title: {c.Title} | MaxScore: {c.MaxScore} | Desc: {c.Description}"));

            var prompt = $@"**BULK GRADING TASK**
CONTEXT: {context}

CRITERIA LIST:
{criteriaListText}

STUDENT SUBMISSION:
{documentText}

**INSTRUCTION:**
Evaluate the submission against ALL criteria listed above.
Return a STRICT JSON ARRAY where each object corresponds to a criteria.
Format: [ {{ ""CriteriaId"": <int>, ""Score"": <decimal>, ""Summary"": ""<short feedback>"" }}, ... ]
Rules:
1. Score must be between 0.25 and MaxScore.
2. Score must be in increments of 0.25 (e.g., 0.25, 0.50, 0.75, 1.0).
3. Summary must be concise (under 40 words per criteria).
4. Do not include markdown formatting (```json), just the raw JSON string.
";

            var jsonResponse = await SummarizeAsync(prompt, maxOutputTokens: 1000);

            try
            {
                jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var feedbacks = JsonSerializer.Deserialize<List<AICriteriaFeedbackItem>>(jsonResponse, options);

                foreach (var item in feedbacks)
                {
                    var original = criteria.FirstOrDefault(c => c.CriteriaId == item.CriteriaId);
                    if (original != null)
                    {
                        item.Title = original.Title;
                        item.Description = original.Description;
                        item.MaxScore = original.MaxScore;

                        item.Score = Math.Round(item.Score * 4) / 4;

                        if (item.Score < 0.25m)
                        {
                            item.Score = 0.25m;
                        }
                        if (item.Score > original.MaxScore)
                        {
                            item.Score = original.MaxScore;
                        }
                    }
                }
                return feedbacks ?? new List<AICriteriaFeedbackItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing bulk criteria feedback JSON");
                return new List<AICriteriaFeedbackItem>();
            }
        }
        public async Task<string> CheckSubmissionRelevanceAsync(string documentText, string context, string assignmentTitle)
        {
            var prompt = $@"**STRICT ASSIGNMENT RELEVANCE & DOMAIN CHECK**

ASSIGNMENT META:
- Title: {assignmentTitle}
- Requirements/Context: {context}

SUBMITTED DOCUMENT CONTENT (Snippet):
{documentText}

**YOUR ROLE:**
You are a strict exam proctor. Your ONLY job is to verify if the student submitted the correct file for the correct subject.

**CRITICAL CHECKS (Step-by-Step):**
1. **Identify Assignment Domain:** What is the subject of the assignment? (e.g., PR, Marketing, History, Coding, Math).
2. **Identify Submission Domain:** What is the subject of the document? (e.g., Java Code, OOP, Cooking, Essay).
3. **Compare Domains:** Do they match? 
   - Public Relations (PR) != Computer Science (Java/OOP)
   - Marketing != Cooking
   - History != Mathematics

**STRICT RULES FOR 'NOT_RELEVANT':**
- If the Assignment is about **Social Sciences/Business** (PR, Marketing, Econ) and the Submission contains **Programming Code** (Java, C#, Python) -> **NOT_RELEVANT** (Wrong Subject).
- If the Assignment requires an **Essay/Case Study** and the Submission is **Technical Documentation/Code** -> **NOT_RELEVANT**.
- Even if the submission is a *high-quality* paper, if it is for the WRONG SUBJECT, it is **NOT_RELEVANT**.
- If the submission discusses 'Classes', 'Objects', 'Code', 'Functions' but the assignment asks for 'Strategies', 'Campaigns', 'Theories' -> **NOT_RELEVANT**.

**RESPONSE FORMAT:**
Return ONLY ONE of these two responses:

RELEVANT|[Brief reason]

OR

NOT_RELEVANT|[Specific reason: Mention the Subject Mismatch. E.g., 'Assignment is PR but submission is Java Code']

**EXAMPLES:**
- Assign: 'PR Case Study', Sub: 'Java OOP Patterns' -> NOT_RELEVANT|Domain mismatch: Assignment is Public Relations, Submission is Computer Science (Java Code).
- Assign: 'Intro to C#', Sub: 'History of Rome' -> NOT_RELEVANT|Domain mismatch: Assignment is Programming, Submission is History.
- Assign: 'PR Case Study', Sub: 'Analysis of Pepsi Crisis' -> RELEVANT|Matches topic and format (PR Case Study).

NOW EVALUATE THE SUBMISSION ABOVE:";

            return await SummarizeAsync(prompt, maxOutputTokens: 400);
        }
        public async Task<string> GenerateOverallSummaryAsync(string documentText, string context)
        {
            var prompt = $@"**AI OVERALL SUMMARY**

ASSIGNMENT CONTEXT: {context}

DOCUMENT: {documentText}

REQUIREMENTS: Provide a balanced overall summary of the submission (~100 words, max 200). 
Focus on:
- Structure and organization
- Content quality and relevance to assignment
- Key strengths and weaknesses
- Overall impression

Keep response in a single paragraph without line breaks.
Do not include scores or grading in the summary.";

            return await SummarizeAsync(prompt, maxOutputTokens: 400);
        }

        public async Task<string> GenerateCriteriaSummaryAsync(string documentText, Criteria criteria, string context)
        {
            var prompt = $@"**AI CRITERIA SUMMARY: {criteria.Title}**

CONTEXT: {context}

CRITERIA: Title: {criteria.Title}, Desc: {criteria.Description}, Weight: {criteria.Weight}%, MaxScore: {criteria.MaxScore}

DOCUMENT: {documentText}

REQUIREMENTS: Evaluate this criterion. Return format: Score: X | Summary: [concise summary max 30 words]. Keep without line breaks. Score from 0 to {criteria.MaxScore}.";

            return await SummarizeAsync(prompt, maxOutputTokens: 150);
        }

        public async Task<(bool IsRelevant, string CheatDetails)> CheckIntegrityAsync(string documentText, string assignmentTitle, string studentName)
        {
            var prompt = $@"
**SUBMISSION INTEGRITY & RELEVANCE ANALYSIS**

ASSIGNMENT TITLE: {assignmentTitle}

DOCUMENT CONTENT (Snippet):
{documentText}

**YOUR TASKS (EXECUTE IN ORDER):**

1.  **DOMAIN/SUBJECT CHECK (CRITICAL):** - Identify the academic subject of the **ASSIGNMENT TITLE** (e.g., Public Relations, History, Marketing).
    - Identify the academic subject of the **DOCUMENT CONTENT** (e.g., Java Coding, OOP, Mathematics).
    - **RULE:** If the subjects are totally different (e.g., Assignment is 'PR/Business' but Content is 'Coding/Java'), set `isRelevant` to `false`.
    - *Example:* Assignment 'PR Case Study' vs Content 'Java Singleton Pattern' => `isRelevant: false`.

2.  **ANONYMITY CHECK (BLIND REVIEW):** - Scan the document for **ANY** Student Name or Student ID appearing in headers, footers, or signatures.
    - **RULE:** If you find ANY personal name (even the author's) or ID, set `hasIdentityIssue` to `true`.
    - **EXCLUSIONS:** Ignore common words like 'self', 'me', 'author', 'student'. Ignore names of famous people/citations.

**RESPONSE FORMAT:**
Return a valid JSON object ONLY. No markdown.
{{
  ""isRelevant"": true/false,
  ""relevanceReason"": ""[Briefly explain why it matches or mismatches the subject]"",
  ""hasIdentityIssue"": true/false,
  ""foundName"": ""[Name found, or empty]"",
  ""foundId"": ""[ID found, or empty]"",
  ""locationSnippet"": ""[Quote where the Name/ID appears]""
}}";

            try
            {
                var resultJson = await SummarizeAsync(prompt, maxOutputTokens: 400);

                resultJson = resultJson.Replace("```json", "").Replace("```", "").Trim();

                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;

                bool isRelevant = root.TryGetProperty("isRelevant", out var relElement) && relElement.GetBoolean();
                string relevanceReason = root.TryGetProperty("relevanceReason", out var reasonEl) ? reasonEl.GetString() : "";

                bool hasIdentityIssue = root.TryGetProperty("hasIdentityIssue", out var idIssueElement) && idIssueElement.GetBoolean();
                string foundName = root.TryGetProperty("foundName", out var nameEl) ? nameEl.GetString() : "";
                string foundId = root.TryGetProperty("foundId", out var idEl) ? idEl.GetString() : "";
                string locationSnippet = root.TryGetProperty("locationSnippet", out var locEl) ? locEl.GetString() : "";

                if (hasIdentityIssue)
                {
                    string cheatDetails = $"The submission contains the name '{foundName}' and student ID '{foundId}' (found in: \"{locationSnippet}\"), which is not allowed and is considered a violation of integrity.";
                    return (isRelevant, cheatDetails);
                }

                if (!isRelevant)
                {
                    return (false, $"Irrelevant Content: {relevanceReason}");
                }

                return (true, "No anomalies detected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI integrity check");
                return (true, "No anomalies detected");
            }
        }
        public async Task<List<float>> EmbedContentAsync(string text, string model = "embedding-001")
        {
            var requestBody = new
            {
                model = model,
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var url = $"{_baseUrl}{model}:embedContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(
                url,
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini Embedding API error: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            // Parse embedding response (assume vector is List<float>)
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("embedding", out var embedding) &&
                embedding.TryGetProperty("values", out var values))
            {
                return values.EnumerateArray().Select(v => v.GetSingle()).ToList();
            }

            throw new Exception("Failed to parse embedding from Gemini API");
        }
    }
}