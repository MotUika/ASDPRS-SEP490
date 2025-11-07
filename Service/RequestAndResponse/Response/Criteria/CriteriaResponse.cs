namespace Service.RequestAndResponse.Response.Criteria
{
    public class CriteriaResponse
    {
        public int CriteriaId { get; set; }
        public int RubricId { get; set; }
        public string RubricTitle { get; set; }
        public int? CriteriaTemplateId { get; set; }
        public string CriteriaTemplateTitle { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Weight { get; set; }
        public decimal MaxScore { get; set; }
        public string ScoringType { get; set; }
        public string ScoreLabel { get; set; }
        public bool IsModified { get; set; }
        public int CriteriaFeedbackCount { get; set; }
        public string CourseName { get; set; }
        public string ClassName { get; set; }
    }
}