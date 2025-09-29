using BussinessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Repository.Repository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.ReviewAssignment;
using Service.RequestAndResponse.Response.Criteria;
using Service.RequestAndResponse.Response.Review;
using Service.RequestAndResponse.Response.ReviewAssignment;
using Service.RequestAndResponse.Response.Rubric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class ReviewAssignmentService : IReviewAssignmentService
    {
        private readonly IReviewAssignmentRepository _reviewAssignmentRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ICourseStudentRepository _courseStudentRepository;
        private readonly ICourseInstanceRepository _courseInstanceRepository;
        private readonly ICriteriaRepository _criteriaRepository;
        private readonly IRubricRepository _rubricRepository;

        public ReviewAssignmentService(
            IReviewAssignmentRepository reviewAssignmentRepository,
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IReviewRepository reviewRepository,
            ICourseStudentRepository courseStudentRepository,
            ICourseInstanceRepository courseInstanceRepository,
            ICriteriaRepository criteriaRepository,
            IRubricRepository rubricRepository)
        {
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _submissionRepository = submissionRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _reviewRepository = reviewRepository;
            _courseStudentRepository = courseStudentRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _criteriaRepository = criteriaRepository;
            _rubricRepository = rubricRepository;
        }

        public async Task<BaseResponse<ReviewAssignmentResponse>> CreateReviewAssignmentAsync(CreateReviewAssignmentRequest request)
        {
            try
            {
                // Validate submission exists
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<ReviewAssignmentResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Validate reviewer exists
                var reviewer = await _userRepository.GetByIdAsync(request.ReviewerUserId);
                if (reviewer == null)
                {
                    return new BaseResponse<ReviewAssignmentResponse>(
                        "Reviewer not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Check if assignment already exists
                var existing = (await _reviewAssignmentRepository.GetBySubmissionIdAsync(request.SubmissionId))
                    .FirstOrDefault(ra => ra.ReviewerUserId == request.ReviewerUserId);

                if (existing != null)
                {
                    return new BaseResponse<ReviewAssignmentResponse>(
                        "Review assignment already exists for this submission and reviewer",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                var reviewAssignment = new ReviewAssignment
                {
                    SubmissionId = request.SubmissionId,
                    ReviewerUserId = request.ReviewerUserId,
                    Status = request.Status,
                    AssignedAt = DateTime.UtcNow,
                    Deadline = request.Deadline,
                    IsAIReview = request.IsAIReview
                };

                await _reviewAssignmentRepository.AddAsync(reviewAssignment);

                var response = await MapToResponse(reviewAssignment);
                return new BaseResponse<ReviewAssignmentResponse>(
                    "Review assignment created successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewAssignmentResponse>(
                    $"Error creating review assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> BulkCreateReviewAssignmentsAsync(BulkCreateReviewAssignmentRequest request)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<List<ReviewAssignmentResponse>>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var responses = new List<ReviewAssignmentResponse>();
                var existingAssignments = (await _reviewAssignmentRepository.GetBySubmissionIdAsync(request.SubmissionId))
                    .Select(ra => ra.ReviewerUserId)
                    .ToHashSet();

                foreach (var reviewerId in request.ReviewerUserIds)
                {
                    if (existingAssignments.Contains(reviewerId))
                        continue;

                    var reviewer = await _userRepository.GetByIdAsync(reviewerId);
                    if (reviewer == null)
                        continue;

                    var reviewAssignment = new ReviewAssignment
                    {
                        SubmissionId = request.SubmissionId,
                        ReviewerUserId = reviewerId,
                        Status = "Assigned",
                        AssignedAt = DateTime.UtcNow,
                        Deadline = request.Deadline,
                        IsAIReview = request.IsAIReview
                    };

                    await _reviewAssignmentRepository.AddAsync(reviewAssignment);
                    responses.Add(await MapToResponse(reviewAssignment));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Bulk review assignments created successfully",
                    StatusCodeEnum.Created_201,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error creating bulk review assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<ReviewAssignmentResponse>> UpdateReviewAssignmentAsync(UpdateReviewAssignmentRequest request)
        {
            try
            {
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(request.ReviewAssignmentId);
                if (reviewAssignment == null)
                {
                    return new BaseResponse<ReviewAssignmentResponse>(
                        "Review assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Status))
                    reviewAssignment.Status = request.Status;

                if (request.Deadline.HasValue)
                    reviewAssignment.Deadline = request.Deadline.Value;

                if (request.IsAIReview.HasValue)
                    reviewAssignment.IsAIReview = request.IsAIReview.Value;

                await _reviewAssignmentRepository.UpdateAsync(reviewAssignment);

                var response = await MapToResponse(reviewAssignment);
                return new BaseResponse<ReviewAssignmentResponse>(
                    "Review assignment updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewAssignmentResponse>(
                    $"Error updating review assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteReviewAssignmentAsync(int reviewAssignmentId)
        {
            try
            {
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(reviewAssignmentId);
                if (reviewAssignment == null)
                {
                    return new BaseResponse<bool>(
                        "Review assignment not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // Delete related reviews
                var reviews = await _reviewRepository.GetByReviewAssignmentIdAsync(reviewAssignmentId);
                foreach (var review in reviews)
                {
                    await _reviewRepository.DeleteAsync(review);
                }

                await _reviewAssignmentRepository.DeleteAsync(reviewAssignment);
                return new BaseResponse<bool>(
                    "Review assignment deleted successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error deleting review assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<ReviewAssignmentResponse>> GetReviewAssignmentByIdAsync(int reviewAssignmentId)
        {
            try
            {
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(reviewAssignmentId);
                if (reviewAssignment == null)
                {
                    return new BaseResponse<ReviewAssignmentResponse>(
                        "Review assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToResponse(reviewAssignment);
                return new BaseResponse<ReviewAssignmentResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewAssignmentResponse>(
                    $"Error retrieving review assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetReviewAssignmentsBySubmissionIdAsync(int submissionId)
        {
            try
            {
                var reviewAssignments = await _reviewAssignmentRepository.GetBySubmissionIdAsync(submissionId);
                var responses = new List<ReviewAssignmentResponse>();

                foreach (var ra in reviewAssignments)
                {
                    responses.Add(await MapToResponse(ra));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving review assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetReviewAssignmentsByReviewerIdAsync(int reviewerId)
        {
            try
            {
                var reviewAssignments = await _reviewAssignmentRepository.GetByReviewerIdAsync(reviewerId);
                var responses = new List<ReviewAssignmentResponse>();

                foreach (var ra in reviewAssignments)
                {
                    responses.Add(await MapToResponse(ra));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving review assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetReviewAssignmentsByAssignmentIdAsync(int assignmentId)
        {
            try
            {
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                var allReviewAssignments = new List<ReviewAssignmentResponse>();

                foreach (var submission in submissions)
                {
                    var reviewAssignments = await _reviewAssignmentRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                    foreach (var ra in reviewAssignments)
                    {
                        allReviewAssignments.Add(await MapToResponse(ra));
                    }
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    allReviewAssignments);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving review assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetOverdueReviewAssignmentsAsync()
        {
            try
            {
                // Thay thế GetAllAsync bằng cách lấy tất cả thông qua các method khác
                var allAssignments = new List<ReviewAssignment>();

                // Lấy tất cả review assignments bằng cách query từ các submission
                var submissions = await _submissionRepository.GetAllAsync();
                foreach (var submission in submissions)
                {
                    var assignments = await _reviewAssignmentRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                    allAssignments.AddRange(assignments);
                }

                var overdueAssignments = allAssignments
                    .Where(ra => ra.Deadline < DateTime.UtcNow && ra.Status != "Completed")
                    .ToList();

                var responses = new List<ReviewAssignmentResponse>();
                foreach (var ra in overdueAssignments)
                {
                    responses.Add(await MapToResponse(ra));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving overdue review assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetPendingReviewAssignmentsAsync(int reviewerId)
        {
            try
            {
                var reviewAssignments = await _reviewAssignmentRepository.GetByReviewerIdAsync(reviewerId);
                var pendingAssignments = reviewAssignments
                    .Where(ra => ra.Status == "Pending" || ra.Status == "Assigned" || ra.Status == "In Progress")
                    .ToList();

                var responses = new List<ReviewAssignmentResponse>();
                foreach (var ra in pendingAssignments)
                {
                    responses.Add(await MapToResponse(ra));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving pending review assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> AssignPeerReviewsAutomaticallyAsync(int assignmentId, int reviewsPerSubmission)
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

                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                if (!submissions.Any())
                {
                    return new BaseResponse<bool>(
                        "No submissions found for this assignment",
                        StatusCodeEnum.BadRequest_400,
                        false);
                }

                var courseInstanceId = assignment.CourseInstanceId;
                var courseStudents = await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId);

                // Separate current students and passed students
                var currentStudents = courseStudents.Where(cs => !cs.IsPassed).Select(cs => cs.UserId).ToList();
                var passedStudents = courseStudents.Where(cs => cs.IsPassed).Select(cs => cs.UserId).ToList();

                // Create hashsets for faster lookup
                var currentStudentSet = new HashSet<int>(currentStudents);
                var passedStudentSet = new HashSet<int>(passedStudents);
                var allStudentSet = new HashSet<int>(currentStudents.Concat(passedStudents));

                // Track assignments to ensure balanced distribution
                var submissionIds = submissions.Select(s => s.SubmissionId).ToList();
                var reviewerAssignments = new Dictionary<int, HashSet<int>>();
                var submissionReviewers = new Dictionary<int, HashSet<int>>();

                // Initialize dictionaries
                foreach (var userId in allStudentSet)
                {
                    reviewerAssignments[userId] = new HashSet<int>();
                }
                foreach (var submissionId in submissionIds)
                {
                    submissionReviewers[submissionId] = new HashSet<int>();
                }

                var random = new Random();
                var assignedCount = 0;

                // Phase 1: Assign current students first (mandatory)
                foreach (var submission in submissions)
                {
                    var submitterId = submission.UserId;
                    var neededReviews = reviewsPerSubmission - submissionReviewers[submission.SubmissionId].Count;

                    if (neededReviews <= 0) continue;

                    var availableCurrentStudents = currentStudentSet
                        .Where(userId => userId != submitterId &&
                               !submissionReviewers[submission.SubmissionId].Contains(userId))
                        .OrderBy(userId => reviewerAssignments[userId].Count)
                        .ThenBy(x => random.Next())
                        .ToList();

                    var studentsToAssign = availableCurrentStudents.Take(neededReviews).ToList();

                    foreach (var reviewerId in studentsToAssign)
                    {
                        if (reviewerAssignments[reviewerId].Count >= reviewsPerSubmission * 2)
                            continue;

                        await CreateReviewAssignment(submission.SubmissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submission.SubmissionId);
                        submissionReviewers[submission.SubmissionId].Add(reviewerId);
                        assignedCount++;
                    }
                }

                // Phase 2: Fill remaining slots with current students (redistribution)
                var submissionsWithMissingReviews = submissionReviewers
                    .Where(kv => kv.Value.Count < reviewsPerSubmission)
                    .OrderBy(kv => kv.Value.Count)
                    .ToList();

                foreach (var kv in submissionsWithMissingReviews)
                {
                    var submissionId = kv.Key;
                    var currentReviewers = kv.Value;

                    var submission = submissions.First(s => s.SubmissionId == submissionId);
                    var submitterId = submission.UserId;
                    var neededReviews = reviewsPerSubmission - currentReviewers.Count;

                    if (neededReviews <= 0) continue;

                    var availableCurrentStudents = currentStudentSet
                        .Where(userId => userId != submitterId &&
                               !currentReviewers.Contains(userId) &&
                               reviewerAssignments[userId].Count < reviewsPerSubmission * 2)
                        .OrderBy(userId => reviewerAssignments[userId].Count)
                        .ThenBy(x => random.Next())
                        .ToList();

                    var studentsToAssign = availableCurrentStudents.Take(neededReviews).ToList();

                    foreach (var reviewerId in studentsToAssign)
                    {
                        await CreateReviewAssignment(submissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submissionId);
                        submissionReviewers[submissionId].Add(reviewerId);
                        assignedCount++;
                    }
                }

                // Phase 3: Use passed students if still needed (optional)
                submissionsWithMissingReviews = submissionReviewers
                    .Where(kv => kv.Value.Count < reviewsPerSubmission)
                    .OrderBy(kv => kv.Value.Count)
                    .ToList();

                foreach (var kv in submissionsWithMissingReviews)
                {
                    var submissionId = kv.Key;
                    var currentReviewers = kv.Value;

                    var submission = submissions.First(s => s.SubmissionId == submissionId);
                    var submitterId = submission.UserId;
                    var neededReviews = reviewsPerSubmission - currentReviewers.Count;

                    if (neededReviews <= 0) continue;

                    var availablePassedStudents = passedStudentSet
                        .Where(userId => userId != submitterId &&
                               !currentReviewers.Contains(userId) &&
                               reviewerAssignments[userId].Count < reviewsPerSubmission)
                        .OrderBy(userId => reviewerAssignments[userId].Count)
                        .ThenBy(x => random.Next())
                        .ToList();

                    var studentsToAssign = availablePassedStudents.Take(neededReviews).ToList();

                    foreach (var reviewerId in studentsToAssign)
                    {
                        await CreateReviewAssignment(submissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submissionId);
                        submissionReviewers[submissionId].Add(reviewerId);
                        assignedCount++;
                    }
                }

                // Phase 4: Final check and report
                var finalStats = submissionReviewers.Select(kv => new
                {
                    SubmissionId = kv.Key,
                    ReviewCount = kv.Value.Count,
                    ReviewerTypes = kv.Value.Select(r => currentStudentSet.Contains(r) ? "Current" : "Passed").ToList()
                }).ToList();

                var submissionsWithInsufficientReviews = finalStats.Where(s => s.ReviewCount < reviewsPerSubmission).ToList();
                var averageReviews = finalStats.Average(s => s.ReviewCount);

                var message = $"Assigned {assignedCount} peer reviews. " +
                             $"Average reviews per submission: {averageReviews:F1}. " +
                             $"Submissions with insufficient reviews: {submissionsWithInsufficientReviews.Count}";

                if (submissionsWithInsufficientReviews.Any())
                {
                    message += $". Consider manual assignment for submission IDs: " +
                              string.Join(", ", submissionsWithInsufficientReviews.Select(s => s.SubmissionId));
                }

                return new BaseResponse<bool>(
                    message,
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error assigning peer reviews automatically: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<PeerReviewStatsResponse>> GetPeerReviewStatisticsAsync(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<PeerReviewStatsResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                var courseStudents = await _courseStudentRepository.GetByCourseInstanceIdAsync(assignment.CourseInstanceId);

                var currentStudents = courseStudents.Where(cs => !cs.IsPassed).Select(cs => cs.UserId).ToHashSet();
                var passedStudents = courseStudents.Where(cs => cs.IsPassed).Select(cs => cs.UserId).ToHashSet();

                var stats = new PeerReviewStatsResponse
                {
                    AssignmentId = assignmentId,
                    AssignmentTitle = assignment.Title,
                    TotalSubmissions = submissions.Count(),
                    RequiredReviewsPerSubmission = assignment.NumPeerReviewsRequired,
                    CurrentStudentCount = currentStudents.Count,
                    PassedStudentCount = passedStudents.Count
                };

                foreach (var submission in submissions)
                {
                    var reviewAssignments = await _reviewAssignmentRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                    var studentUser = await _userRepository.GetByIdAsync(submission.UserId);
                    var studentName = studentUser != null ? $"{studentUser.FirstName} {studentUser.LastName}".Trim() : "Unknown";

                    var submissionStats = new SubmissionReviewStats
                    {
                        SubmissionId = submission.SubmissionId,
                        StudentName = studentName,
                        TotalReviews = reviewAssignments.Count(),
                        CurrentStudentReviews = reviewAssignments.Count(ra => currentStudents.Contains(ra.ReviewerUserId)),
                        PassedStudentReviews = reviewAssignments.Count(ra => passedStudents.Contains(ra.ReviewerUserId)),
                        Status = reviewAssignments.Count() >= assignment.NumPeerReviewsRequired ? "Complete" : "Incomplete"
                    };
                    stats.SubmissionStats.Add(submissionStats);
                }

                stats.AverageReviewsPerSubmission = stats.SubmissionStats.Average(s => s.TotalReviews);
                stats.CompletedSubmissions = stats.SubmissionStats.Count(s => s.Status == "Complete");

                return new BaseResponse<PeerReviewStatsResponse>(
                    "Peer review statistics retrieved successfully",
                    StatusCodeEnum.OK_200,
                    stats);
            }
            catch (Exception ex)
            {
                return new BaseResponse<PeerReviewStatsResponse>(
                    $"Error retrieving peer review statistics: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private async Task CreateReviewAssignment(int submissionId, int reviewerId, Assignment assignment)
        {
            var reviewAssignment = new ReviewAssignment
            {
                SubmissionId = submissionId,
                ReviewerUserId = reviewerId,
                Status = "Assigned",
                AssignedAt = DateTime.UtcNow,
                Deadline = assignment.ReviewDeadline ?? assignment.Deadline,
                IsAIReview = false
            };

            await _reviewAssignmentRepository.AddAsync(reviewAssignment);
        }
        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetPendingReviewsForStudentAsync(int studentId, int? courseInstanceId = null)
        {
            try
            {
                var reviewAssignments = await _reviewAssignmentRepository.GetByReviewerIdAsync(studentId);

                // Mở rộng điều kiện status để bao gồm cả 'Pending'
                var pendingAssignments = reviewAssignments
                    .Where(ra => ra.Status == "Pending" || ra.Status == "Assigned" || ra.Status == "In Progress")
                    .ToList();

                var responses = new List<ReviewAssignmentResponse>();

                foreach (var ra in pendingAssignments)
                {
                    var submission = await _submissionRepository.GetByIdAsync(ra.SubmissionId);
                    if (submission == null) continue;

                    var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                    if (assignment == null) continue;

                    // Nếu có filter theo course instance, kiểm tra
                    if (courseInstanceId.HasValue && assignment.CourseInstanceId != courseInstanceId.Value)
                        continue;

                    // Kiểm tra deadline review
                    if (ra.Deadline < DateTime.UtcNow && ra.Status != "Completed")
                    {
                        ra.Status = "Overdue";
                        await _reviewAssignmentRepository.UpdateAsync(ra);
                    }

                    var response = await MapToStudentReviewResponse(ra);
                    responses.Add(response);
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving pending reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private async Task<ReviewAssignmentResponse> MapToStudentReviewResponse(ReviewAssignment reviewAssignment)
        {
            var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
            var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;
            var courseInstance = assignment != null ? await _courseInstanceRepository.GetByIdAsync(assignment.CourseInstanceId) : null;

            // Ẩn danh thông tin người nộp bài
            var studentName = "Anonymous";
            var studentCode = "Anonymous";

            return new ReviewAssignmentResponse
            {
                ReviewAssignmentId = reviewAssignment.ReviewAssignmentId,
                SubmissionId = reviewAssignment.SubmissionId,
                ReviewerUserId = reviewAssignment.ReviewerUserId,
                Status = reviewAssignment.Status,
                AssignedAt = reviewAssignment.AssignedAt,
                Deadline = reviewAssignment.Deadline,
                IsAIReview = reviewAssignment.IsAIReview,

                // Thông tin assignment
                AssignmentTitle = assignment?.Title ?? string.Empty,
                AssignmentDescription = assignment?.Description ?? string.Empty, // Sửa lỗi chính tả
                AssignmentDeadline = assignment?.Deadline ?? DateTime.MinValue,

                // Thông tin course
                CourseName = courseInstance?.Course?.CourseName ?? string.Empty,
                CourseCode = courseInstance?.Course?.CourseCode ?? string.Empty,
                SectionCode = courseInstance?.SectionCode ?? string.Empty,

                // Thông tin submission (ẩn danh)
                StudentName = studentName,
                StudentCode = studentCode,
                FileUrl = submission?.FileUrl ?? string.Empty,
                FileName = submission?.FileName ?? string.Empty,
                Keywords = submission?.Keywords ?? string.Empty,
                SubmittedAt = submission?.SubmittedAt ?? DateTime.MinValue
            };
        }

        public async Task<BaseResponse<ReviewAssignmentDetailResponse>> GetReviewAssignmentDetailsAsync(int reviewAssignmentId)
        {
            try
            {
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(reviewAssignmentId);
                if (reviewAssignment == null)
                {
                    return new BaseResponse<ReviewAssignmentDetailResponse>(
                        "Review assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
                var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;

                // Lấy rubric
                RubricResponse rubricResponse = null;
                if (assignment?.RubricId.HasValue == true)
                {
                    var rubric = await _rubricRepository.GetByIdAsync(assignment.RubricId.Value);
                    if (rubric != null)
                    {
                        var criteria = await _criteriaRepository.GetByRubricIdAsync(rubric.RubricId);
                        rubricResponse = new RubricResponse
                        {
                            RubricId = rubric.RubricId,
                            Title = rubric.Title,
                            Description = rubric.Description,
                            Criteria = criteria.Select(c => new CriteriaResponse
                            {
                                CriteriaId = c.CriteriaId,
                                Title = c.Title,
                                Description = c.Description,
                                MaxScore = c.MaxScore,
                                Weight = c.Weight
                            }).ToList()
                        };
                    }
                }

                var response = new ReviewAssignmentDetailResponse
                {
                    ReviewAssignmentId = reviewAssignment.ReviewAssignmentId,
                    SubmissionId = reviewAssignment.SubmissionId,
                    AssignmentId = assignment?.AssignmentId ?? 0,
                    Status = reviewAssignment.Status,
                    AssignedAt = reviewAssignment.AssignedAt,
                    Deadline = reviewAssignment.Deadline,
                    AssignmentTitle = assignment?.Title ?? string.Empty,
                    StudentName = "Anonymous", // Luôn ẩn danh
                    FileUrl = submission?.FileUrl ?? string.Empty,
                    FileName = submission?.FileName ?? string.Empty,
                    Rubric = rubricResponse
                };

                return new BaseResponse<ReviewAssignmentDetailResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewAssignmentDetailResponse>(
                    $"Error retrieving review assignment details: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        private async Task<ReviewAssignmentResponse> MapToResponse(ReviewAssignment reviewAssignment)
        {
            var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
            var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;
            var reviewer = await _userRepository.GetByIdAsync(reviewAssignment.ReviewerUserId);
            var student = submission != null ? await _userRepository.GetByIdAsync(submission.UserId) : null;

            var reviews = await _reviewRepository.GetByReviewAssignmentIdAsync(reviewAssignment.ReviewAssignmentId);
            var reviewResponses = new List<ReviewResponse>();

            foreach (var review in reviews)
            {
                reviewResponses.Add(new ReviewResponse
                {
                    ReviewId = review.ReviewId,
                    OverallScore = review.OverallScore,
                    GeneralFeedback = review.GeneralFeedback,
                    ReviewedAt = review.ReviewedAt,
                    ReviewType = review.ReviewType,
                    FeedbackSource = review.FeedbackSource
                });
            }

            var reviewerName = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}".Trim() : string.Empty;
            var studentName = assignment?.IsBlindReview == true ? "Anonymous" : (student != null ? $"{student.FirstName} {student.LastName}".Trim() : string.Empty);
            var studentCode = assignment?.IsBlindReview == true ? string.Empty : (student?.StudentCode ?? string.Empty);

            return new ReviewAssignmentResponse
            {
                ReviewAssignmentId = reviewAssignment.ReviewAssignmentId,
                SubmissionId = reviewAssignment.SubmissionId,
                ReviewerUserId = reviewAssignment.ReviewerUserId,
                Status = reviewAssignment.Status,
                AssignedAt = reviewAssignment.AssignedAt,
                Deadline = reviewAssignment.Deadline,
                IsAIReview = reviewAssignment.IsAIReview,
                ReviewerName = reviewerName,
                ReviewerEmail = reviewer?.Email ?? string.Empty,
                AssignmentTitle = assignment?.Title ?? string.Empty,
                StudentName = studentName,
                StudentCode = studentCode,
                CourseName = assignment?.CourseInstance?.Course?.CourseName ?? string.Empty,
                Reviews = reviewResponses
            };
        }
    }
}