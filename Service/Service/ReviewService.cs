using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Request.Review;
using Service.RequestAndResponse.Response.CriteriaFeedback;
using Service.RequestAndResponse.Response.Review;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IReviewAssignmentRepository _reviewAssignmentRepository;
        private readonly ICriteriaFeedbackRepository _criteriaFeedbackRepository;
        private readonly ICriteriaRepository _criteriaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ICourseStudentRepository _courseStudentRepository;
        private readonly ASDPRSContext _context;

        public ReviewService(
            IReviewRepository reviewRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            ICriteriaFeedbackRepository criteriaFeedbackRepository,
            ICriteriaRepository criteriaRepository,
            IUserRepository userRepository,
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            ICourseStudentRepository courseStudentRepository,
            ASDPRSContext context)
        {
            _reviewRepository = reviewRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _criteriaFeedbackRepository = criteriaFeedbackRepository;
            _criteriaRepository = criteriaRepository;
            _userRepository = userRepository;
            _submissionRepository = submissionRepository;
            _assignmentRepository = assignmentRepository;
            _courseStudentRepository = courseStudentRepository;
            _context = context;
        }

        public async Task<BaseResponse<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request)
        {
            try
            {
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(request.ReviewAssignmentId);
                if (reviewAssignment == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var existingReviews = await _reviewRepository.GetByReviewAssignmentIdAsync(request.ReviewAssignmentId);
                if (existingReviews.Any(r => r.ReviewType == request.ReviewType && r.FeedbackSource == request.FeedbackSource))
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review already exists for this assignment with the same type and source",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                decimal? overallScore = null;
                if (request.CriteriaFeedbacks != null && request.CriteriaFeedbacks.Any(cf => cf.Score.HasValue))
                {
                    var validScores = request.CriteriaFeedbacks
                        .Where(cf => cf.Score.HasValue)
                        .Select(cf => cf.Score.Value)
                        .ToList();
                    if (validScores.Any())
                    {
                        overallScore = Math.Round(validScores.Average(), 2);
                    }
                }

                var review = new Review
                {
                    ReviewAssignmentId = request.ReviewAssignmentId,
                    OverallScore = overallScore,
                    GeneralFeedback = request.GeneralFeedback,
                    ReviewedAt = request.ReviewedAt ?? DateTime.UtcNow,
                    ReviewType = request.ReviewType,
                    FeedbackSource = request.FeedbackSource
                };

                await _reviewRepository.AddAsync(review);

                if (request.CriteriaFeedbacks != null && request.CriteriaFeedbacks.Any())
                {
                    foreach (var cfRequest in request.CriteriaFeedbacks)
                    {
                        var criteria = await _criteriaRepository.GetByIdAsync(cfRequest.CriteriaId);
                        if (criteria == null) continue;

                        var criteriaFeedback = new CriteriaFeedback
                        {
                            ReviewId = review.ReviewId,
                            CriteriaId = cfRequest.CriteriaId,
                            ScoreAwarded = cfRequest.Score,
                            Feedback = cfRequest.Feedback,
                            FeedbackSource = request.FeedbackSource
                        };

                        await _criteriaFeedbackRepository.AddAsync(criteriaFeedback);
                    }
                }

                reviewAssignment.Status = "Completed";
                await _reviewAssignmentRepository.UpdateAsync(reviewAssignment);

                var response = await MapToResponseAsync(review);
                return new BaseResponse<ReviewResponse>(
                    "Review created successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewResponse>(
                    $"Error creating review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<ReviewResponse>> UpdateReviewAsync(UpdateReviewRequest request)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId);
                if (review == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                review.OverallScore = request.OverallScore;
                review.GeneralFeedback = request.GeneralFeedback;
                review.ReviewedAt = request.ReviewedAt ?? review.ReviewedAt;
                review.ReviewType = request.ReviewType;
                review.FeedbackSource = request.FeedbackSource;

                await _reviewRepository.UpdateAsync(review);

                if (request.CriteriaFeedbacks != null && request.CriteriaFeedbacks.Any())
                {
                    var existingFeedbacks = await _criteriaFeedbackRepository.GetByReviewIdAsync(review.ReviewId);
                    foreach (var cf in existingFeedbacks)
                    {
                        await _criteriaFeedbackRepository.DeleteAsync(cf);
                    }

                    foreach (var cfRequest in request.CriteriaFeedbacks)
                    {
                        var criteria = await _criteriaRepository.GetByIdAsync(cfRequest.CriteriaId);
                        if (criteria == null) continue;

                        var criteriaFeedback = new CriteriaFeedback
                        {
                            ReviewId = review.ReviewId,
                            CriteriaId = cfRequest.CriteriaId,
                            ScoreAwarded = cfRequest.Score,
                            Feedback = cfRequest.Feedback,
                            FeedbackSource = request.FeedbackSource
                        };

                        await _criteriaFeedbackRepository.AddAsync(criteriaFeedback);
                    }

                    if (request.CriteriaFeedbacks.Any(cf => cf.Score.HasValue))
                    {
                        var validScores = request.CriteriaFeedbacks
                            .Where(cf => cf.Score.HasValue)
                            .Select(cf => cf.Score.Value)
                            .ToList();
                        if (validScores.Any())
                        {
                            review.OverallScore = Math.Round(validScores.Average(), 2);
                            await _reviewRepository.UpdateAsync(review);
                        }
                    }
                }

                var response = await MapToResponseAsync(review);
                return new BaseResponse<ReviewResponse>(
                    "Review updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewResponse>(
                    $"Error updating review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteReviewAsync(int reviewId)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);
                if (review == null)
                {
                    return new BaseResponse<bool>(
                        "Review not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                var criteriaFeedbacks = await _criteriaFeedbackRepository.GetByReviewIdAsync(reviewId);
                foreach (var cf in criteriaFeedbacks)
                {
                    await _criteriaFeedbackRepository.DeleteAsync(cf);
                }

                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(review.ReviewAssignmentId);
                if (reviewAssignment != null)
                {
                    reviewAssignment.Status = "Assigned";
                    await _reviewAssignmentRepository.UpdateAsync(reviewAssignment);
                }

                await _reviewRepository.DeleteAsync(review);
                return new BaseResponse<bool>(
                    "Review deleted successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error deleting review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<ReviewResponse>> GetReviewByIdAsync(int reviewId)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);
                if (review == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToResponseAsync(review);
                return new BaseResponse<ReviewResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewResponse>(
                    $"Error retrieving review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewResponse>>> GetReviewsByReviewAssignmentIdAsync(int reviewAssignmentId)
        {
            try
            {
                var reviews = await _reviewRepository.GetByReviewAssignmentIdAsync(reviewAssignmentId);
                var responses = new List<ReviewResponse>();

                foreach (var review in reviews)
                {
                    responses.Add(await MapToResponseAsync(review));
                }

                return new BaseResponse<List<ReviewResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewResponse>>(
                    $"Error retrieving reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewResponse>>> GetReviewsBySubmissionIdAsync(int submissionId)
        {
            try
            {
                var reviews = await _reviewRepository.GetBySubmissionIdAsync(submissionId);
                var responses = new List<ReviewResponse>();

                foreach (var review in reviews)
                {
                    responses.Add(await MapToResponseAsync(review));
                }

                return new BaseResponse<List<ReviewResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewResponse>>(
                    $"Error retrieving reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewResponse>>> GetReviewsByReviewerIdAsync(int reviewerId)
        {
            try
            {
                var reviews = await _reviewRepository.GetByReviewerIdAsync(reviewerId);
                var responses = new List<ReviewResponse>();

                foreach (var review in reviews)
                {
                    responses.Add(await MapToResponseAsync(review));
                }

                return new BaseResponse<List<ReviewResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewResponse>>(
                    $"Error retrieving reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<ReviewResponse>>> GetReviewsByAssignmentIdAsync(int assignmentId)
        {
            try
            {
                var reviews = await _reviewRepository.GetByAssignmentIdAsync(assignmentId);
                var responses = new List<ReviewResponse>();

                foreach (var review in reviews)
                {
                    responses.Add(await MapToResponseAsync(review));
                }

                return new BaseResponse<List<ReviewResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ReviewResponse>>(
                    $"Error retrieving reviews: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<ReviewResponse>> CreateAIReviewAsync(CreateReviewRequest request)
        {
            try
            {
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(request.ReviewAssignmentId);
                if (reviewAssignment == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Simulate AI-generated review (replace with actual AI service integration)
                var aiFeedback = $"AI-generated feedback for submission {submission.SubmissionId}: " +
                                $"This submission demonstrates {submission.Keywords}. " +
                                $"Content analysis: Well-structured but could improve clarity.";
                var aiScore = new Random().Next(70, 95); // Placeholder score

                request.FeedbackSource = "AI";
                request.ReviewType = "AI";
                request.ReviewedAt = DateTime.UtcNow;
                request.GeneralFeedback = aiFeedback;

                if (assignment.RubricId.HasValue)
                {
                    var criteria = await _criteriaRepository.GetByRubricIdAsync(assignment.RubricId.Value);
                    request.CriteriaFeedbacks = criteria.Select(c => new CriteriaFeedbackRequest
                    {
                        CriteriaId = c.CriteriaId,
                        Score = Math.Round(aiScore * (c.Weight / 100.0m), 2),
                        Feedback = $"AI evaluation for {c.Title}: Meets expectations."
                    }).ToList();
                }

                return await CreateReviewAsync(request);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewResponse>(
                    $"Error creating AI review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<ReviewResponse>> SubmitStudentReviewAsync(SubmitStudentReviewRequest request)
        {
            try
            {
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(request.ReviewAssignmentId);
                if (reviewAssignment == null || reviewAssignment.ReviewerUserId != request.ReviewerUserId)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review assignment not found or access denied",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var courseStudent = await _courseStudentRepository.GetByCourseInstanceAndUserAsync(assignment.CourseInstanceId, request.ReviewerUserId);
                if (courseStudent == null || (courseStudent.Status != "Enrolled" && !courseStudent.IsPassed))
                {
                    return new BaseResponse<ReviewResponse>(
                        "Access denied: You do not have permission to review this assignment",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                if (reviewAssignment.Status == "Completed")
                {
                    return new BaseResponse<ReviewResponse>(
                        "This review assignment has already been completed",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                if (reviewAssignment.Deadline < DateTime.UtcNow)
                {
                    reviewAssignment.Status = "Overdue";
                    await _reviewAssignmentRepository.UpdateAsync(reviewAssignment);
                    var missPenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "MissingReviewPenalty");
                    if (decimal.TryParse(missPenaltyStr, out decimal missPenalty))
                    {
                        // Log penalty application (actual grade adjustment handled in CourseStudentService)
                    }
                }

                decimal? overallScore = null;
                if (request.CriteriaFeedbacks != null && request.CriteriaFeedbacks.Any(cf => cf.Score.HasValue))
                {
                    var validScores = request.CriteriaFeedbacks
                        .Where(cf => cf.Score.HasValue)
                        .Select(cf => cf.Score.Value)
                        .ToList();
                    if (validScores.Any())
                    {
                        overallScore = Math.Round(validScores.Average(), 2);
                    }
                }

                var review = new Review
                {
                    ReviewAssignmentId = request.ReviewAssignmentId,
                    OverallScore = overallScore,
                    GeneralFeedback = request.GeneralFeedback,
                    ReviewedAt = DateTime.UtcNow,
                    ReviewType = "Peer",
                    FeedbackSource = "Student"
                };

                await _reviewRepository.AddAsync(review);

                if (request.CriteriaFeedbacks != null && request.CriteriaFeedbacks.Any())
                {
                    foreach (var cfRequest in request.CriteriaFeedbacks)
                    {
                        var criteria = await _criteriaRepository.GetByIdAsync(cfRequest.CriteriaId);
                        if (criteria == null) continue;

                        var criteriaFeedback = new CriteriaFeedback
                        {
                            ReviewId = review.ReviewId,
                            CriteriaId = cfRequest.CriteriaId,
                            ScoreAwarded = cfRequest.Score,
                            Feedback = cfRequest.Feedback,
                            FeedbackSource = "Student"
                        };

                        await _criteriaFeedbackRepository.AddAsync(criteriaFeedback);
                    }
                }

                reviewAssignment.Status = "Completed";
                await _reviewAssignmentRepository.UpdateAsync(reviewAssignment);

                var response = await MapToResponseAsync(review);
                return new BaseResponse<ReviewResponse>(
                    "Review submitted successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewResponse>(
                    $"Error submitting review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<ReviewResponse>> UpdateStudentReviewAsync(UpdateStudentReviewRequest request)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId);
                if (review == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(review.ReviewAssignmentId);
                if (reviewAssignment == null || reviewAssignment.ReviewerUserId != request.ReviewerUserId)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review assignment not found or access denied",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);

                // Check if still within review period
                if (assignment.ReviewDeadline.HasValue && DateTime.UtcNow > assignment.ReviewDeadline.Value)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Cannot edit review after review deadline",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                // Update review
                review.GeneralFeedback = request.GeneralFeedback;
                review.ReviewedAt = DateTime.UtcNow;

                // Update criteria feedbacks
                var existingFeedbacks = await _criteriaFeedbackRepository.GetByReviewIdAsync(review.ReviewId);

                // Remove existing feedbacks
                foreach (var cf in existingFeedbacks)
                {
                    await _criteriaFeedbackRepository.DeleteAsync(cf);
                }

                // Add new feedbacks
                if (request.CriteriaFeedbacks != null && request.CriteriaFeedbacks.Any())
                {
                    foreach (var cfRequest in request.CriteriaFeedbacks)
                    {
                        var criteria = await _criteriaRepository.GetByIdAsync(cfRequest.CriteriaId);
                        if (criteria == null) continue;

                        var criteriaFeedback = new CriteriaFeedback
                        {
                            ReviewId = review.ReviewId,
                            CriteriaId = cfRequest.CriteriaId,
                            ScoreAwarded = cfRequest.Score,
                            Feedback = cfRequest.Feedback,
                            FeedbackSource = "Student"
                        };

                        await _criteriaFeedbackRepository.AddAsync(criteriaFeedback);
                    }

                    // Recalculate overall score
                    var validScores = request.CriteriaFeedbacks
                        .Where(cf => cf.Score.HasValue)
                        .Select(cf => cf.Score.Value)
                        .ToList();

                    if (validScores.Any())
                    {
                        review.OverallScore = Math.Round(validScores.Average(), 2);
                    }
                }

                await _reviewRepository.UpdateAsync(review);

                var response = await MapToResponseAsync(review);
                return new BaseResponse<ReviewResponse>(
                    "Review updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ReviewResponse>(
                    $"Error updating review: {ex.Message}",
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

        private async Task<ReviewResponse> MapToResponseAsync(Review review)
        {
            var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(review.ReviewAssignmentId);
            var reviewerUser = reviewAssignment != null ? await _userRepository.GetByIdAsync(reviewAssignment.ReviewerUserId) : null;
            var submission = reviewAssignment != null ? await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId) : null;
            var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;

            string penaltyNote = string.Empty;
            if (reviewAssignment?.Deadline < DateTime.UtcNow && reviewAssignment.Status != "Completed")
            {
                var missPenaltyStr = await GetAssignmentConfig(assignment?.AssignmentId ?? 0, "MissingReviewPenalty");
                if (decimal.TryParse(missPenaltyStr, out decimal missPenalty))
                {
                    penaltyNote = $" (Overdue: {missPenalty}% penalty)";
                }
            }

            var criteriaFeedbacks = await _criteriaFeedbackRepository.GetByReviewIdAsync(review.ReviewId);
            var criteriaFeedbackResponses = new List<CriteriaFeedbackResponse>();

            foreach (var cf in criteriaFeedbacks)
            {
                var criteria = await _criteriaRepository.GetByIdAsync(cf.CriteriaId);
                criteriaFeedbackResponses.Add(new CriteriaFeedbackResponse
                {
                    CriteriaFeedbackId = cf.CriteriaFeedbackId,
                    ReviewId = review.ReviewId,
                    CriteriaId = cf.CriteriaId,
                    CriteriaTitle = criteria?.Title ?? string.Empty,
                    ScoreAwarded = cf.ScoreAwarded,
                    Feedback = cf.Feedback,
                    FeedbackSource = cf.FeedbackSource
                });
            }

            return new ReviewResponse
            {
                ReviewId = review.ReviewId,
                ReviewAssignmentId = review.ReviewAssignmentId,
                OverallScore = review.OverallScore,
                GeneralFeedback = review.GeneralFeedback + penaltyNote,
                ReviewedAt = review.ReviewedAt,
                ReviewType = review.ReviewType,
                FeedbackSource = review.FeedbackSource,
                SubmissionId = reviewAssignment?.SubmissionId ?? 0,
                ReviewerName = review.FeedbackSource == "AI" ? "AI System" : (reviewerUser != null ? $"{reviewerUser.FirstName} {reviewerUser.LastName}".Trim() : string.Empty),
                ReviewerEmail = review.FeedbackSource == "AI" ? string.Empty : (reviewerUser?.Email ?? string.Empty),
                AssignmentTitle = assignment?.Title ?? string.Empty,
                CourseName = assignment?.CourseInstance?.Course?.CourseName ?? string.Empty,
                CriteriaFeedbacks = criteriaFeedbackResponses
            };
        }
    }
}