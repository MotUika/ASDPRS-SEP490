using Microsoft.AspNetCore.Http;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Submission;
using Service.RequestAndResponse.Response.Submission;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ISubmissionService
    {
        Task<BaseResponse<SubmissionResponse>> CreateSubmissionAsync(CreateSubmissionRequest request);
        Task<BaseResponse<SubmissionResponse>> SubmitAssignmentAsync(SubmitAssignmentRequest request);
        Task<BaseResponse<SubmissionResponse>> GetSubmissionByIdAsync(GetSubmissionByIdRequest request);
        Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByFilterAsync(GetSubmissionsByFilterRequest request);
        Task<BaseResponse<SubmissionResponse>> UpdateSubmissionAsync(UpdateSubmissionRequest request);
        Task<BaseResponse<SubmissionResponse>> UpdateSubmissionStatusAsync(UpdateSubmissionStatusRequest request);
        Task<BaseResponse<bool>> DeleteSubmissionAsync(int submissionId);
        Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByAssignmentIdAsync(int assignmentId, int pageNumber = 1, int pageSize = 20);
        Task<BaseResponse<SubmissionListResponse>> GetSubmissionsAllStudentByAssignmentIdAsync(int assignmentId);
        Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 20);
        Task<BaseResponse<SubmissionStatisticsResponse>> GetSubmissionStatisticsAsync(int assignmentId);
        Task<BaseResponse<bool>> CheckSubmissionExistsAsync(int assignmentId, int userId);
        Task<BaseResponse<SubmissionResponse>> GetSubmissionWithDetailsAsync(int submissionId);
        Task<BaseResponse<bool>> CanStudentModifySubmissionAsync(int submissionId, int studentId);
        Task<BaseResponse<SubmissionResponse>> GetSubmissionByAssignmentAndUserAsync(int assignmentId, int userId);
        Task<BaseResponse<List<SubmissionResponse>>> GetSubmissionsByCourseInstanceAndUserAsync(int courseInstanceId, int userId);
        Task<BaseResponse<List<SubmissionResponse>>> GetSubmissionsByUserAndSemesterAsync(int userId, int semesterId);
        Task<BaseResponse<GradeSubmissionResponse>> GradeSubmissionAsync(GradeSubmissionRequest request);
        Task<BaseResponse<PublishGradesResponse>> PublishGradesAsync(PublishGradesRequest request);
        Task<BaseResponse<AutoGradeZeroResponse>> AutoGradeZeroForNonSubmittersAsync(AutoGradeZeroRequest request);
        Task<BaseResponse<IEnumerable<SubmissionSummaryResponse>>> GetSubmissionSummaryAsync(
            int? courseId, int? classId, int? assignmentId);
        Task<BaseResponse<SubmissionResponse>> CreateSubmissionWithCheckAsync(CreateSubmissionRequest request);
        Task<BaseResponse<SubmissionResponse>> SubmitAssignmentWithCheckAsync(SubmitAssignmentRequest request);
        Task<BaseResponse<SubmissionResponse>> UpdateSubmissionWithCheckAsync(UpdateSubmissionRequest request);
        Task<BaseResponse<PlagiarismCheckResponse>> CheckPlagiarismActiveAsync(int assignmentId, IFormFile file, int? excludeSubmissionId = null);
        Task<BaseResponse<decimal?>> GetMyScoreAsync(int assignmentId, int studentId);
        Task<BaseResponse<MyScoreDetailsResponse>> GetMyScoreDetailsAsync(int assignmentId, int studentId);
        Task<IEnumerable<InstructorSubmissionInfoResponse>> GetInstructorSubmissionInfoAsync(
    int userId, int? classId, int? assignmentId);
        Task<BaseResponse<List<SubmissionDetailExportResponse>>>
     GetAllSubmissionDetailsForExportAsync(int assignmentId);
        //Task<BaseResponse<GradeSubmissionResponse>> ImportGradeAsync(ImportGradeRequest request);
        Task<BaseResponse<List<GradeSubmissionResponse>>> ImportGradesFromExcelAsync(IFormFile file);
    }
}