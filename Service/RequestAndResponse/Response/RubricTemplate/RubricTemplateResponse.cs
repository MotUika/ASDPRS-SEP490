using System;
using System.Collections.Generic;

using Service.RequestAndResponse.Response.CriteriaTemplate;
namespace Service.RequestAndResponse.Response.RubricTemplate
{
    public class RubricTemplateResponse
    {
        public int TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RubricCount { get; set; }
        public int CriteriaTemplateCount { get; set; }
        public List<CriteriaTemplateResponse> CriteriaTemplates { get; set; }
    }
}