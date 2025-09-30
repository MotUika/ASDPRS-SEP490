using BussinessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Repository.Repository;
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

        public ReviewService(
            IReviewRepository reviewRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            ICriteriaFeedbackRepository criteriaFeedbackRepository,
            ICriteriaRepository criteriaRepository,
            IUserRepository userRepository,
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            ICourseStudentRepository courseStudentRepository)
        {
            _reviewRepository = reviewRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _criteriaFeedbackRepository = criteriaFeedbackRepository;
            _criteriaRepository = criteriaRepository;
            _userRepository = userRepository;
            _submissionRepository = submissionRepository;
            _assignmentRepository = assignmentRepository;
            _courseStudentRepository = courseStudentRepository;
        }

        public async Task<BaseResponse<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request)
        {
            try
            {
                // Lấy review assignment
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(request.ReviewAssignmentId);
                if (reviewAssignment == null)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Kiểm tra xem review đã tồn tại chưa
                var existingReviews = await _reviewRepository.GetByReviewAssignmentIdAsync(request.ReviewAssignmentId);
                if (existingReviews.Any(r => r.ReviewType == request.ReviewType && r.FeedbackSource == request.FeedbackSource))
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review already exists for this assignment with the same type and source",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                // Tính OverallScore tự động từ criteria feedbacks
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

                // Tạo review
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

                // Tạo criteria feedbacks
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

                // Cập nhật review assignment status
                reviewAssignment.Status = "Completed";
                await _reviewAssignmentRepository.UpdateAsync(reviewAssignment);

                // Map to response
                var response = await MapToResponse(review);
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

                // Update fields
                review.OverallScore = request.OverallScore;
                review.GeneralFeedback = request.GeneralFeedback;
                review.ReviewedAt = request.ReviewedAt ?? review.ReviewedAt;
                review.ReviewType = request.ReviewType;
                review.FeedbackSource = request.FeedbackSource;

                await _reviewRepository.UpdateAsync(review);

                var response = await MapToResponse(review);
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

                // Delete related criteria feedbacks
                var criteriaFeedbacks = await _criteriaFeedbackRepository.GetByReviewIdAsync(reviewId);
                foreach (var cf in criteriaFeedbacks)
                {
                    await _criteriaFeedbackRepository.DeleteAsync(cf);
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

                var response = await MapToResponse(review);
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
                    responses.Add(await MapToResponse(review));
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
                    responses.Add(await MapToResponse(review));
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
                    responses.Add(await MapToResponse(review));
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
                // Get all submissions for the assignment
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                var allReviews = new List<ReviewResponse>();

                foreach (var submission in submissions)
                {
                    var reviews = await _reviewRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                    foreach (var review in reviews)
                    {
                        allReviews.Add(await MapToResponse(review));
                    }
                }

                return new BaseResponse<List<ReviewResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    allReviews);
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
                // Set AI-specific properties
                request.FeedbackSource = "AI";
                request.ReviewType = "AI";
                request.ReviewedAt = DateTime.UtcNow;

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
                // Kiểm tra review assignment tồn tại và thuộc về sinh viên này
                var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(request.ReviewAssignmentId);
                if (reviewAssignment == null || reviewAssignment.ReviewerUserId != request.ReviewerUserId)
                {
                    return new BaseResponse<ReviewResponse>(
                        "Review assignment not found or access denied",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Kiểm tra thêm: Sinh viên có quyền review bài này không
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

                // Kiểm tra sinh viên có trong lớp và có quyền review
                var courseStudent = await _courseStudentRepository.GetByCourseInstanceAndUserAsync(assignment.CourseInstanceId, request.ReviewerUserId);
                if (courseStudent == null || (courseStudent.Status != "Enrolled" && !courseStudent.IsPassed))
                {
                    return new BaseResponse<ReviewResponse>(
                        "Access denied: You do not have permission to review this assignment",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                // Kiểm tra review assignment chưa hoàn thành
                if (reviewAssignment.Status == "Completed")
                {
                    return new BaseResponse<ReviewResponse>(
                        "This review assignment has already been completed",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                // Tính điểm tổng
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

                // Tạo review
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

                // Tạo criteria feedbacks
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

                // Cập nhật review assignment status
                reviewAssignment.Status = "Completed";
                await _reviewAssignmentRepository.UpdateAsync(reviewAssignment);

                var response = await MapToResponse(review);
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

        private async Task<ReviewResponse> MapToResponse(Review review)
        {
            var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(review.ReviewAssignmentId);
            var reviewerUser = reviewAssignment != null ? await _userRepository.GetByIdAsync(reviewAssignment.ReviewerUserId) : null;

            var submission = reviewAssignment != null ? await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId) : null;
            var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;

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
                    FeedbackSource = review.FeedbackSource
                });
            }

            return new ReviewResponse
            {
                ReviewId = review.ReviewId,
                ReviewAssignmentId = review.ReviewAssignmentId,
                OverallScore = review.OverallScore,
                GeneralFeedback = review.GeneralFeedback,
                ReviewedAt = review.ReviewedAt,
                ReviewType = review.ReviewType,
                FeedbackSource = review.FeedbackSource,
                SubmissionId = reviewAssignment?.SubmissionId ?? 0,
                ReviewerName = reviewerUser?.FirstName ?? string.Empty,
                ReviewerEmail = reviewerUser?.Email ?? string.Empty,
                AssignmentTitle = assignment?.Title ?? string.Empty,
                CourseName = assignment?.CourseInstance?.Course?.CourseName ?? string.Empty,
                CriteriaFeedbacks = criteriaFeedbackResponses
            };
        }
    }
}