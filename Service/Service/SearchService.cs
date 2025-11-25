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
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 3)
            {
                return new BaseResponse<SearchResultEFResponse>("Keyword must be at least 3 characters", StatusCodeEnum.BadRequest_400, null);
            }

            keyword = keyword.Trim();
            var searchTerms = keyword.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(term => $"\"{term}*\"")
                                     .ToList();

            if (!searchTerms.Any())
            {
                return new BaseResponse<SearchResultEFResponse>("Invalid keyword", StatusCodeEnum.BadRequest_400, null);
            }

            var results = new SearchResultEFResponse();
            try
            {
                // 1. Search Assignments
                var assignmentQuery = _context.Assignments
                    .Include(a => a.CourseInstance)
                        .ThenInclude(ci => ci.Course)
                    .AsQueryable();

                if (role == "Student")
                {
                    assignmentQuery = assignmentQuery.Where(a =>
                        a.CourseInstance.CourseStudents.Any(cs => cs.UserId == userId));
                }
                else if (role == "Instructor")
                {
                    assignmentQuery = assignmentQuery.Where(a =>
                        a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId));
                }

                assignmentQuery = assignmentQuery.Where(a =>
                    searchTerms.Any(term =>
                        Microsoft.EntityFrameworkCore.EF.Functions.Contains(a.Title, term) ||
                        Microsoft.EntityFrameworkCore.EF.Functions.Contains(a.Description, term) ||
                        Microsoft.EntityFrameworkCore.EF.Functions.Contains(a.CourseInstance.Course.CourseName, term)
                    ));

                results.Assignments = await assignmentQuery
                    .Select(a => new AssignmentSearchResult
                    {
                        AssignmentId = a.AssignmentId,
                        Title = a.Title,
                        CourseName = a.CourseInstance.Course.CourseName,
                        DescriptionSnippet = a.Description != null && a.Description.Length > 100
                            ? a.Description.Substring(0, 100) + "..."
                            : a.Description,
                        Type = "Assignment"
                    })
                    .ToListAsync();

                // 2. Search Feedback (Reviews)
                var reviewQuery = _context.Reviews
                    .Include(r => r.ReviewAssignment)
                        .ThenInclude(ra => ra.Submission)
                            .ThenInclude(s => s.Assignment)
                    .Include(r => r.CriteriaFeedbacks)
                    .AsQueryable();

                if (role == "Student")
                {
                    // Students can only search reviews THEY CREATED
                    reviewQuery = reviewQuery.Where(r => r.ReviewAssignment.ReviewerUserId == userId);
                }
                else if (role == "Instructor")
                {
                    // Instructors can search reviews in their courses
                    reviewQuery = reviewQuery.Where(r =>
                        r.ReviewAssignment.Submission.Assignment.CourseInstance.CourseInstructors
                            .Any(ci => ci.UserId == userId));
                }

                reviewQuery = reviewQuery.Where(r =>
                    searchTerms.Any(term =>
                        Microsoft.EntityFrameworkCore.EF.Functions.Contains(r.GeneralFeedback, term) ||
                        r.CriteriaFeedbacks.Any(cf => Microsoft.EntityFrameworkCore.EF.Functions.Contains(cf.Feedback, term))
                    ));

                results.Feedback = await reviewQuery
                    .Select(r => new FeedbackSearchResult
                    {
                        ReviewId = r.ReviewId,
                        AssignmentTitle = r.ReviewAssignment.Submission.Assignment.Title,
                        OverallFeedback = r.GeneralFeedback,
                        ReviewerType = r.ReviewType,
                        Type = "Feedback"
                    })
                    .ToListAsync();

                // 3. Search LLM Summaries (NOT for Students)
                if (role != "Student")
                {
                    var summaryQuery = _context.AISummaries
                        .Include(ais => ais.Submission)
                            .ThenInclude(s => s.Assignment)
                        .AsQueryable();

                    if (role == "Instructor")
                    {
                        summaryQuery = summaryQuery.Where(ais =>
                            ais.Submission.Assignment.CourseInstance.CourseInstructors
                                .Any(ci => ci.UserId == userId));
                    }

                    summaryQuery = summaryQuery.Where(ais =>
                        searchTerms.Any(term =>
                            Microsoft.EntityFrameworkCore.EF.Functions.Contains(ais.Content, term) ||
                            Microsoft.EntityFrameworkCore.EF.Functions.Contains(ais.SummaryType, term)
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
                            GeneratedAt = ais.GeneratedAt,
                            Type = "Summary"
                        })
                        .ToListAsync();
                }

                // 4. Search Submissions
                var submissionQuery = _context.Submissions
                    .Include(s => s.Assignment)
                    .Include(s => s.User)
                    .AsQueryable();

                if (role == "Student")
                {
                    submissionQuery = submissionQuery.Where(s => s.UserId == userId);
                }
                else if (role == "Instructor")
                {
                    submissionQuery = submissionQuery.Where(s =>
                        s.Assignment.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId));
                }

                submissionQuery = submissionQuery.Where(s =>
                    searchTerms.Any(term =>
                        Microsoft.EntityFrameworkCore.EF.Functions.Contains(s.Keywords, term) ||
                        Microsoft.EntityFrameworkCore.EF.Functions.Contains(s.OriginalFileName, term)
                    ));

                results.Submissions = await submissionQuery
                    .Select(s => new SubmissionSearchResult
                    {
                        SubmissionId = s.SubmissionId,
                        AssignmentTitle = s.Assignment.Title,
                        FileName = s.OriginalFileName,
                        Keywords = s.Keywords,
                        SubmittedAt = s.SubmittedAt,
                        StudentName = $"{s.User.FirstName} {s.User.LastName}".Trim(),
                        Type = "Submission"
                    })
                    .ToListAsync();

                // 5. Search Rubric Criteria (Only for Instructors/Admin)
                if (role != "Student")
                {
                    var criteriaQuery = _context.Criteria
                        .Include(c => c.Rubric)
                            .ThenInclude(r => r.Assignment)
                                .ThenInclude(a => a.CourseInstance)
                                    .ThenInclude(ci => ci.Course)
                        .AsQueryable();

                    if (role == "Instructor")
                    {
                        criteriaQuery = criteriaQuery.Where(c =>
                            c.Rubric.Assignment.CourseInstance.CourseInstructors
                                .Any(ci => ci.UserId == userId));
                    }

                    criteriaQuery = criteriaQuery.Where(c =>
                        searchTerms.Any(term =>
                            Microsoft.EntityFrameworkCore.EF.Functions.Contains(c.Title, term) ||
                            Microsoft.EntityFrameworkCore.EF.Functions.Contains(c.Description, term)
                        ));

                    results.Criteria = await criteriaQuery
                        .Select(c => new CriteriaSearchResult
                        {
                            CriteriaId = c.CriteriaId,
                            Title = c.Title,
                            Description = c.Description,
                            RubricTitle = c.Rubric.Title,
                            AssignmentTitle = c.Rubric.Assignment.Title,
                            CourseName = c.Rubric.Assignment.CourseInstance.Course.CourseName,
                            MaxScore = c.MaxScore,
                            Weight = c.Weight,
                            Type = "Criteria"
                        })
                        .ToListAsync();
                }

                return new BaseResponse<SearchResultEFResponse>(
                    $"Search completed successfully. Found {results.Assignments.Count} assignments, {results.Feedback.Count} feedback, {results.Summaries.Count} summaries, {results.Submissions.Count} submissions, {results.Criteria.Count} criteria",
                    StatusCodeEnum.OK_200,
                    results
                );
            }
            catch (Exception ex)
            {
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
                CourseName = a.CourseInstance.Course.CourseName,
                Type = "Assignment"
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
                OverallFeedback = r.GeneralFeedback,
                Type = "Feedback"
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
                ContentSnippet = ais.Content.Substring(0, Math.Min(100, ais.Content.Length)) + "...",
                Type = "Summary"
            }).ToListAsync();

            return new BaseResponse<SearchResultEFResponse>(
                "Search completed successfully",
                StatusCodeEnum.OK_200,
                results
            );
        }
    }
}