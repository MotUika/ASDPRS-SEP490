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
    }

}
