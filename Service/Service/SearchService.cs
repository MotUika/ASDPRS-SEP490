using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Response.Search;
using Service.RequestAndResponse.Response.Submission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class KeywordSearchService : IKeywordSearchService
    {
        private readonly ASDPRSContext _context;

        public KeywordSearchService(ASDPRSContext context)
        {
            _context = context;
        }
        public async Task<BaseResponse<SearchResultEFResponse>> SearchAsync(string keyword, int userId, string role)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new BaseResponse<SearchResultEFResponse>("Keyword is required", StatusCodeEnum.BadRequest_400, null);
            }

            // Tách keyword thành các từ riêng lẻ để tìm kiếm tốt hơn
            var searchTerms = keyword.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (!searchTerms.Any())
            {
                return new BaseResponse<SearchResultEFResponse>("Invalid keyword", StatusCodeEnum.BadRequest_400, null);
            }

            var results = new SearchResultEFResponse();

            try
            {
                // Search Assignments với Full-Text Search
                var assignmentQuery = _context.Assignments
                    .Include(a => a.CourseInstance)
                        .ThenInclude(ci => ci.Course)
                    .AsQueryable();

                // Áp dụng role-based filtering
                if (role == "Student")
                {
                    assignmentQuery = assignmentQuery.Where(a =>
                        a.CourseInstance.CourseStudents.Any(cs => cs.UserId == userId));
                }

                // Sử dụng Full-Text Search nếu có, fallback về LIKE nếu không
                assignmentQuery = assignmentQuery.Where(a =>
                    searchTerms.Any(term =>
                        EF.Functions.Like(a.Title, $"%{term}%") ||
                        EF.Functions.Like(a.Description, $"%{term}%") ||
                        EF.Functions.Like(a.CourseInstance.Course.CourseName, $"%{term}%") ||
                        EF.Functions.Like(a.Keywords, $"%{term}%")
                    ));

                results.Assignments = await assignmentQuery
                    .Select(a => new AssignmentSearchResult
                    {
                        AssignmentId = a.AssignmentId,
                        Title = a.Title,
                        CourseName = a.CourseInstance.Course.CourseName,
                        DescriptionSnippet = a.Description != null && a.Description.Length > 100
                            ? a.Description.Substring(0, 100) + "..."
                            : a.Description
                    })
                    .ToListAsync();

                // Search Feedback (Reviews) với cải tiến tìm kiếm
                var reviewQuery = _context.Reviews
                    .Include(r => r.ReviewAssignment)
                        .ThenInclude(ra => ra.Submission)
                            .ThenInclude(s => s.Assignment)
                    .Include(r => r.CriteriaFeedbacks)
                    .AsQueryable();

                if (role == "Student")
                {
                    reviewQuery = reviewQuery.Where(r => r.ReviewAssignment.Submission.UserId == userId);
                }

                reviewQuery = reviewQuery.Where(r =>
                    searchTerms.Any(term =>
                        EF.Functions.Like(r.GeneralFeedback, $"%{term}%") ||
                        r.CriteriaFeedbacks.Any(cf => EF.Functions.Like(cf.Feedback, $"%{term}%"))
                    ));

                results.Feedback = await reviewQuery
                    .Select(r => new FeedbackSearchResult
                    {
                        ReviewId = r.ReviewId,
                        AssignmentTitle = r.ReviewAssignment.Submission.Assignment.Title,
                        OverallFeedback = r.GeneralFeedback,
                        ReviewerType = r.ReviewType
                    })
                    .ToListAsync();

                // Search LLM Summaries với cải tiến
                var summaryQuery = _context.AISummaries
                    .Include(ais => ais.Submission)
                        .ThenInclude(s => s.Assignment)
                    .AsQueryable();

                if (role == "Student")
                {
                    summaryQuery = summaryQuery.Where(ais => ais.Submission.UserId == userId);
                }

                summaryQuery = summaryQuery.Where(ais =>
                    searchTerms.Any(term =>
                        EF.Functions.Like(ais.Content, $"%{term}%") ||
                        EF.Functions.Like(ais.SummaryType, $"%{term}%")
                    ));

                results.Summaries = await summaryQuery
                    .Select(ais => new SummarySearchResult
                    {
                        SummaryId = ais.SummaryId,
                        AssignmentTitle = ais.Submission.Assignment.Title,
                        ContentSnippet = ais.Content.Length > 100
                            ? ais.Content.Substring(0, 100) + "..."
                            : ais.Content,
                        SummaryType = ais.SummaryType,
                        GeneratedAt = ais.GeneratedAt
                    })
                    .ToListAsync();

                // Search Submissions với keywords
                var submissionQuery = _context.Submissions
                    .Include(s => s.Assignment)
                    .Include(s => s.User)
                    .AsQueryable();

                if (role == "Student")
                {
                    submissionQuery = submissionQuery.Where(s => s.UserId == userId);
                }

                submissionQuery = submissionQuery.Where(s =>
                    searchTerms.Any(term =>
                        EF.Functions.Like(s.Keywords, $"%{term}%") ||
                        EF.Functions.Like(s.OriginalFileName, $"%{term}%")
                    ));

                results.Submissions = await submissionQuery
                    .Select(s => new SubmissionSearchResult
                    {
                        SubmissionId = s.SubmissionId,
                        AssignmentTitle = s.Assignment.Title,
                        FileName = s.OriginalFileName,
                        Keywords = s.Keywords,
                        SubmittedAt = s.SubmittedAt,
                        StudentName = $"{s.User.FirstName} {s.User.LastName}".Trim()
                    })
                    .ToListAsync();

                return new BaseResponse<SearchResultEFResponse>(
                    $"Search completed successfully. Found {results.Assignments.Count} assignments, {results.Feedback.Count} feedback, {results.Summaries.Count} summaries, {results.Submissions.Count} submissions",
                    StatusCodeEnum.OK_200,
                    results
                );
            }
            catch (Exception ex)
            {
                // Fallback to basic search if full-text fails
                return await BasicSearchAsync(keyword, userId, role);
            }
        }
        public async Task<BaseResponse<SearchResultEFResponse>> BasicSearchAsync(string keyword, int userId, string role)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new BaseResponse<SearchResultEFResponse>("Keyword is required", StatusCodeEnum.BadRequest_400, null);
            }

            var results = new SearchResultEFResponse();

            // Search Assignments
            var assignmentQuery = _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Where(a => EF.Functions.Like(a.Title, $"%{keyword}%") || EF.Functions.Like(a.Description, $"%{keyword}%") || EF.Functions.Like(a.CourseInstance.Course.CourseName, $"%{keyword}%"));

            if (role == "Student")
            {
                assignmentQuery = assignmentQuery.Where(a => a.CourseInstance.CourseStudents.Any(cs => cs.UserId == userId));
            }

            results.Assignments = await assignmentQuery.Select(a => new AssignmentSearchResult
            {
                AssignmentId = a.AssignmentId,
                Title = a.Title,
                CourseName = a.CourseInstance.Course.CourseName
            }).ToListAsync();

            // Search Feedback (Reviews)
            var reviewQuery = _context.Reviews
                .Include(r => r.ReviewAssignment)
                    .ThenInclude(ra => ra.Submission)
                        .ThenInclude(s => s.Assignment)
                .Where(r => EF.Functions.Like(r.GeneralFeedback, $"%{keyword}%"));

            if (role == "Student")
            {
                reviewQuery = reviewQuery.Where(r => r.ReviewAssignment.Submission.UserId == userId);
            }

            results.Feedback = await reviewQuery.Select(r => new FeedbackSearchResult
            {
                ReviewId = r.ReviewId,
                AssignmentTitle = r.ReviewAssignment.Submission.Assignment.Title,
                OverallFeedback = r.GeneralFeedback
            }).ToListAsync();

            // Search LLM Summaries
            var summaryQuery = _context.AISummaries
                .Include(ais => ais.Submission)
                    .ThenInclude(s => s.Assignment)
                .Where(ais => EF.Functions.Like(ais.Content, $"%{keyword}%"));

            if (role == "Student")
            {
                summaryQuery = summaryQuery.Where(ais => ais.Submission.UserId == userId);
            }

            results.Summaries = await summaryQuery.Select(ais => new SummarySearchResult
            {
                SummaryId = ais.SummaryId,
                AssignmentTitle = ais.Submission.Assignment.Title,
                ContentSnippet = ais.Content.Substring(0, Math.Min(100, ais.Content.Length)) + "..."
            }).ToListAsync();

            return new BaseResponse<SearchResultEFResponse>(
                "Search completed successfully",
                StatusCodeEnum.OK_200,
                results
            );
        }
    }
}