using BussinessObject.Models;
using Service.RequestAndResponse.Response.AISummary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IGenAIService
    {
        Task<string> SummarizeAsync(string text, string model = null, int maxOutputTokens = 800);
        Task<string> AnalyzeDocumentAsync(string text, string analysisType);
        Task<string> GenerateReviewAsync(string documentText, string context);
        Task<string> GenerateEnhancedReviewAsync(string documentText, string context, List<Criteria> criteria);
        Task<string> GenerateCriteriaReviewAsync(string documentText, Criteria criteria, string context);
        Task<string> GenerateOverallSummaryAsync(string documentText, string context);
        Task<string> GenerateCriteriaSummaryAsync(string documentText, Criteria criteria, string context);
        Task<List<AICriteriaFeedbackItem>> GenerateBulkCriteriaFeedbackAsync(string documentText, List<Criteria> criteria, string context);
        Task<string> CheckSubmissionRelevanceAsync(string documentText, string context, string assignmentTitle);
        Task<List<float>> EmbedContentAsync(string text, string model = "embedding-001");
        Task<(bool IsRelevant, string CheatDetails)> CheckIntegrityAsync(string documentText, string assignmentTitle, string studentName);
    }

}
