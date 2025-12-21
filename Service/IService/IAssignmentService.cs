using BussinessObject.Models;
using Microsoft.AspNetCore.Mvc;
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
        Task<BaseResponse<List<AssignmentResponse>>> GetAssignmentsByRubricTemplateAsync(int rubricTemplateId);
        Task<BaseResponse<bool>> ExtendDeadlineAsync(int assignmentId, DateTime newDeadline);
        Task<BaseResponse<bool>> UpdateRubricAsync(int assignmentId, int rubricId);
        Task<BaseResponse<AssignmentStatsResponse>> GetAssignmentStatisticsAsync(int assignmentId);
        Task<BaseResponse<RubricResponse>> GetAssignmentRubricForReviewAsync(int assignmentId);
        Task<BaseResponse<List<AssignmentBasicResponse>>> GetAssignmentsByCourseInstanceBasicAsync(int courseInstanceId);
        Task<BaseResponse<IEnumerable<AssignmentResponse>>> GetActiveAssignmentsByCourseInstanceAsync(int courseInstanceId, int? studentId = null);
        Task<BaseResponse<AssignmentResponse>> CloneAssignmentAsync(int sourceAssignmentId, int targetCourseInstanceId, CloneAssignmentRequest request);
        Task<BaseResponse<AssignmentResponse>> UpdateAssignmentTimelineAsync(int assignmentId, UpdateAssignmentTimelineRequest request);
        Task<BaseResponse<AssignmentStatusSummaryResponse>> GetAssignmentStatusSummaryAsync(int courseInstanceId);
        Task<BaseResponse<AssignmentTrackingResponse>> GetAssignmentTrackingAsync(int assignmentId);
        Task<BaseResponse<bool>> PublishGradesAsync(int assignmentId);
        Task<BaseResponse<AssignmentResponse>> PublishAssignmentAsync(int assignmentId);
        Task<List<Submission>> GetEligibleSubmissionsForCrossClassReviewAsync(int reviewerStudentId, int currentAssignmentId);
        Task<BaseResponse<List<PublishedGradeAssignmentResponse>>> GetPublishedGradeAssignmentsForStudentAsync(int studentId);
        Task<BaseResponse<List<AssignmentSummaryResponse>>> GetStudentAssignmentStatusesBySemesterAsync(int studentId, int semesterId);
        Task<BaseResponse<List<StudentSemesterScoreResponse>>> GetStudentSemesterScoresAsync(int studentId, int semesterId);
    }
}