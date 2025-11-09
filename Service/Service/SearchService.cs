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