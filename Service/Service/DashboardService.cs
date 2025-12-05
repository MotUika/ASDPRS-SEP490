using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BussinessObject.Models;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Response.Dashboard;

namespace Service.Service
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ISemesterRepository _semesterRepository;

        public DashboardService(IDashboardRepository dashboardRepository, ISemesterRepository semesterRepository)
        {
            _dashboardRepository = dashboardRepository;
            _semesterRepository = semesterRepository;
        }

        public async Task<BaseResponse<SemesterStatisticResponse>> GetSemesterStatisticsAsync(int academicYearId, int semesterId)
        {
            try
            {
                // 1. Validate Semester thuộc AcademicYear
                var semester = await _semesterRepository.GetByIdAsync(semesterId);
                if (semester == null)
                    return new BaseResponse<SemesterStatisticResponse>("Semester not found", StatusCodeEnum.NotFound_404, null);

                if (semester.AcademicYearId != academicYearId)
                    return new BaseResponse<SemesterStatisticResponse>("Semester does not belong to the provided Academic Year", StatusCodeEnum.BadRequest_400, null);

                var response = new SemesterStatisticResponse();

                // 2. User Statistics
                response.TotalStudents = await _dashboardRepository.GetTotalStudentsBySemesterAsync(semesterId);
                response.TotalInstructors = await _dashboardRepository.GetTotalInstructorsBySemesterAsync(semesterId);

                // 3. Assignment Status Overview
                var statusCounts = await _dashboardRepository.GetAssignmentStatusCountsAsync(semesterId);
                response.TotalActiveAssignments = statusCounts.GetValueOrDefault("Active", 0);
                response.TotalInReviewAssignments = statusCounts.GetValueOrDefault("InReview", 0);
                response.TotalClosedAssignments = statusCounts.GetValueOrDefault("Closed", 0) + statusCounts.GetValueOrDefault("GradesPublished", 0);

                // 4. Rubric & Criteria Overview
                var (rubricCount, criteriaCount) = await _dashboardRepository.GetRubricAndCriteriaCountsAsync(semesterId);
                response.TotalRubricsUsed = rubricCount;
                response.TotalCriteriaUsed = criteriaCount;

                // 5. Calculate Submission Rates & Top Lowest
                var assignments = await _dashboardRepository.GetAssignmentsWithSubmissionsBySemesterAsync(semesterId);

                int totalExpected = await _dashboardRepository.GetTotalExpectedSubmissionsAsync(semesterId);
                int totalSubmissions = 0;
                int totalGraded = 0;
                int totalSubmittedNotGraded = 0;

                var assignmentRates = new List<LowSubmissionAssignmentResponse>();

                foreach (var asm in assignments)
                {
                    int enrolledCount = asm.CourseInstance.CourseStudents.Count(cs => cs.Status == "Enrolled");
                    int submitCount = asm.Submissions.Count;

                    decimal rate = enrolledCount > 0 ? (decimal)submitCount / enrolledCount * 100 : 0;

                    assignmentRates.Add(new LowSubmissionAssignmentResponse
                    {
                        AssignmentTitle = asm.Title,
                        CourseName = asm.CourseInstance.Course?.CourseName,
                        ClassName = asm.CourseInstance.SectionCode,
                        SubmissionRate = Math.Round(rate, 2)
                    });

                    // Stats for Semester Aggregate
                    var graded = asm.Submissions.Count(s => s.Status == "Graded" || s.Status == "GradesPublished");
                    var submitted = submitCount - graded; // Submitted, Late...

                    totalGraded += graded;
                    totalSubmittedNotGraded += submitted;
                    totalSubmissions += submitCount;
                }

                // 5.1 Top 3 Lowest
                response.LowestSubmissionAssignments = assignmentRates
                    .OrderBy(x => x.SubmissionRate)
                    .Take(3)
                    .ToList();

                // 5.2 Submission Rate Distribution
                int notSubmittedCount = Math.Max(0, totalExpected - totalSubmissions);

                decimal baseTotal = totalExpected > 0 ? totalExpected : 1;

                response.SubmissionRate = new SubmissionRateResponse
                {
                    NotSubmitted = new StatisticItem
                    {
                        Count = notSubmittedCount,
                        Percentage = Math.Round((decimal)notSubmittedCount / baseTotal * 100, 2)
                    },
                    Submitted = new StatisticItem
                    {
                        Count = totalSubmittedNotGraded,
                        Percentage = Math.Round((decimal)totalSubmittedNotGraded / baseTotal * 100, 2)
                    },
                    Graded = new StatisticItem
                    {
                        Count = totalGraded,
                        Percentage = Math.Round((decimal)totalGraded / baseTotal * 100, 2)
                    }
                };

                // 6. Score Distribution (0-1, 1-2, ..., 9-10)
                var scores = await _dashboardRepository.GetFinalScoresBySemesterAsync(semesterId);
                var scoreDistribution = new List<ScoreDistributionResponse>();
                int totalScores = scores.Count;
                decimal scoreBase = totalScores > 0 ? totalScores : 1;

                // Create ranges: 0-1, 1-2 ... 9-10
                for (int i = 0; i < 10; i++)
                {
                    int min = i;
                    int max = i + 1;
                    int count = 0;

                    if (i == 9) // Range 9-10 (inclusive 10)
                    {
                        count = scores.Count(s => s >= min && s <= max);
                    }
                    else // Range [min, max)
                    {
                        count = scores.Count(s => s >= min && s < max);
                    }

                    scoreDistribution.Add(new ScoreDistributionResponse
                    {
                        RangeLabel = $"{min}-{max}",
                        Count = count,
                        Percentage = Math.Round((decimal)count / scoreBase * 100, 2)
                    });
                }
                response.ScoreDistribution = scoreDistribution;

                return new BaseResponse<SemesterStatisticResponse>(
                    "Semester statistics retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<SemesterStatisticResponse>(
                    $"Error fetching statistics: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
    }
}

