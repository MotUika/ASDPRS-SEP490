namespace Service.RequestAndResponse.Response.Search
{
    public class SearchResultEFResponse
    {
        public List<AssignmentSearchResult> Assignments { get; set; } = new List<AssignmentSearchResult>();
        public List<FeedbackSearchResult> Feedback { get; set; } = new List<FeedbackSearchResult>();
        public List<SummarySearchResult> Summaries { get; set; } = new List<SummarySearchResult>();
        public List<SubmissionSearchResult> Submissions { get; set; } = new List<SubmissionSearchResult>();
        public List<CriteriaSearchResult> Criteria { get; set; } = new List<CriteriaSearchResult>();
    }

    public class AssignmentSearchResult
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string CourseName { get; set; }
        public int CourseId { get; set; }
        public string DescriptionSnippet { get; set; }
        public string Type { get; set; }
    }

    public class FeedbackSearchResult
    {
        public int ReviewId { get; set; }
        public string AssignmentTitle { get; set; }
        public int CourseId { get; set; }
        public string OverallFeedback { get; set; }
        public string ReviewerType { get; set; }
        public string Type { get; set; }
    }

    public class SummarySearchResult
    {
        public int SummaryId { get; set; }
        public string AssignmentTitle { get; set; }
        public int CourseId { get; set; }
        public string ContentSnippet { get; set; }
        public string SummaryType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Type { get; set; }
    }

    public class SubmissionSearchResult
    {
        public int SubmissionId { get; set; }
        public string AssignmentTitle { get; set; }
        public int CourseId { get; set; }
        public string FileName { get; set; }
        public string Keywords { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string StudentName { get; set; }
        public string Type { get; set; }
    }

    public class CriteriaSearchResult
    {
        public int CriteriaId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string RubricTitle { get; set; }
        public string AssignmentTitle { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public decimal MaxScore { get; set; }
        public int Weight { get; set; }
        public string Type { get; set; }
    }
}