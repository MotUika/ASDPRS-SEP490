using System;
using System.Collections.Generic;
using Service.RequestAndResponse.Response.CriteriaTemplate;

namespace Service.RequestAndResponse.Response.RubricTemplate
{
    public class RubricTemplateResponse
    {
        public int TemplateId { get; set; }
        public string Title { get; set; }
        public bool IsPublic { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RubricCount { get; set; }
        public int CriteriaTemplateCount { get; set; }
        public int? MajorId { get; set; }         
        public string? MajorName { get; set; }
        public List<CriteriaTemplateResponse> CriteriaTemplates { get; set; }

        // Thêm danh sách assignment đang dùng rubric template này
        public List<AssignmentUsingTemplateResponse> AssignmentsUsingTemplate { get; set; }
    }

    // Class phụ chứa thông tin assignment + lớp + môn + campus
    public class AssignmentUsingTemplateResponse
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string CourseName { get; set; }
        public string ClassName { get; set; }
        public string CampusName { get; set; }
        public DateTime? Deadline { get; set; }
    }
}
