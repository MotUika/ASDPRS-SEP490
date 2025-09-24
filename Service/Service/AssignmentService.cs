using BussinessObject.Models;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Assignment;
using Service.RequestAndResponse.Response.Assignment;
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

        public AssignmentService(
            IAssignmentRepository assignmentRepository,
            ICourseInstanceRepository courseInstanceRepository,
            IRubricRepository rubricRepository,
            ISubmissionRepository submissionRepository,
            IReviewRepository reviewRepository,
            ICourseInstructorRepository courseInstructorRepository,
            ICourseStudentRepository courseStudentRepository)
        {
            _assignmentRepository = assignmentRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _rubricRepository = rubricRepository;
            _submissionRepository = submissionRepository;
            _reviewRepository = reviewRepository;
            _courseInstructorRepository = courseInstructorRepository;
            _courseStudentRepository = courseStudentRepository;
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
                    ReviewDeadline = request.ReviewDeadline,
                    NumPeerReviewsRequired = request.NumPeerReviewsRequired,
                    AllowCrossClass = request.AllowCrossClass,
                    IsBlindReview = request.IsBlindReview,
                    InstructorWeight = request.InstructorWeight,
                    PeerWeight = request.PeerWeight,
                    IncludeAIScore = request.IncludeAIScore
                };

                await _assignmentRepository.AddAsync(assignment);

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
                var responses = new List<AssignmentResponse>();

                foreach (var assignment in assignments)
                {
                    responses.Add(await MapToResponse(assignment));
                }

                return new BaseResponse<List<AssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
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
                        Description = rubric.Description
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
                NumPeerReviewsRequired = assignment.NumPeerReviewsRequired,
                AllowCrossClass = assignment.AllowCrossClass,
                IsBlindReview = assignment.IsBlindReview,
                InstructorWeight = assignment.InstructorWeight,
                PeerWeight = assignment.PeerWeight,
                IncludeAIScore = assignment.IncludeAIScore,
                CourseName = courseInstance?.Course?.CourseName ?? string.Empty,
                CourseCode = courseInstance?.Course?.CourseCode ?? string.Empty,
                SectionCode = courseInstance?.SectionCode ?? string.Empty,
                CampusName = courseInstance?.Campus?.CampusName ?? string.Empty,
                Rubric = rubricResponse,
                SubmissionCount = submissions.Count(),
                ReviewCount = reviews.Count
            };
        }

        private async Task<AssignmentSummaryResponse> MapToSummaryResponse(Assignment assignment)
        {
            var courseInstance = await _courseInstanceRepository.GetByIdAsync(assignment.CourseInstanceId);
            var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignment.AssignmentId);

            return new AssignmentSummaryResponse
            {
                AssignmentId = assignment.AssignmentId,
                Title = assignment.Title,
                Description = assignment.Description,
                Deadline = assignment.Deadline,
                ReviewDeadline = assignment.ReviewDeadline,
                CourseName = courseInstance?.Course?.CourseName ?? string.Empty,
                SectionCode = courseInstance?.SectionCode ?? string.Empty,
                SubmissionCount = submissions.Count(),
                IsOverdue = DateTime.UtcNow > assignment.Deadline,
                DaysUntilDeadline = (int)(assignment.Deadline - DateTime.UtcNow).TotalDays,
                Status = GetAssignmentStatus(assignment)
            };
        }

        private string GetAssignmentStatus(Assignment assignment)
        {
            if (assignment.StartDate.HasValue && DateTime.UtcNow < assignment.StartDate.Value)
                return "Upcoming";
            if (DateTime.UtcNow > assignment.Deadline)
                return "Overdue";
            if (DateTime.UtcNow > assignment.Deadline.AddDays(-7))
                return "Due Soon";
            return "Active";
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
    }
}