using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using MathNet.Numerics.Distributions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Repository.Repository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Assignment;
using Service.RequestAndResponse.Response.Assignment;
using Service.RequestAndResponse.Response.Criteria;
using Service.RequestAndResponse.Response.Rubric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ICourseInstanceRepository _courseInstanceRepository;
        private readonly IRubricRepository _rubricRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ICourseInstructorRepository _courseInstructorRepository;
        private readonly ICourseStudentRepository _courseStudentRepository;
        private readonly ICriteriaRepository _criteriaRepository;
        private readonly IReviewAssignmentRepository _reviewAssignmentRepository;
        private readonly ASDPRSContext _context;
        private readonly INotificationService _notificationService;
        private readonly ICriteriaFeedbackRepository _criteriaFeedbackRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public AssignmentService(
            IAssignmentRepository assignmentRepository,
            ICourseInstanceRepository courseInstanceRepository,
            IRubricRepository rubricRepository,
            ISubmissionRepository submissionRepository,
            IReviewRepository reviewRepository,
            ICourseInstructorRepository courseInstructorRepository,
            ICourseStudentRepository courseStudentRepository,
            ICriteriaRepository criteriaRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            ASDPRSContext context,
            INotificationService notificationService,
            ICriteriaFeedbackRepository criteriaFeedbackRepository, IHttpContextAccessor httpContextAccessor)
        {
            _assignmentRepository = assignmentRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _rubricRepository = rubricRepository;
            _submissionRepository = submissionRepository;
            _reviewRepository = reviewRepository;
            _courseInstructorRepository = courseInstructorRepository;
            _courseStudentRepository = courseStudentRepository;
            _criteriaRepository = criteriaRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _context = context;
            _notificationService = notificationService;
            _criteriaFeedbackRepository = criteriaFeedbackRepository;
            _httpContextAccessor = httpContextAccessor;
        }
        private int GetCurrentStudentId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int studentId))
            {
                throw new UnauthorizedAccessException("Invalid user token - userId not found");
            }
            return studentId;
        }

        public async Task<BaseResponse<AssignmentResponse>> CreateAssignmentAsync(CreateAssignmentRequest request)
        {
            try
            {
                // Validate course instance exists
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<AssignmentResponse>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Validate rubric exists if provided
                if (request.RubricId.HasValue)
                {
                    var rubric = await _rubricRepository.GetByIdAsync(request.RubricId.Value);
                    if (rubric == null)
                    {
                        return new BaseResponse<AssignmentResponse>(
                            "Rubric not found",
                            StatusCodeEnum.NotFound_404,
                            null);
                    }
                }

                // Validate weights sum to 100
                if (request.InstructorWeight + request.PeerWeight != 100)
                {
                    return new BaseResponse<AssignmentResponse>(
                        "Instructor weight and peer weight must sum to 100%",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Validate grading scale
                if (request.GradingScale != "Scale10" && request.GradingScale != "PassFail")
                {
                    return new BaseResponse<AssignmentResponse>(
                        "Grading scale must be either 'Scale10' or 'PassFail'",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Validate weights sum to 100
                if (request.InstructorWeight + request.PeerWeight != 100)
                {
                    return new BaseResponse<AssignmentResponse>(
                        "Instructor weight and peer weight must sum to 100%",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var assignment = new Assignment
                {
                    CourseInstanceId = request.CourseInstanceId,
                    RubricId = request.RubricId,
                    Title = request.Title,
                    Description = request.Description,
                    Guidelines = request.Guidelines,
                    CreatedAt = DateTime.UtcNow,
                    StartDate = request.StartDate,
                    Deadline = request.Deadline,
                    FinalDeadline = request.FinalDeadline,
                    ReviewDeadline = request.ReviewDeadline,
                    NumPeerReviewsRequired = request.NumPeerReviewsRequired,
                    AllowCrossClass = request.AllowCrossClass,
                    IsBlindReview = request.IsBlindReview,
                    InstructorWeight = request.InstructorWeight,
                    PeerWeight = request.PeerWeight,
                    GradingScale = request.GradingScale,
                    Weight = request.Weight,
                    IncludeAIScore = request.IncludeAIScore,
                    Status = AssignmentStatusEnum.Draft.ToString()
                };

                await _assignmentRepository.AddAsync(assignment);
                await SaveAssignmentConfigs(assignment.AssignmentId, "LateSubmissionPenalty", "0");  // Default 0%
                await SaveAssignmentConfigs(assignment.AssignmentId, "MissingReviewPenalty", "0");
                await _notificationService.SendNewAssignmentNotificationAsync(assignment.AssignmentId, assignment.CourseInstanceId);
                var response = await MapToResponse(assignment);
                return new BaseResponse<AssignmentResponse>(
                    "Assignment created successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentResponse>(
                    $"Error creating assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        private async Task SaveAssignmentConfigs(int assignmentId, string key, string value)
        {
            var configKey = $"{key}_{assignmentId}";
            var config = await _context.SystemConfigs.FirstOrDefaultAsync(sc => sc.ConfigKey == configKey);

            if (config == null)
            {
                config = new SystemConfig
                {
                    ConfigKey = configKey,
                    ConfigValue = value,
                    Description = "Assignment config",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = 1
                };
                _context.SystemConfigs.Add(config);
            }
            else
            {
                config.ConfigValue = value;
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<BaseResponse<AssignmentResponse>> UpdateAssignmentAsync(UpdateAssignmentRequest request)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AssignmentResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Update fields if provided
                if (request.RubricId.HasValue)
                {
                    if (request.RubricId.Value == 0)
                    {
                        assignment.RubricId = null;
                    }
                    else
                    {
                        var rubric = await _rubricRepository.GetByIdAsync(request.RubricId.Value);
                        if (rubric == null)
                        {
                            return new BaseResponse<AssignmentResponse>(
                                "Rubric not found",
                                StatusCodeEnum.NotFound_404,
                                null);
                        }
                        assignment.RubricId = request.RubricId.Value;
                    }
                }

                if (!string.IsNullOrEmpty(request.Title))
                    assignment.Title = request.Title;

                if (!string.IsNullOrEmpty(request.Description))
                    assignment.Description = request.Description;

                if (!string.IsNullOrEmpty(request.Guidelines))
                    assignment.Guidelines = request.Guidelines;

                if (request.StartDate.HasValue)
                    assignment.StartDate = request.StartDate.Value;

                if (request.Deadline.HasValue)
                    assignment.Deadline = request.Deadline.Value;

                if (request.ReviewDeadline.HasValue)
                    assignment.ReviewDeadline = request.ReviewDeadline.Value;

                if (request.FinalDeadline.HasValue)
                    assignment.FinalDeadline = request.FinalDeadline.Value;

                if (request.NumPeerReviewsRequired.HasValue)
                    assignment.NumPeerReviewsRequired = request.NumPeerReviewsRequired.Value;

                if (request.AllowCrossClass.HasValue)
                    assignment.AllowCrossClass = request.AllowCrossClass.Value;

                if (request.IsBlindReview.HasValue)
                    assignment.IsBlindReview = request.IsBlindReview.Value;

                if (request.InstructorWeight.HasValue && request.PeerWeight.HasValue)
                {
                    if (request.InstructorWeight.Value + request.PeerWeight.Value != 100)
                    {
                        return new BaseResponse<AssignmentResponse>(
                            "Instructor weight and peer weight must sum to 100%",
                            StatusCodeEnum.BadRequest_400,
                            null);
                    }
                    assignment.InstructorWeight = request.InstructorWeight.Value;
                    assignment.PeerWeight = request.PeerWeight.Value;
                }
                if (request.GradingScale != null)
                    assignment.GradingScale = request.GradingScale;

                if (request.Weight.HasValue)
                    assignment.Weight = request.Weight.Value;

                if (request.LateSubmissionPenalty.HasValue)
                    await SaveAssignmentConfigs(assignment.AssignmentId, "LateSubmissionPenalty", request.LateSubmissionPenalty.Value.ToString());

                if (request.MissingReviewPenalty.HasValue)
                    await SaveAssignmentConfigs(assignment.AssignmentId, "MissingReviewPenalty", request.MissingReviewPenalty.Value.ToString());

                if (request.IncludeAIScore.HasValue)
                    assignment.IncludeAIScore = request.IncludeAIScore.Value;

                await _assignmentRepository.UpdateAsync(assignment);

                var response = await MapToResponse(assignment);
                return new BaseResponse<AssignmentResponse>(
                    "Assignment updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentResponse>(
                    $"Error updating assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteAssignmentAsync(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<bool>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // Check if there are submissions for this assignment
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                if (submissions.Any())
                {
                    return new BaseResponse<bool>(
                        "Cannot delete assignment that has submissions",
                        StatusCodeEnum.Conflict_409,
                        false);
                }

                await _assignmentRepository.DeleteAsync(assignment);
                return new BaseResponse<bool>(
                    "Assignment deleted successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error deleting assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<AssignmentResponse>> GetAssignmentByIdAsync(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AssignmentResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToResponse(assignment);
                return new BaseResponse<AssignmentResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentResponse>(
                    $"Error retrieving assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<AssignmentResponse>> GetAssignmentWithDetailsAsync(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetAssignmentWithRubricAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AssignmentResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToResponse(assignment);
                return new BaseResponse<AssignmentResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentResponse>(
                    $"Error retrieving assignment details: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<AssignmentResponse>>> GetAssignmentsByCourseInstanceAsync(int courseInstanceId)
        {
            try
            {
                var assignments = await _assignmentRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                IQueryable<Assignment> query = _context.Assignments
                    .Where(a => a.CourseInstanceId == courseInstanceId);

                var responses = new List<AssignmentResponse>();

                foreach (var assignment in assignments)
                {
                    responses.Add(await MapToResponse(assignment));
                }

                return new BaseResponse<List<AssignmentResponse>>("Success", StatusCodeEnum.OK_200, responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AssignmentResponse>>(
                    $"Error retrieving assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<AssignmentSummaryResponse>>> GetAssignmentsByInstructorAsync(int instructorId)
        {
            try
            {
                var assignments = await _assignmentRepository.GetAssignmentsByInstructorAsync(instructorId);
                var responses = new List<AssignmentSummaryResponse>();

                foreach (var assignment in assignments)
                {
                    responses.Add(await MapToSummaryResponse(assignment));
                }

                return new BaseResponse<List<AssignmentSummaryResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AssignmentSummaryResponse>>(
                    $"Error retrieving instructor assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<AssignmentSummaryResponse>>> GetAssignmentsByStudentAsync(int studentId)
        {
            try
            {
                var assignments = await _assignmentRepository.GetAssignmentsByStudentAsync(studentId);
                var responses = new List<AssignmentSummaryResponse>();

                foreach (var assignment in assignments)
                {
                    responses.Add(await MapToSummaryResponse(assignment));
                }

                return new BaseResponse<List<AssignmentSummaryResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AssignmentSummaryResponse>>(
                    $"Error retrieving student assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<AssignmentSummaryResponse>>> GetActiveAssignmentsAsync()
        {
            try
            {
                var assignments = await _assignmentRepository.GetActiveAssignmentsAsync();
                var responses = new List<AssignmentSummaryResponse>();

                foreach (var assignment in assignments)
                {
                    responses.Add(await MapToSummaryResponse(assignment));
                }

                return new BaseResponse<List<AssignmentSummaryResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AssignmentSummaryResponse>>(
                    $"Error retrieving active assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<AssignmentSummaryResponse>>> GetOverdueAssignmentsAsync()
        {
            try
            {
                var assignments = await _assignmentRepository.GetOverdueAssignmentsAsync();
                var responses = new List<AssignmentSummaryResponse>();

                foreach (var assignment in assignments)
                {
                    responses.Add(await MapToSummaryResponse(assignment));
                }

                return new BaseResponse<List<AssignmentSummaryResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AssignmentSummaryResponse>>(
                    $"Error retrieving overdue assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> ExtendDeadlineAsync(int assignmentId, DateTime newDeadline)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<bool>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                if (newDeadline <= assignment.Deadline)
                {
                    return new BaseResponse<bool>(
                        "New deadline must be after current deadline",
                        StatusCodeEnum.BadRequest_400,
                        false);
                }

                assignment.Deadline = newDeadline;
                await _assignmentRepository.UpdateAsync(assignment);

                return new BaseResponse<bool>(
                    "Assignment deadline extended successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error extending assignment deadline: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<bool>> UpdateRubricAsync(int assignmentId, int rubricId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<bool>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                var rubric = await _rubricRepository.GetByIdAsync(rubricId);
                if (rubric == null)
                {
                    return new BaseResponse<bool>(
                        "Rubric not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                assignment.RubricId = rubricId;
                await _assignmentRepository.UpdateAsync(assignment);

                return new BaseResponse<bool>(
                    "Assignment rubric updated successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error updating assignment rubric: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<AssignmentStatsResponse>> GetAssignmentStatisticsAsync(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AssignmentStatsResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                var reviews = new List<Review>();

                foreach (var submission in submissions)
                {
                    var submissionReviews = await _reviewRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                    reviews.AddRange(submissionReviews);
                }

                var stats = new AssignmentStatsResponse
                {
                    AssignmentId = assignmentId,
                    AssignmentTitle = assignment.Title,
                    TotalSubmissions = submissions.Count(),
                    TotalReviews = reviews.Count,
                    AverageScore = reviews.Where(r => r.OverallScore.HasValue).Average(r => r.OverallScore.Value),
                    SubmissionRate = await CalculateSubmissionRateAsync(assignmentId, assignment.CourseInstanceId),
                    ReviewCompletionRate = await CalculateReviewCompletionRateAsync(assignmentId),
                    GradeDistribution = CalculateGradeDistribution(reviews)
                };

                return new BaseResponse<AssignmentStatsResponse>(
                    "Statistics retrieved successfully",
                    StatusCodeEnum.OK_200,
                    stats);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentStatsResponse>(
                    $"Error retrieving assignment statistics: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<RubricResponse>> GetAssignmentRubricForReviewAsync(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<RubricResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                if (!assignment.RubricId.HasValue)
                {
                    return new BaseResponse<RubricResponse>(
                        "Assignment does not have a rubric",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var rubric = await _rubricRepository.GetByIdAsync(assignment.RubricId.Value);
                if (rubric == null)
                {
                    return new BaseResponse<RubricResponse>(
                        "Rubric not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Lấy criteria của rubric
                var criteria = await _criteriaRepository.GetByRubricIdAsync(rubric.RubricId);
                var criteriaResponses = criteria.Select(c => new CriteriaResponse
                {
                    CriteriaId = c.CriteriaId,
                    Title = c.Title,
                    Description = c.Description,
                    MaxScore = c.MaxScore,
                    Weight = c.Weight
                }).ToList();

                var response = new RubricResponse
                {
                    RubricId = rubric.RubricId,
                    Title = rubric.Title,
                    Criteria = criteriaResponses
                };

                return new BaseResponse<RubricResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricResponse>(
                    $"Error retrieving assignment rubric: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<List<AssignmentBasicResponse>>> GetAssignmentsByCourseInstanceBasicAsync(int courseInstanceId)
        {
            try
            {
                // Lấy studentId từ token (sẽ implement sau)
                var studentId = GetCurrentStudentId();

                var assignments = await _assignmentRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                var responses = new List<AssignmentBasicResponse>();

                foreach (var assignment in assignments)
                {
                    var tracking = await GetAssignmentReviewTrackingAsync(assignment.AssignmentId, studentId);

                    responses.Add(new AssignmentBasicResponse
                    {
                        AssignmentId = assignment.AssignmentId,
                        Title = assignment.Title,
                        Description = assignment.Description,
                        Guidelines = assignment.Guidelines,
                        CreatedAt = assignment.CreatedAt,
                        StartDate = assignment.StartDate ?? DateTime.MinValue,
                        Deadline = assignment.Deadline,
                        ReviewDeadline = assignment.ReviewDeadline ?? DateTime.MinValue,
                        FinalDeadline = assignment.FinalDeadline ?? DateTime.MinValue,
                        Status = assignment.Status,
                        NumPeerReviewsRequired = assignment.NumPeerReviewsRequired,
                        PendingReviewsCount = tracking.PendingCount,
                        CompletedReviewsCount = tracking.CompletedCount
                    });
                }

                return new BaseResponse<List<AssignmentBasicResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AssignmentBasicResponse>>(
                    $"Error retrieving assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private async Task<(int PendingCount, int CompletedCount)> GetAssignmentReviewTrackingAsync(int assignmentId, int studentId)
        {
            var pendingCount = 0;
            var completedCount = 0;

            // Tối ưu: Chỉ lấy review assignments của student này trong assignment này
            var studentReviewAssignments = await _reviewAssignmentRepository.GetByReviewerIdAsync(studentId);

            // Lọc theo assignment
            foreach (var reviewAssignment in studentReviewAssignments)
            {
                var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
                if (submission?.AssignmentId == assignmentId)
                {
                    if (reviewAssignment.Status == "Completed")
                        completedCount++;
                    else
                        pendingCount++;
                }
            }

            return (pendingCount, completedCount);
        }

        public async Task<BaseResponse<AssignmentTrackingResponse>> GetAssignmentTrackingAsync(int assignmentId)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);

                if (assignment == null)
                {
                    return new BaseResponse<AssignmentTrackingResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }
                assignment.Status = CalculateAssignmentStatus(assignment);
                var tracking = await GetAssignmentReviewTrackingAsync(assignmentId, studentId);

                var hasMetMinimum = tracking.CompletedCount >= assignment.NumPeerReviewsRequired;
                var remaining = Math.Max(0, assignment.NumPeerReviewsRequired - tracking.CompletedCount);

                string reviewStatus = tracking.CompletedCount >= assignment.NumPeerReviewsRequired ? "Completed" :
                                    tracking.CompletedCount > 0 ? "In Progress" : "Not Started";

                decimal completionPercentage = assignment.NumPeerReviewsRequired > 0 ?
                    (decimal)tracking.CompletedCount / assignment.NumPeerReviewsRequired * 100 : 0;

                var response = new AssignmentTrackingResponse
                {
                    AssignmentId = assignment.AssignmentId,
                    Title = assignment.Title,
                    Description = assignment.Description,
                    Guidelines = assignment.Guidelines,
                    CreatedAt = assignment.CreatedAt,
                    StartDate = assignment.StartDate ?? DateTime.MinValue,
                    Deadline = assignment.Deadline,
                    ReviewDeadline = assignment.ReviewDeadline ?? DateTime.MinValue,
                    FinalDeadline = assignment.FinalDeadline ?? DateTime.MinValue,
                    Status = assignment.Status,
                    NumPeerReviewsRequired = assignment.NumPeerReviewsRequired,
                    PendingReviewsCount = tracking.PendingCount,
                    CompletedReviewsCount = tracking.CompletedCount,
                    HasMetMinimumReviews = hasMetMinimum,
                    RemainingReviewsRequired = remaining,
                    ReviewStatus = reviewStatus,
                    ReviewCompletionPercentage = Math.Round(completionPercentage, 1)
                };

                return new BaseResponse<AssignmentTrackingResponse>(
                    "Assignment tracking retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentTrackingResponse>(
                    $"Error retrieving assignment tracking: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<bool>> PublishGradesAsync(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<bool>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // Check if past review deadline
                if (!assignment.ReviewDeadline.HasValue || DateTime.UtcNow <= assignment.ReviewDeadline.Value)
                {
                    return new BaseResponse<bool>(
                        "Cannot publish grades before review deadline",
                        StatusCodeEnum.BadRequest_400,
                        false);
                }

                assignment.Status = "GradesPublished";
                await _assignmentRepository.UpdateAsync(assignment);


                return new BaseResponse<bool>(
                    "Grades published successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error publishing grades: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }
        private async Task<AssignmentResponse> MapToResponse(Assignment assignment)
        {
            var courseInstance = await _courseInstanceRepository.GetByIdAsync(assignment.CourseInstanceId);
            RubricResponse rubricResponse = null;

            if (assignment.RubricId.HasValue)
            {
                var rubric = await _rubricRepository.GetByIdAsync(assignment.RubricId.Value);
                if (rubric != null)
                {
                    // Map rubric to RubricResponse (you'll need to create this mapping)
                    rubricResponse = new RubricResponse
                    {
                        RubricId = rubric.RubricId,
                        Title = rubric.Title,
                        // Add other rubric properties as needed
                    };
                }
            }

            var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignment.AssignmentId);
            var reviews = new List<Review>();

            foreach (var submission in submissions)
            {
                var submissionReviews = await _reviewRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                reviews.AddRange(submissionReviews);
            }

            return new AssignmentResponse
            {
                AssignmentId = assignment.AssignmentId,
                CourseInstanceId = assignment.CourseInstanceId,
                RubricId = assignment.RubricId,
                Title = assignment.Title,
                Description = assignment.Description,
                Guidelines = assignment.Guidelines,
                CreatedAt = assignment.CreatedAt,
                StartDate = assignment.StartDate,
                Deadline = assignment.Deadline,
                ReviewDeadline = assignment.ReviewDeadline,
                FinalDeadline = assignment.FinalDeadline,
                NumPeerReviewsRequired = assignment.NumPeerReviewsRequired,
                AllowCrossClass = assignment.AllowCrossClass,
                IsBlindReview = assignment.IsBlindReview,
                InstructorWeight = assignment.InstructorWeight,
                PeerWeight = assignment.PeerWeight,
                Weight = assignment.Weight,
                IncludeAIScore = assignment.IncludeAIScore,
                CourseName = courseInstance?.Course?.CourseName ?? string.Empty,
                CourseCode = courseInstance?.Course?.CourseCode ?? string.Empty,
                SectionCode = courseInstance?.SectionCode ?? string.Empty,
                CampusName = courseInstance?.Campus?.CampusName ?? string.Empty,
                Rubric = rubricResponse,
                SubmissionCount = submissions.Count(),
                ReviewCount = reviews.Count,
                Status = assignment.Status,
                UiStatus = GetUiStatus(assignment)
            };
        }

        private async Task<AssignmentSummaryResponse> MapToSummaryResponse(Assignment assignment)
        {
            var courseInstance = await _courseInstanceRepository.GetByIdAsync(assignment.CourseInstanceId);
            var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignment.AssignmentId);
            var students = await _courseStudentRepository.GetByCourseInstanceIdAsync(assignment.CourseInstanceId);

            return new AssignmentSummaryResponse
            {
                AssignmentId = assignment.AssignmentId,
                Title = assignment.Title,
                Description = assignment.Description,
                Deadline = assignment.Deadline,
                ReviewDeadline = assignment.ReviewDeadline,
                FinalDeadline = assignment.FinalDeadline,
                CourseName = courseInstance?.Course?.CourseName ?? string.Empty,
                SectionCode = courseInstance?.SectionCode ?? string.Empty,
                SubmissionCount = submissions.Count(),
                StudentCount = students.Count(),
                IsOverdue = DateTime.UtcNow > assignment.Deadline,
                DaysUntilDeadline = (int)(assignment.Deadline - DateTime.UtcNow).TotalDays,
                Status = assignment.Status,
                UiStatus = GetUiStatus(assignment)
            };
        }

        private string GetUiStatus(Assignment assignment)
        {
            var now = DateTime.UtcNow;

            // Chỉ hiển thị phụ khi đang Active
            if (assignment.Status == AssignmentStatusEnum.Active.ToString())
            {
                if (now > assignment.Deadline)
                    return "Overdue";

                var daysLeft = (assignment.Deadline - now).TotalDays;
                if (daysLeft <= 3)
                    return "Due Soon";
            }

            // Còn lại hiển thị status thật
            return assignment.Status;
        }


        private async Task<decimal> CalculateSubmissionRateAsync(int assignmentId, int courseInstanceId)
        {
            var courseStudents = await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId);
            var totalStudents = courseStudents.Count();
            var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);

            if (totalStudents == 0) return 0;
            return (decimal)submissions.Count() / totalStudents * 100;
        }

        private async Task<decimal> CalculateReviewCompletionRateAsync(int assignmentId)
        {
            var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
            var totalRequiredReviews = submissions.Sum(s => 2); // Assuming 2 reviews per submission
            var completedReviews = 0;

            foreach (var submission in submissions)
            {
                var reviews = await _reviewRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                completedReviews += reviews.Count(r => r.ReviewedAt.HasValue);
            }

            if (totalRequiredReviews == 0) return 0;
            return (decimal)completedReviews / totalRequiredReviews * 100;
        }

        private Dictionary<string, int> CalculateGradeDistribution(IEnumerable<Review> reviews)
        {
            var distribution = new Dictionary<string, int>
            {
                { "A (90-100)", 0 },
                { "B (80-89)", 0 },
                { "C (70-79)", 0 },
                { "D (60-69)", 0 },
                { "F (0-59)", 0 }
            };

            foreach (var review in reviews.Where(r => r.OverallScore.HasValue))
            {
                var score = review.OverallScore.Value;
                if (score >= 90) distribution["A (90-100)"]++;
                else if (score >= 80) distribution["B (80-89)"]++;
                else if (score >= 70) distribution["C (70-79)"]++;
                else if (score >= 60) distribution["D (60-69)"]++;
                else distribution["F (0-59)"]++;
            }

            return distribution;
        }
        private string CalculateAssignmentStatus(Assignment assignment)
        {
            var now = DateTime.UtcNow;

            // 1. Upcoming - chưa đến StartDate
            if (assignment.StartDate.HasValue && now < assignment.StartDate.Value)
                return AssignmentStatusEnum.Upcoming.ToString();

            // 2. Active - đang trong thời gian nộp bài
            if (now <= assignment.Deadline)
                return AssignmentStatusEnum.Active.ToString();

            // 4. InReview - cho STUDENT: từ sau Deadline đến ReviewDeadline
            if (now <= assignment.ReviewDeadline)
                return AssignmentStatusEnum.InReview.ToString();

            // 5. Closed - sau ReviewDeadline
            return AssignmentStatusEnum.Closed.ToString();
        }


        public async Task<BaseResponse<IEnumerable<AssignmentResponse>>> GetActiveAssignmentsByCourseInstanceAsync(int courseInstanceId, int? studentId = null)
        {
            try
            {
                var assignments = await _context.Assignments
                    .Where(a => a.CourseInstanceId == courseInstanceId &&
                               a.Status != "Draft" &&
                               a.Status != "Archived")
                    .Include(a => a.CourseInstance)
                    .Include(a => a.Rubric)
                    .ToListAsync();

                // Filter assignments based on timeline và trạng thái
                var now = DateTime.UtcNow;
                var activeAssignments = assignments
                    .Where(a =>
                        (a.StartDate == null || a.StartDate <= now) &&
                        (a.FinalDeadline == null || now <= a.FinalDeadline)
                    )
                    .ToList();

                // Cập nhật trạng thái real-time
                foreach (var assignment in activeAssignments)
                {
                    assignment.Status = CalculateAssignmentStatus(assignment);
                }

                List<AssignmentResponse> response = new List<AssignmentResponse>();
                foreach (var assignment in activeAssignments)
                {
                    var assignmentResponse = await MapToResponse(assignment);
                    response.Add(assignmentResponse);
                }
                return new BaseResponse<IEnumerable<AssignmentResponse>>(
                    "Active assignments retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<AssignmentResponse>>(
                    $"Error retrieving active assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<AssignmentResponse>> CloneAssignmentAsync(int sourceAssignmentId, int targetCourseInstanceId, CloneAssignmentRequest request)
        {
            try
            {
                var sourceAssignment = await _context.Assignments
                    .Include(a => a.Rubric)
                        .ThenInclude(r => r.Criteria)
                    .FirstOrDefaultAsync(a => a.AssignmentId == sourceAssignmentId);

                if (sourceAssignment == null)
                {
                    return new BaseResponse<AssignmentResponse>("Source assignment not found", StatusCodeEnum.NotFound_404, null);
                }

                var targetCourseInstance = await _courseInstanceRepository.GetByIdAsync(targetCourseInstanceId);
                if (targetCourseInstance == null)
                {
                    return new BaseResponse<AssignmentResponse>("Target course instance not found", StatusCodeEnum.NotFound_404, null);
                }

                // Clone assignment
                var clonedAssignment = new Assignment
                {
                    CourseInstanceId = targetCourseInstanceId,
                    Title = request.NewTitle ?? $"{sourceAssignment.Title} (Clone)",
                    Description = sourceAssignment.Description,
                    Guidelines = sourceAssignment.Guidelines,
                    StartDate = request.NewStartDate ?? sourceAssignment.StartDate,
                    Deadline = request.NewDeadline ?? sourceAssignment.Deadline,
                    FinalDeadline = request.NewFinalDeadline ?? sourceAssignment.FinalDeadline,
                    ReviewDeadline = request.NewReviewDeadline ?? sourceAssignment.ReviewDeadline,
                    NumPeerReviewsRequired = sourceAssignment.NumPeerReviewsRequired,
                    AllowCrossClass = sourceAssignment.AllowCrossClass,
                    IsBlindReview = sourceAssignment.IsBlindReview,
                    InstructorWeight = sourceAssignment.InstructorWeight,
                    PeerWeight = sourceAssignment.PeerWeight,
                    GradingScale = sourceAssignment.GradingScale,
                    Weight = sourceAssignment.Weight,
                    IncludeAIScore = sourceAssignment.IncludeAIScore,
                    Status = AssignmentStatusEnum.Draft.ToString(),
                    ClonedFromAssignmentId = sourceAssignmentId,
                    CreatedAt = DateTime.UtcNow
                };

                // Clone rubric nếu có
                if (sourceAssignment.Rubric != null)
                {
                    var clonedRubric = new Rubric
                    {
                        Title = sourceAssignment.Rubric.Title,
                        TemplateId = sourceAssignment.Rubric.TemplateId,
                        IsModified = sourceAssignment.Rubric.IsModified
                    };

                    _context.Rubrics.Add(clonedRubric);
                    await _context.SaveChangesAsync(); // Save để lấy RubricId

                    // Clone criteria
                    foreach (var criteria in sourceAssignment.Rubric.Criteria)
                    {
                        var clonedCriteria = new Criteria
                        {
                            RubricId = clonedRubric.RubricId,
                            CriteriaTemplateId = criteria.CriteriaTemplateId,
                            Title = criteria.Title,
                            Description = criteria.Description,
                            Weight = criteria.Weight,
                            MaxScore = criteria.MaxScore,
                            ScoringType = criteria.ScoringType,
                            ScoreLabel = criteria.ScoreLabel,
                            IsModified = criteria.IsModified
                        };
                        _context.Criteria.Add(clonedCriteria);
                    }

                    clonedAssignment.RubricId = clonedRubric.RubricId;
                }

                _context.Assignments.Add(clonedAssignment);
                await _context.SaveChangesAsync();
                // Copy penalties from source assignment
                var sourceLatePenalty = await GetAssignmentConfig(sourceAssignmentId, "LateSubmissionPenalty");
                var sourceMissingReviewPenalty = await GetAssignmentConfig(sourceAssignmentId, "MissingReviewPenalty");

                // Use request values if provided, otherwise use source values or default to 0
                var latePenalty = request.LateSubmissionPenalty?.ToString() ?? sourceLatePenalty ?? "0";
                var missingReviewPenalty = request.MissingReviewPenalty?.ToString() ?? sourceMissingReviewPenalty ?? "0";

                var response = await MapToResponse(clonedAssignment);
                return new BaseResponse<AssignmentResponse>(
                    "Assignment cloned successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentResponse>(
                    $"Error cloning assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private async Task<string> GetAssignmentConfig(int assignmentId, string key)
        {
            var configKey = $"{key}_{assignmentId}";
            var config = await _context.SystemConfigs.FirstOrDefaultAsync(sc => sc.ConfigKey == configKey);
            return config?.ConfigValue;
        }

        public async Task<BaseResponse<AssignmentResponse>> UpdateAssignmentTimelineAsync(int assignmentId, UpdateAssignmentTimelineRequest request)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AssignmentResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                }

                // Validate timeline: StartDate <= Deadline <= FinalDeadline
                if (request.StartDate.HasValue && request.Deadline.HasValue &&
                    request.StartDate.Value > request.Deadline.Value)
                {
                    return new BaseResponse<AssignmentResponse>("Start date must be before deadline", StatusCodeEnum.BadRequest_400, null);
                }

                if (request.Deadline.HasValue && request.FinalDeadline.HasValue &&
                    request.Deadline.Value > request.FinalDeadline.Value)
                {
                    return new BaseResponse<AssignmentResponse>("Deadline must be before final deadline", StatusCodeEnum.BadRequest_400, null);
                }

                // Cập nhật timeline
                if (request.StartDate.HasValue) assignment.StartDate = request.StartDate.Value;
                if (request.Deadline.HasValue) assignment.Deadline = request.Deadline.Value;
                if (request.FinalDeadline.HasValue) assignment.FinalDeadline = request.FinalDeadline.Value;
                if (request.ReviewDeadline.HasValue) assignment.ReviewDeadline = request.ReviewDeadline.Value;

                // Cập nhật trạng thái
                assignment.Status = CalculateAssignmentStatus(assignment);

                await _assignmentRepository.UpdateAsync(assignment);
                var response = await MapToResponse(assignment);

                return new BaseResponse<AssignmentResponse>(
                    "Assignment timeline updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentResponse>(
                    $"Error updating assignment timeline: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        // Thêm method để xử lý điểm số theo thang điểm
        private decimal CalculateScoreBasedOnGradingScale(decimal rawScore, string gradingScale)
        {
            {
                if (gradingScale == "PassFail")
                {
                    // Pass/Fail: >=50% là Pass (100), ngược lại Fail (0)
                    return rawScore >= 50 ? 100 : 0;
                }
                else // Scale10
                {
                    // Scale10: giữ nguyên điểm 0-10, làm tròn 1 chữ số
                    return Math.Round(rawScore, 1);
                }
            }
        }

        // Method để hiển thị điểm theo định dạng
        public string FormatScoreForDisplay(decimal score, string gradingScale)
        {
            if (gradingScale == "PassFail")
            {
                return score >= 50 ? "Pass" : "Fail";
            }
            else // Scale10
            {
                return Math.Round(score, 1).ToString("0.0");
            }
        }

        // Cập nhật method tính điểm review
        private async Task<decimal> CalculateReviewScore(Review review, Assignment assignment)
        {
            var criteriaFeedbacks = await _criteriaFeedbackRepository.GetByReviewIdAsync(review.ReviewId);

            if (!criteriaFeedbacks.Any())
                return 0;

            decimal totalScore = 0;
            decimal totalWeight = 0;

            foreach (var feedback in criteriaFeedbacks)
            {
                var criteria = await _criteriaRepository.GetByIdAsync(feedback.CriteriaId);
                if (criteria == null) continue;

                decimal score = feedback.ScoreAwarded ?? 0;
                decimal maxScore = criteria.MaxScore;
                decimal weight = criteria.Weight;

                // Tính điểm chuẩn hóa theo phần trăm
                decimal normalizedScore = maxScore > 0 ? (score / maxScore) * 100 : 0;
                totalScore += normalizedScore * weight;
                totalWeight += weight;
            }

            decimal rawScore = totalWeight > 0 ? totalScore / totalWeight : 0;

            // Áp dụng thang điểm
            return CalculateScoreBasedOnGradingScale(rawScore, assignment.GradingScale);
        }

        // Lấy assignment status summary
        public async Task<BaseResponse<AssignmentStatusSummaryResponse>> GetAssignmentStatusSummaryAsync(int courseInstanceId)
        {
            try
            {
                var assignments = await _context.Assignments
                    .Where(a => a.CourseInstanceId == courseInstanceId)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                var summary = new AssignmentStatusSummaryResponse
                {
                    TotalAssignments = assignments.Count,
                    DraftCount = assignments.Count(a => a.Status == "Draft"),
                    UpcomingCount = assignments.Count(a => a.Status == "Upcoming"),
                    ActiveCount = assignments.Count(a => a.Status == "Active"),
                    LateSubmissionCount = assignments.Count(a => a.Status == "LateSubmission"),
                    ClosedCount = assignments.Count(a => a.Status == "Closed"),
                    ArchivedCount = assignments.Count(a => a.Status == "Archived")
                };

                return new BaseResponse<AssignmentStatusSummaryResponse>(
                    "Assignment status summary retrieved successfully",
                    StatusCodeEnum.OK_200,
                    summary);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentStatusSummaryResponse>(
                    $"Error retrieving assignment status summary: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<AssignmentResponse>> PublishAssignmentAsync(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AssignmentResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Chỉ cho phép publish từ trạng thái Draft
                if (assignment.Status != AssignmentStatusEnum.Draft.ToString())
                {
                    return new BaseResponse<AssignmentResponse>(
                        $"Only Draft assignments can be published (current: {assignment.Status})",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Kiểm tra logic chuyển trạng thái
                if (assignment.StartDate.HasValue && DateTime.UtcNow < assignment.StartDate.Value)
                {
                    assignment.Status = AssignmentStatusEnum.Upcoming.ToString();
                }
                else
                {
                    assignment.Status = AssignmentStatusEnum.Active.ToString();
                }

                await _assignmentRepository.UpdateAsync(assignment);

                var response = await MapToResponse(assignment);
                return new BaseResponse<AssignmentResponse>(
                    $"Assignment published successfully (Status: {assignment.Status})",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AssignmentResponse>(
                    $"Error publishing assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task AutoUpdateUpcomingAssignmentsAsync()
        {
            var now = DateTime.UtcNow;
            var UpcomingAssignments = await _context.Assignments
                .Where(a => a.Status == AssignmentStatusEnum.Upcoming.ToString() &&
                            a.StartDate.HasValue &&
                            a.StartDate <= now)
                .ToListAsync();

            foreach (var assignment in UpcomingAssignments)
            {
                assignment.Status = AssignmentStatusEnum.Active.ToString();
            }

            if (UpcomingAssignments.Any())
            {
                await _context.SaveChangesAsync();
            }
        }


    }
}