using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Assignment;
using Service.RequestAndResponse.Response.Assignment;
using Service.RequestAndResponse.Response.Rubric;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IAssignmentService
    {
        Task<BaseResponse<AssignmentResponse>> CreateAssignmentAsync(CreateAssignmentRequest request);
        Task<BaseResponse<AssignmentResponse>> UpdateAssignmentAsync(UpdateAssignmentRequest request);
        Task<BaseResponse<bool>> DeleteAssignmentAsync(int assignmentId);
        Task<BaseResponse<AssignmentResponse>> GetAssignmentByIdAsync(int assignmentId);
        Task<BaseResponse<AssignmentResponse>> GetAssignmentWithDetailsAsync(int assignmentId);
        Task<BaseResponse<List<AssignmentResponse>>> GetAssignmentsByCourseInstanceAsync(int courseInstanceId);
        Task<BaseResponse<List<AssignmentSummaryResponse>>> GetAssignmentsByInstructorAsync(int instructorId);
        Task<BaseResponse<List<AssignmentSummaryResponse>>> GetAssignmentsByStudentAsync(int studentId);
        Task<BaseResponse<List<AssignmentSummaryResponse>>> GetActiveAssignmentsAsync();
        Task<BaseResponse<List<AssignmentSummaryResponse>>> GetOverdueAssignmentsAsync();
        Task<BaseResponse<bool>> ExtendDeadlineAsync(int assignmentId, DateTime newDeadline);
        Task<BaseResponse<bool>> UpdateRubricAsync(int assignmentId, int rubricId);
        Task<BaseResponse<AssignmentStatsResponse>> GetAssignmentStatisticsAsync(int assignmentId);
        Task<BaseResponse<RubricResponse>> GetAssignmentRubricForReviewAsync(int assignmentId);
        Task<BaseResponse<List<AssignmentBasicResponse>>> GetAssignmentsByCourseInstanceBasicAsync(int courseInstanceId, int studentId);
        Task<BaseResponse<IEnumerable<AssignmentResponse>>> GetActiveAssignmentsByCourseInstanceAsync(int courseInstanceId, int? studentId = null);
        Task<BaseResponse<AssignmentResponse>> CloneAssignmentAsync(int sourceAssignmentId, int targetCourseInstanceId, CloneAssignmentRequest request);
        Task<BaseResponse<AssignmentResponse>> UpdateAssignmentTimelineAsync(int assignmentId, UpdateAssignmentTimelineRequest request);
        Task<BaseResponse<AssignmentStatusSummaryResponse>> GetAssignmentStatusSummaryAsync(int courseInstanceId);
    }
}