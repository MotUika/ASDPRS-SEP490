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
        public string Description { get; set; }
        public bool IsModified { get; set; }
        public int CriteriaCount { get; set; }
    }
}