namespace Service.RequestAndResponse.Response.Search
{
    public class SearchResultEFResponse
    {
        public List<AssignmentSearchResult> Assignments { get; set; } = new List<AssignmentSearchResult>();
        public List<FeedbackSearchResult> Feedback { get; set; } = new List<FeedbackSearchResult>();
        public List<SummarySearchResult> Summaries { get; set; } = new List<SummarySearchResult>();
    }

    public class AssignmentSearchResult
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string CourseName { get; set; }
    }

    public class FeedbackSearchResult
    {
        public int ReviewId { get; set; }
        public string AssignmentTitle { get; set; }
        public string OverallFeedback { get; set; }
    }

    public class SummarySearchResult
    {
        public int SummaryId { get; set; }
        public string AssignmentTitle { get; set; }
        public string ContentSnippet { get; set; }
    }
}