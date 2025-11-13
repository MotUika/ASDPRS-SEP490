namespace Service.RequestAndResponse.Response.Search
{
    public class SearchResultEFResponse
    {
        public List<AssignmentSearchResult> Assignments { get; set; } = new List<AssignmentSearchResult>();
        public List<FeedbackSearchResult> Feedback { get; set; } = new List<FeedbackSearchResult>();
        public List<SummarySearchResult> Summaries { get; set; } = new List<SummarySearchResult>();
        public List<SubmissionSearchResult> Submissions { get; set; } = new List<SubmissionSearchResult>();
    }

    public class AssignmentSearchResult
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string CourseName { get; set; }
        public string DescriptionSnippet { get; set; }
    }

    public class FeedbackSearchResult
    {
        public int ReviewId { get; set; }
        public string AssignmentTitle { get; set; }
        public string OverallFeedback { get; set; }
        public string ReviewerType { get; set; }
    }

    public class SummarySearchResult
    {
        public int SummaryId { get; set; }
        public string AssignmentTitle { get; set; }
        public string ContentSnippet { get; set; }
        public string SummaryType { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SubmissionSearchResult
    {
        public int SubmissionId { get; set; }
        public string AssignmentTitle { get; set; }
        public string FileName { get; set; }
        public string Keywords { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string StudentName { get; set; }
    }
}