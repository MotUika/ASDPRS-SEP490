namespace Service.RequestAndResponse.Response.CriteriaTemplate
{
    public class CriteriaTemplateResponse
    {
        public int CriteriaTemplateId { get; set; }
        public int TemplateId { get; set; }
        public string TemplateTitle { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Weight { get; set; }
        public decimal MaxScore { get; set; }
        public string ScoringType { get; set; }
        public string ScoreLabel { get; set; }
        public int CriteriaCount { get; set; }
    }
}