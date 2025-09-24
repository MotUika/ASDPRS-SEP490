using System;

namespace Service.RequestAndResponse.Response.AISummary
{
    public class AISummaryGenerationResponse
    {
        public int SummaryId { get; set; }
        public string Content { get; set; }
        public string SummaryType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool WasGenerated { get; set; } // True if new summary was generated, false if existing was returned
        public string Status { get; set; }
        public string ModelUsed { get; set; }
        public TimeSpan GenerationTime { get; set; }
    }
}