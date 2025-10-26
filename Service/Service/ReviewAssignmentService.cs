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
        private static readonly ThreadLocal<Random> _threadLocalRandom =
    new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
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

                var currentWorkload = await CalculateFairWorkload(assignmentId, allStudentSet.ToList());

                var totalSubmissions = submissions.Count();
                var totalStudents = allStudentSet.Count;
                var maxReviewsPerStudent = (int)Math.Ceiling((double)(totalSubmissions * reviewsPerSubmission) / totalStudents);
                maxReviewsPerStudent = Math.Max(maxReviewsPerStudent, 1); // Đảm bảo ít nhất 1

                var reviewerAssignments = new Dictionary<int, HashSet<int>>();
                var submissionReviewers = new Dictionary<int, HashSet<int>>();

                foreach (var userId in allStudentSet)
                {
                    reviewerAssignments[userId] = new HashSet<int>();
                    if (currentWorkload.ContainsKey(userId))
                    {
                    }
                }
                foreach (var submissionId in submissionIds)
                {
                    submissionReviewers[submissionId] = new HashSet<int>();
                }

                var random = new Random();
                var assignedCount = 0;

                // PHASE 1: SẮP XẾP THEO WORKLOAD HIỆN TẠI
                var availableCurrentStudents = currentStudentSet
                    .OrderBy(userId => currentWorkload.ContainsKey(userId) ? currentWorkload[userId] : 0)
                    .ThenBy(_ => random.Next())
                    .ToList();

                foreach (var submission in submissions)
                {
                    var submitterId = submission.UserId;
                    var neededReviews = reviewsPerSubmission - submissionReviewers[submission.SubmissionId].Count;

                    if (neededReviews <= 0) continue;

                    var availableReviewers = availableCurrentStudents
                        .Where(userId => userId != submitterId &&
                               !submissionReviewers[submission.SubmissionId].Contains(userId) &&
                               currentWorkload[userId] < maxReviewsPerStudent) // KIỂM TRA GIỚI HẠN MAX
                        .Take(neededReviews)
                        .ToList();

                    foreach (var reviewerId in availableReviewers)
                    {
                        await CreateReviewAssignment(submission.SubmissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submission.SubmissionId);
                        submissionReviewers[submission.SubmissionId].Add(reviewerId);
                        currentWorkload[reviewerId]++;
                        assignedCount++;
                    }

                    // CẬP NHẬT LẠI THỨ TỰ SAU MỖI SUBMISSION
                    availableCurrentStudents = availableCurrentStudents
                        .OrderBy(userId => currentWorkload[userId])
                        .ThenBy(_ => random.Next())
                        .ToList();
                }

                //PHASE 2: FILL SLOTS CÒN THIẾU VỚI WORKLOAD CÂN BẰNG
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
                               currentWorkload[userId] < maxReviewsPerStudent)
                        .OrderBy(userId => currentWorkload[userId])
                        .ThenBy(_ => random.Next())
                        .Take(neededReviews)
                        .ToList();

                    foreach (var reviewerId in availableReviewers)
                    {
                        await CreateReviewAssignment(submissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submissionId);
                        submissionReviewers[submissionId].Add(reviewerId);
                        currentWorkload[reviewerId]++; 
                        assignedCount++;
                    }
                }

                //PHASE 3: DÙNG PASSED STUDENTS VỚI WORKLOAD CÂN BẰNG
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
                               currentWorkload[userId] < maxReviewsPerStudent)
                        .OrderBy(userId => currentWorkload[userId])
                        .ThenBy(_ => random.Next())
                        .Take(neededReviews)
                        .ToList();

                    foreach (var reviewerId in availableReviewers)
                    {
                        await CreateReviewAssignment(submissionId, reviewerId, assignment);
                        reviewerAssignments[reviewerId].Add(submissionId);
                        submissionReviewers[submissionId].Add(reviewerId);
                        currentWorkload[reviewerId]++;
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
                            _logger.LogInformation($"Submission {submissionId} has insufficient reviews. Penalty: {missPenalty}%");
                        }
                    }
                }

                var submissionsWithInsufficientReviews = submissionReviewers
                    .Count(kv => kv.Value.Count < reviewsPerSubmission);
                var averageReviews = submissionReviewers.Average(kv => kv.Value.Count);

                //THÊM THỐNG KÊ WORKLOAD VÀO MESSAGE
                var avgWorkload = currentWorkload.Values.Average();
                var minWorkload = currentWorkload.Values.Min();
                var maxWorkload = currentWorkload.Values.Max();

                var message = $"Assigned {assignedCount} peer reviews. " +
                             $"Average reviews per submission: {averageReviews:F1}. " +
                             $"Workload - Avg: {avgWorkload:F1}, Min: {minWorkload}, Max: {maxWorkload}. " +
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

        // Method để tính workflow cho từng student cần xem thêm
        private async Task<Dictionary<int, int>> CalculateFairWorkload(int assignmentId, List<int> allStudents)
        {
            var workload = new Dictionary<int, int>();

            // KHỞI TẠO WORKLOAD = 0 CHO TẤT CẢ STUDENTS
            foreach (var studentId in allStudents)
            {
                workload[studentId] = 0;
            }

            // ĐẾM SỐ REVIEW ASSIGNMENTS HIỆN CÓ
            var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
            foreach (var submission in submissions)
            {
                var reviewAssignments = await _reviewAssignmentRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                foreach (var ra in reviewAssignments)
                {
                    if (workload.ContainsKey(ra.ReviewerUserId))
                    {
                        workload[ra.ReviewerUserId]++;
                    }
                }
            }

            return workload;
        }

        public async Task<Dictionary<int, int>> GetWorkloadDistribution(int assignmentId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) return new Dictionary<int, int>();

            var courseStudents = await _courseStudentRepository.GetByCourseInstanceIdAsync(assignment.CourseInstanceId);
            var allStudents = courseStudents.Select(cs => cs.UserId).ToList();

            return await CalculateFairWorkload(assignmentId, allStudents);
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
                    if (ra.Status == "Assigned")
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
                    AssignmentDescription = assignment.Description,
                    AssignmentGuidelines = assignment.Guidelines,
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
                AssignmentGuidelines = assignment?.Guidelines ?? string.Empty,
                AssignmentDeadline = assignment?.Deadline ?? DateTime.MinValue,
                AssignmentStatus = assignment?.Status ?? string.Empty,
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

        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetPendingReviewsByAssignmentAsync(int assignmentId, int reviewerId)
        {
            try
            {
                // Lấy tất cả submissions của assignment
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);

                // Lấy tất cả review assignments của reviewer
                var reviewerAssignments = await _reviewAssignmentRepository.GetByReviewerIdAsync(reviewerId);

                // Lọc các review assignment trong assignment này và chưa hoàn thành
                var pendingReviews = new List<ReviewAssignment>();

                foreach (var submission in submissions)
                {
                    var reviewAssignment = reviewerAssignments
                        .FirstOrDefault(ra => ra.SubmissionId == submission.SubmissionId &&
                                         ra.ReviewerUserId == reviewerId &&
                                         ra.Status != "Completed");

                    if (reviewAssignment != null)
                    {
                        pendingReviews.Add(reviewAssignment);
                    }
                }

                var responses = new List<ReviewAssignmentResponse>();
                foreach (var ra in pendingReviews)
                {
                    responses.Add(await MapToResponseAsync(ra, true));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Found {responses.Count} pending reviews in assignment",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending reviews by assignment {AssignmentId} for reviewer {ReviewerId}", assignmentId, reviewerId);
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error retrieving pending reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<ReviewAssignmentDetailResponse>> GetRandomPendingReviewByAssignmentAsync(int assignmentId, int reviewerId)
        {
            try
            {
                // Lấy tất cả submissions của assignment
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);

                // Lấy tất cả review assignments của reviewer này
                var reviewerAssignments = await _reviewAssignmentRepository.GetByReviewerIdAsync(reviewerId);

                // Lọc các submission mà reviewer CHƯA review và KHÔNG phải bài của chính họ
                var availableSubmissions = submissions
                    .Where(s => s.UserId != reviewerId &&
                           !reviewerAssignments.Any(ra =>
                               ra.SubmissionId == s.SubmissionId &&
                               ra.ReviewerUserId == reviewerId &&
                               ra.Status == "Completed"))
                    .ToList();

                if (!availableSubmissions.Any())
                {
                    return new BaseResponse<ReviewAssignmentDetailResponse>(
                        "No available submissions to review in this assignment",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Chọn ngẫu nhiên 1 submission
                var random = new Random();
                var selectedSubmission = availableSubmissions[random.Next(availableSubmissions.Count)];

                // Tạo review assignment mới nếu chưa có
                var existingReviewAssignment = reviewerAssignments
                    .FirstOrDefault(ra => ra.SubmissionId == selectedSubmission.SubmissionId && ra.ReviewerUserId == reviewerId);

                ReviewAssignment reviewAssignment;

                if (existingReviewAssignment == null)
                {
                    var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                    reviewAssignment = new ReviewAssignment
                    {
                        SubmissionId = selectedSubmission.SubmissionId,
                        ReviewerUserId = reviewerId,
                        Status = "Assigned",
                        AssignedAt = DateTime.UtcNow,
                        Deadline = assignment.ReviewDeadline ?? assignment.Deadline.AddDays(7),
                        IsAIReview = false
                    };

                    await _reviewAssignmentRepository.AddAsync(reviewAssignment);
                }
                else
                {
                    reviewAssignment = existingReviewAssignment;
                }

                // Trả về review assignment details
                return await GetReviewAssignmentDetailsAsync(reviewAssignment.ReviewAssignmentId, reviewerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random pending review for assignment {AssignmentId} and reviewer {ReviewerId}", assignmentId, reviewerId);
                return new BaseResponse<ReviewAssignmentDetailResponse>(
                    $"Error getting random review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        // Trong ReviewAssignmentService
        public async Task<BaseResponse<ReviewAssignmentDetailResponse>> GetRandomReviewAssignmentAsync(int assignmentId, int reviewerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<ReviewAssignmentDetailResponse>(
                        "Assignment not found", StatusCodeEnum.NotFound_404, null);
                }

                if (assignment.Status != "InReview")
                {
                    return new BaseResponse<ReviewAssignmentDetailResponse>(
                        "Assignment is not in review phase yet", StatusCodeEnum.BadRequest_400, null);
                }

                var existingInProgressAssignment = await _context.ReviewAssignments
                    .Include(ra => ra.Submission)
                    .ThenInclude(s => s.Assignment)
                    .FirstOrDefaultAsync(ra =>
                        ra.ReviewerUserId == reviewerId &&
                        ra.Submission.AssignmentId == assignmentId &&
                        (ra.Status == "Assigned" || ra.Status == "In Progress"));

                if (existingInProgressAssignment != null)
                {
                    await transaction.CommitAsync();
                    return await GetReviewAssignmentDetailsAsync(existingInProgressAssignment.ReviewAssignmentId, reviewerId);
                }

                // 🔴 ƯU TIÊN 2: Tìm bài đã được gán nhưng chưa bắt đầu (có thể do back ra)
                var existingAssignedAssignment = await _context.ReviewAssignments
                    .Include(ra => ra.Submission)
                    .ThenInclude(s => s.Assignment)
                    .FirstOrDefaultAsync(ra =>
                        ra.ReviewerUserId == reviewerId &&
                        ra.Submission.AssignmentId == assignmentId &&
                        ra.Status == "Assigned");

                if (existingAssignedAssignment != null)
                {
                    // Update status để tracking
                    existingAssignedAssignment.Status = "In Progress";
                    await _reviewAssignmentRepository.UpdateAsync(existingAssignedAssignment);
                    await transaction.CommitAsync();

                    return await GetReviewAssignmentDetailsAsync(existingAssignedAssignment.ReviewAssignmentId, reviewerId);
                }

                // Nếu không có bài nào đang dở, mới tìm bài mới
                var availableSubmissions = await _reviewAssignmentRepository.GetAvailableSubmissionsForReviewerAsync(assignmentId, reviewerId);

                if (!availableSubmissions.Any())
                {
                    return new BaseResponse<ReviewAssignmentDetailResponse>(
                        "No available submissions to review", StatusCodeEnum.NotFound_404, null);
                }

                // Logic chọn bài ngẫu nhiên (giữ nguyên)
                var submissionsWithReviewCount = availableSubmissions.Select(s => new
                {
                    Submission = s,
                    ReviewCount = _context.ReviewAssignments.Count(ra => ra.SubmissionId == s.SubmissionId && ra.Status == "Completed")
                }).ToList();

                var minReviewCount = submissionsWithReviewCount.Min(x => x.ReviewCount);
                var prioritySubmissions = submissionsWithReviewCount.Where(x => x.ReviewCount == minReviewCount)
                                                                   .Select(x => x.Submission)
                                                                   .ToList();

                var random = _threadLocalRandom.Value;
                var selectedSubmission = prioritySubmissions[random.Next(prioritySubmissions.Count)];

                var reviewAssignment = new ReviewAssignment
                {
                    SubmissionId = selectedSubmission.SubmissionId,
                    ReviewerUserId = reviewerId,
                        Status = "Assigned",
                    AssignedAt = DateTime.UtcNow,
                    Deadline = assignment.ReviewDeadline ?? DateTime.UtcNow.AddDays(7),
                    IsAIReview = false
                };

                await _reviewAssignmentRepository.AddAsync(reviewAssignment);
                await transaction.CommitAsync();

                return await GetReviewAssignmentDetailsAsync(reviewAssignment.ReviewAssignmentId, reviewerId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error getting random review assignment");
                return new BaseResponse<ReviewAssignmentDetailResponse>(
                    $"Error getting random review assignment: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task<BaseResponse<List<ReviewAssignmentResponse>>> GetAvailableReviewsForStudentAsync(int assignmentId, int studentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<List<ReviewAssignmentResponse>>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Check if assignment is in review phase
                if (assignment.Status != "InReview")
                {
                    return new BaseResponse<List<ReviewAssignmentResponse>>(
                        "Assignment is not in review phase yet",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                var existingReviewAssignments = await _reviewAssignmentRepository.GetByReviewerIdAsync(studentId);

                var reviewedSubmissionIds = existingReviewAssignments
                    .Select(ra => ra.SubmissionId)
                    .ToHashSet();

                var availableSubmissions = submissions
                    .Where(s => s.UserId != studentId && !reviewedSubmissionIds.Contains(s.SubmissionId))
                    .ToList();

                var responses = new List<ReviewAssignmentResponse>();

                foreach (var submission in availableSubmissions)
                {
                    // Create temporary review assignment for display
                    var tempAssignment = new ReviewAssignment
                    {
                        SubmissionId = submission.SubmissionId,
                        ReviewerUserId = studentId,
                        Status = "Available",
                        AssignedAt = DateTime.UtcNow,
                        Deadline = assignment.ReviewDeadline ?? DateTime.UtcNow.AddDays(7),
                        IsAIReview = false
                    };

                    responses.Add(await MapToResponseAsync(tempAssignment, true));
                }

                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Found {responses.Count} available submissions to review",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available reviews");
                return new BaseResponse<List<ReviewAssignmentResponse>>(
                    $"Error getting available reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
    }
}