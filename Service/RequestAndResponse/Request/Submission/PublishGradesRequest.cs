using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class PublishGradesRequest
    {
        [Required(ErrorMessage = "AssignmentId is required")]
        public int AssignmentId { get; set; }

        // Nếu muốn force publish dù chưa đủ 50% lớp hoặc chưa qua deadline
        public bool ForcePublish { get; set; } = false;
    }
}
