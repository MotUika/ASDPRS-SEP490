// File: Service/RequestAndResponse/Response/Submission/AutoGradeZeroResponse.cs
using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Submission
{
    public class AutoGradeZeroResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public int NonSubmittedCount { get; set; }
        public int GradedZeroCount { get; set; }
        public List<string> StudentCodes { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}