// File: Service/RequestAndResponse/Request/Submission/AutoGradeZeroRequest.cs
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class AutoGradeZeroRequest
    {
        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public bool ConfirmZeroGrade { get; set; } = false;
    }
}