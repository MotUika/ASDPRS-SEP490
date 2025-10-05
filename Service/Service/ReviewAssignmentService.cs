using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Notification;
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
        private readonly ASDPRSContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReviewAssignmentService> _logger;

        public ReviewAssignmentService(
            IReviewAssignmentRepository reviewAssignmentRepository,
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IReviewRepository reviewRepository,
            ICourseStudentRepository courseStudentRepository,
            ICourseInstanceRepository courseInstanceRepository,
            ICriteriaRepository criteriaRepository,
            IRubricRepository rubricRepository,
            ASDPRSContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<ReviewAssignmentService> logger)
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
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<BaseResponse<ReviewAssignmentResponse>> CreateReviewAssignmentAsync(CreateReviewAssignmentRequest request)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<ReviewAssignmentResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var reviewer = await _userRepository.GetByIdAsync(request.ReviewerUserId);
                if (reviewer == null)
                {
                    return new BaseResponse<ReviewAssignmentResponse>(
                        "Reviewer not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

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

                var response = await MapToResponseAsync(reviewAssignment);
                return new BaseResponse<ReviewAssignmentResponse>(
                    "Review assignment created successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review assignment");
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
                    responses.Add(await MapToResponseAsync(reviewAssignment));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Successfully created {responses.Count} review assignments",
                    StatusCodeEnum.Created_201,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk review assignments");
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

                if (!string.IsNullOrEmpty(request.Status))
                    reviewAssignment.Status = request.Status;

                if (request.Deadline.HasValue)
                    reviewAssignment.Deadline = request.Deadline.Value;

                if (request.IsAIReview.HasValue)
                    reviewAssignment.IsAIReview = request.IsAIReview.Value;

                await _reviewAssignmentRepository.UpdateAsync(reviewAssignment);

                var response = await MapToResponseAsync(reviewAssignment);
                return new BaseResponse<ReviewAssignmentResponse>(
                    "Review assignment updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review assignment");
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
                _logger.LogError(ex, "Error deleting review assignment");
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

                var response = await MapToResponseAsync(reviewAssignment);
                return new BaseResponse<ReviewAssignmentResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving review assignment");
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
                    responses.Add(await MapToResponseAsync(ra));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving review assignments by submission");
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
                    responses.Add(await MapToResponseAsync(ra));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving review assignments by reviewer");
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
                var responses = new List<ReviewAssignmentResponse>();

                foreach (var submission in submissions)
                {
                    var reviewAssignments = await _reviewAssignmentRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                    foreach (var ra in reviewAssignments)
                    {
                        responses.Add(await MapToResponseAsync(ra));
                    }
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving review assignments by assignment");
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
                var overdueAssignments = await _reviewAssignmentRepository.GetOverdueAsync(DateTime.UtcNow);
                var responses = new List<ReviewAssignmentResponse>();

                foreach (var ra in overdueAssignments)
                {
                    if (ra.Status != "Completed")
                    {
                        ra.Status = "Overdue";
                        await _reviewAssignmentRepository.UpdateAsync(ra);
                    }
                    responses.Add(await MapToResponseAsync(ra));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue review assignments");
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
                var pendingAssignments = new List<ReviewAssignment>();

                foreach (var ra in reviewAssignments)
                {
                    if (ra.Status == "Pending" || ra.Status == "Assigned" || ra.Status == "In Progress")
                    {
                        if (ra.Deadline < DateTime.UtcNow && ra.Status != "Completed")
                        {
                            ra.Status = "Overdue";
                            await _reviewAssignmentRepository.UpdateAsync(ra);
                        }
                        pendingAssignments.Add(ra);
                    }
                }

                var responses = new List<ReviewAssignmentResponse>();
                foreach (var ra in pendingAssignments)
                {
                    responses.Add(await MapToResponseAsync(ra));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending review assignments");
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving pending review assignments: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> AssignPeerReviewsAutomaticallyAsync(int assignmentId, int reviewsPerSubmission)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
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
                var currentStudents = courseStudents.Where(cs => !cs.IsPassed && cs.Status == "Enrolled").Select(cs => cs.UserId).ToList();
                var passedStudents = courseStudents.Where(cs => cs.IsPassed).Select(cs => cs.UserId).ToList();

                var currentStudentSet = new HashSet<int>(currentStudents);
                var passedStudentSet = new HashSet<int>(passedStudents);
                var allStudentSet = new HashSet<int>(currentStudents.Concat(passedStudents));

                var submissionIds = submissions.Select(s => s.SubmissionId).ToList();
                var reviewerAssignments = new Dictionary<int, HashSet<int>>();
                var submissionReviewers = new Dictionary<int, HashSet<int>>();

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

                // Phase 1: Assign current students
                foreach (var submission in submissions)
                {
                    var submitterId = submission.UserId;
                    var neededReviews = reviewsPerSubmission - submissionReviewers[submission.SubmissionId].Count;

                    if (neededReviews <= 0) continue;

                    var availableReviewers = currentStudentSet
                        .Where(userId => userId != submitterId &&
                               !submissionReviewers[submission.SubmissionId].Contains(userId))
                        .OrderBy(userId => reviewerAssignments[userId].Count)
                        .ThenBy(_ => random.Next())
                        .Take(neededReviews)
                        .ToList();

                    foreach (var reviewerId in availableReviewers)
                    {
                        if (reviewerAssignments[reviewerId].Count >= reviewsPerSubmission * 2)
                            continue;

                        await CreateReviewAssignment(submission.SubmissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submission.SubmissionId);
                        submissionReviewers[submission.SubmissionId].Add(reviewerId);
                        assignedCount++;
                    }
                }

                // Phase 2: Fill remaining slots with current students
                foreach (var submissionId in submissionReviewers.Keys.ToList())
                {
                    var currentReviewers = submissionReviewers[submissionId];
                    var submission = submissions.First(s => s.SubmissionId == submissionId);
                    var submitterId = submission.UserId;
                    var neededReviews = reviewsPerSubmission - currentReviewers.Count;

                    if (neededReviews <= 0) continue;

                    var availableReviewers = currentStudentSet
                        .Where(userId => userId != submitterId &&
                               !currentReviewers.Contains(userId) &&
                               reviewerAssignments[userId].Count < reviewsPerSubmission * 2)
                        .OrderBy(userId => reviewerAssignments[userId].Count)
                        .ThenBy(_ => random.Next())
                        .Take(neededReviews)
                        .ToList();

                    foreach (var reviewerId in availableReviewers)
                    {
                        await CreateReviewAssignment(submissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submissionId);
                        submissionReviewers[submissionId].Add(reviewerId);
                        assignedCount++;
                    }
                }

                // Phase 3: Use passed students if needed
                foreach (var submissionId in submissionReviewers.Keys.ToList())
                {
                    var currentReviewers = submissionReviewers[submissionId];
                    var submission = submissions.First(s => s.SubmissionId == submissionId);
                    var submitterId = submission.UserId;
                    var neededReviews = reviewsPerSubmission - currentReviewers.Count;

                    if (neededReviews <= 0) continue;

                    var availableReviewers = passedStudentSet
                        .Where(userId => userId != submitterId &&
                               !currentReviewers.Contains(userId) &&
                               reviewerAssignments[userId].Count < reviewsPerSubmission)
                        .OrderBy(userId => reviewerAssignments[userId].Count)
                        .ThenBy(_ => random.Next())
                        .Take(neededReviews)
                        .ToList();

                    foreach (var reviewerId in availableReviewers)
                    {
                        await CreateReviewAssignment(submissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submissionId);
                        submissionReviewers[submissionId].Add(reviewerId);
                        assignedCount++;
                    }
                }

                // Phase 4: Apply MissingReviewPenalty for incomplete assignments
                foreach (var submissionId in submissionReviewers.Keys)
                {
                    if (submissionReviewers[submissionId].Count < reviewsPerSubmission)
                    {
                        var submission = submissions.First(s => s.SubmissionId == submissionId);
                        var assignmentPenalty = await GetAssignmentConfig(assignment.AssignmentId, "MissingReviewPenalty");
                        if (decimal.TryParse(assignmentPenalty, out decimal missPenalty))
                        {
                            // Log penalty application (actual grade adjustment handled in CourseStudentService)
                            _logger.LogInformation($"Submission {submissionId} has insufficient reviews. Penalty: {missPenalty}%");
                        }
                    }
                }

                var submissionsWithInsufficientReviews = submissionReviewers
                    .Count(kv => kv.Value.Count < reviewsPerSubmission);
                var averageReviews = submissionReviewers.Average(kv => kv.Value.Count);

                var message = $"Assigned {assignedCount} peer reviews. " +
                             $"Average reviews per submission: {averageReviews:F1}. " +
                             $"Submissions with insufficient reviews: {submissionsWithInsufficientReviews}";

                if (submissionsWithInsufficientReviews > 0)
                {
                    message += $". Consider manual assignment for submission IDs: " +
                              string.Join(", ", submissionReviewers
                                  .Where(kv => kv.Value.Count < reviewsPerSubmission)
                                  .Select(kv => kv.Key));
                }

                await transaction.CommitAsync();

                return new BaseResponse<bool>(
                    message,
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error assigning peer reviews automatically");
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

                var currentStudents = courseStudents.Where(cs => !cs.IsPassed && cs.Status == "Enrolled").Select(cs => cs.UserId).ToHashSet();
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

                    var overdueCount = reviewAssignments.Count(ra => ra.Deadline < DateTime.UtcNow && ra.Status != "Completed");
                    string penaltyNote = string.Empty;
                    if (overdueCount > 0)
                    {
                        var missPenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "MissingReviewPenalty");
                        if (decimal.TryParse(missPenaltyStr, out decimal missPenalty))
                        {
                            penaltyNote = $" ({overdueCount} overdue reviews, {missPenalty}% penalty per review)";
                        }
                    }

                    var submissionStats = new SubmissionReviewStats
                    {
                        SubmissionId = submission.SubmissionId,
                        StudentName = studentName,
                        TotalReviews = reviewAssignments.Count(),
                        CurrentStudentReviews = reviewAssignments.Count(ra => currentStudents.Contains(ra.ReviewerUserId)),
                        PassedStudentReviews = reviewAssignments.Count(ra => passedStudents.Contains(ra.ReviewerUserId)),
                        Status = reviewAssignments.Count() >= assignment.NumPeerReviewsRequired ? "Complete" : $"Incomplete{penaltyNote}"
                    };
                    stats.SubmissionStats.Add(submissionStats);
                }

                stats.AverageReviewsPerSubmission = stats.SubmissionStats.Any() ? stats.SubmissionStats.Average(s => s.TotalReviews) : 0;
                stats.CompletedSubmissions = stats.SubmissionStats.Count(s => s.Status.StartsWith("Complete"));

                return new BaseResponse<PeerReviewStatsResponse>(
                    "Peer review statistics retrieved successfully",
                    StatusCodeEnum.OK_200,
                    stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving peer review statistics");
                return new BaseResponse<PeerReviewStatsResponse>(
                    $"Error retrieving peer review statistics: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetPendingReviewsForStudentAsync(int studentId, int? courseInstanceId = null)
        {
            try
            {
                var reviewAssignments = await _reviewAssignmentRepository.GetByReviewerIdAsync(studentId);
                var pendingAssignments = new List<ReviewAssignment>();

                foreach (var ra in reviewAssignments)
                {
                    if (ra.Status == "Pending" || ra.Status == "Assigned" || ra.Status == "In Progress")
                    {
                        if (ra.Deadline < DateTime.UtcNow && ra.Status != "Completed")
                        {
                            ra.Status = "Overdue";
                            await _reviewAssignmentRepository.UpdateAsync(ra);
                        }
                        pendingAssignments.Add(ra);
                    }
                }

                var responses = new List<ReviewAssignmentResponse>();
                foreach (var ra in pendingAssignments)
                {
                    var submission = await _submissionRepository.GetByIdAsync(ra.SubmissionId);
                    if (submission == null) continue;

                    var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                    if (assignment == null) continue;

                    if (courseInstanceId.HasValue && assignment.CourseInstanceId != courseInstanceId.Value)
                        continue;

                    var response = await MapToResponseAsync(ra, true);
                    responses.Add(response);
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending reviews");
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving pending reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<ReviewAssignmentDetailResponse>> GetReviewAssignmentDetailsAsync(int reviewAssignmentId, int studentId)
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

                if (reviewAssignment.ReviewerUserId != studentId)
                {
                    return new BaseResponse<ReviewAssignmentDetailResponse>(
                        "Access denied: This review assignment does not belong to you",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<ReviewAssignmentDetailResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<ReviewAssignmentDetailResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                RubricResponse rubricResponse = null;
                if (assignment.RubricId.HasValue)
                {
                    var rubric = await _rubricRepository.GetByIdAsync(assignment.RubricId.Value);
                    if (rubric != null)
                    {
                        var criteria = await _criteriaRepository.GetByRubricIdAsync(rubric.RubricId);
                        rubricResponse = new RubricResponse
                        {
                            RubricId = rubric.RubricId,
                            Title = rubric.Title,
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

                string penaltyNote = string.Empty;
                if (reviewAssignment.Deadline < DateTime.UtcNow && reviewAssignment.Status != "Completed")
                {
                    var missPenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "MissingReviewPenalty");
                    if (decimal.TryParse(missPenaltyStr, out decimal missPenalty))
                    {
                        penaltyNote = $"Overdue: {missPenalty}% penalty may apply";
                    }
                }

                var response = new ReviewAssignmentDetailResponse
                {
                    ReviewAssignmentId = reviewAssignment.ReviewAssignmentId,
                    SubmissionId = reviewAssignment.SubmissionId,
                    AssignmentId = assignment.AssignmentId,
                    Status = reviewAssignment.Status + (string.IsNullOrEmpty(penaltyNote) ? "" : $" ({penaltyNote})"),
                    AssignedAt = reviewAssignment.AssignedAt,
                    Deadline = reviewAssignment.Deadline,
                    AssignmentTitle = assignment.Title,
                    StudentName = "Anonymous",
                    FileUrl = submission.FileUrl ?? string.Empty,
                    FileName = submission.FileName ?? string.Empty,
                    Rubric = rubricResponse
                };

                return new BaseResponse<ReviewAssignmentDetailResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving review assignment details");
                return new BaseResponse<ReviewAssignmentDetailResponse>(
                    $"Error retrieving review assignment details: {ex.Message}",
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
                Deadline = assignment.ReviewDeadline ?? assignment.Deadline.AddDays(7),
                IsAIReview = false
            };

            await _reviewAssignmentRepository.AddAsync(reviewAssignment);
        }

        private async Task<string> GetAssignmentConfig(int assignmentId, string key)
        {
            var configKey = $"{key}_{assignmentId}";
            var config = await _context.SystemConfigs.FirstOrDefaultAsync(sc => sc.ConfigKey == configKey);
            return config?.ConfigValue;
        }
        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetPendingReviewsAcrossAllClassesAsync(int studentId)
        {
            try
            {
                // Lấy tất cả course instances mà sinh viên tham gia
                var courseStudents = await _courseStudentRepository.GetByStudentIdAsync(studentId);
                var enrolledCourseInstanceIds = courseStudents
                    .Where(cs => cs.Status == "Enrolled")
                    .Select(cs => cs.CourseInstanceId)
                    .ToList();

                if (!enrolledCourseInstanceIds.Any())
                {
                    return new BaseResponse<List<ReviewAssignmentResponse>>(
                        "Student is not enrolled in any courses",
                        StatusCodeEnum.OK_200,
                        new List<ReviewAssignmentResponse>());
                }

                var allPendingReviews = new List<ReviewAssignmentResponse>();

                // Lấy pending reviews từ tất cả các lớp
                foreach (var courseInstanceId in enrolledCourseInstanceIds)
                {
                    var reviewsInCourse = await GetPendingReviewsForStudentAsync(studentId, courseInstanceId);
                    if (reviewsInCourse.StatusCode == StatusCodeEnum.OK_200 && reviewsInCourse.Data != null)
                    {
                        allPendingReviews.AddRange(reviewsInCourse.Data);
                    }
                }

                // Sắp xếp theo deadline gần nhất
                allPendingReviews = allPendingReviews
                    .OrderBy(r => r.Deadline)
                    .ThenBy(r => r.AssignmentTitle)
                    .ToList();

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Found {allPendingReviews.Count} pending reviews across {enrolledCourseInstanceIds.Count} classes",
                    StatusCodeEnum.OK_200,
                    allPendingReviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending reviews across all classes for student {StudentId}", studentId);
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving pending reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private async Task<ReviewAssignmentResponse> MapToResponseAsync(ReviewAssignment reviewAssignment, bool forStudent = false)
        {
            var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
            var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;
            var courseInstance = assignment != null ? await _courseInstanceRepository.GetByIdAsync(assignment.CourseInstanceId) : null;
            var reviewer = await _userRepository.GetByIdAsync(reviewAssignment.ReviewerUserId);
            var student = submission != null ? await _userRepository.GetByIdAsync(submission.UserId) : null;

            var reviews = await _reviewRepository.GetByReviewAssignmentIdAsync(reviewAssignment.ReviewAssignmentId);
            var reviewResponses = reviews.Select(review => new ReviewResponse
            {
                ReviewId = review.ReviewId,
                OverallScore = review.OverallScore,
                GeneralFeedback = review.GeneralFeedback,
                ReviewedAt = review.ReviewedAt,
                ReviewType = review.ReviewType,
                FeedbackSource = review.FeedbackSource
            }).ToList();

            string penaltyNote = string.Empty;
            if (reviewAssignment.Deadline < DateTime.UtcNow && reviewAssignment.Status != "Completed")
            {
                var missPenaltyStr = await GetAssignmentConfig(assignment?.AssignmentId ?? 0, "MissingReviewPenalty");
                if (decimal.TryParse(missPenaltyStr, out decimal missPenalty))
                {
                    penaltyNote = $" (Overdue: {missPenalty}% penalty)";
                }
            }

            var reviewerName = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}".Trim() : string.Empty;
            var studentName = forStudent || (assignment?.IsBlindReview == true) ? "Anonymous" : (student != null ? $"{student.FirstName} {student.LastName}".Trim() : string.Empty);
            var studentCode = forStudent || (assignment?.IsBlindReview == true) ? string.Empty : (student?.StudentCode ?? string.Empty);

            return new ReviewAssignmentResponse
            {
                ReviewAssignmentId = reviewAssignment.ReviewAssignmentId,
                SubmissionId = reviewAssignment.SubmissionId,
                ReviewerUserId = reviewAssignment.ReviewerUserId,
                Status = reviewAssignment.Status + penaltyNote,
                AssignedAt = reviewAssignment.AssignedAt,
                Deadline = reviewAssignment.Deadline,
                IsAIReview = reviewAssignment.IsAIReview,
                ReviewerName = reviewerName,
                ReviewerEmail = reviewer?.Email ?? string.Empty,
                AssignmentTitle = assignment?.Title ?? string.Empty,
                AssignmentDescription = assignment?.Description ?? string.Empty,
                AssignmentDeadline = assignment?.Deadline ?? DateTime.MinValue,
                CourseName = courseInstance?.Course?.CourseName ?? string.Empty,
                CourseCode = courseInstance?.Course?.CourseCode ?? string.Empty,
                SectionCode = courseInstance?.SectionCode ?? string.Empty,
                StudentName = studentName,
                StudentCode = studentCode,
                FileUrl = submission?.FileUrl ?? string.Empty,
                FileName = submission?.FileName ?? string.Empty,
                Keywords = submission?.Keywords ?? string.Empty,
                SubmittedAt = submission?.SubmittedAt ?? DateTime.MinValue,
                Reviews = reviewResponses
            };
        }
    }
}