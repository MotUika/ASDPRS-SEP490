using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IGenAIService
    {
        /// <summary>
        /// Summarize given text (single chunk). Return summary text.
        /// </summary>
        Task<string> SummarizeAsync(string text, string model = null, int maxOutputTokens = 800);
        Task<string> AnalyzeDocumentAsync(string text, string analysisType);
    }

}
