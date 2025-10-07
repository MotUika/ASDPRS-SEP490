using Service.RequestAndResponse.Response.Criteria;

namespace Service.RequestAndResponse.Response.Rubric
{
    public class RubricResponse
    {
        public int RubricId { get; set; }
        public int? TemplateId { get; set; }
        public string TemplateTitle { get; set; }
        public int? AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public string Title { get; set; }
        public bool IsModified { get; set; }
        public int CriteriaCount { get; set; }
        public string GradingScale { get; set; }
        public List<CriteriaResponse> Criteria { get; set; } = new List<CriteriaResponse>();
    }
}