using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Response.Statistic;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class StatisticsService : IStatisticsService
{
    private readonly ASDPRSContext _context;

    public StatisticsService(ASDPRSContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<IEnumerable<AssignmentStatisticResponse>>> GetAssignmentStatisticsByClassAsync(int userId, int courseInstanceId)
    {
        // 1. Lấy tất cả assignment của course instance
        var assignments = await _context.Assignments
            .Where(a => a.CourseInstanceId == courseInstanceId &&
                        a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId))
            .Include(a => a.Submissions)
            .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.CourseStudents)
                    .ThenInclude(cs => cs.User)
            .ToListAsync();

        var result = new List<AssignmentStatisticResponse>();

        // 2. Tính trạng thái pass/fail cho tất cả sinh viên và tất cả assignment trước
        var studentPassStatus = new Dictionary<int, Dictionary<int, bool>>(); // [UserId][AssignmentId] = isPass

        foreach (var a in assignments)
        {
            foreach (var stu in a.CourseInstance.CourseStudents)
            {
                var submission = a.Submissions.FirstOrDefault(s => s.UserId == stu.UserId && s.FinalScore.HasValue);
                bool isPass = false;

                if (submission != null)
                {
                    if (a.GradingScale == "Scale10") isPass = submission.FinalScore > 0;
                    else if (a.GradingScale == "PassFail") isPass = submission.FinalScore >= a.PassThreshold;
                }

                if (!studentPassStatus.ContainsKey(stu.UserId))
                    studentPassStatus[stu.UserId] = new Dictionary<int, bool>();

                studentPassStatus[stu.UserId][a.AssignmentId] = isPass;
            }
        }

        foreach (var a in assignments)
        {
            int totalStudents = a.CourseInstance.CourseStudents.Count;
            int totalSubmissions = a.Submissions.Count;
            int gradedCount = a.Submissions.Count(s => s.FinalScore.HasValue);

            var gradedSubmissions = a.Submissions.Where(s => s.FinalScore.HasValue).ToList();

            decimal submissionRate = totalStudents > 0 ? (decimal)totalSubmissions / totalStudents * 100 : 0;
            decimal gradedRate = totalSubmissions > 0 ? (decimal)gradedCount / totalSubmissions * 100 : 0;

            int passCount = gradedSubmissions.Count(s =>
                (a.GradingScale == "Scale10" && s.FinalScore > 0) ||
                (a.GradingScale == "PassFail" && s.FinalScore >= a.PassThreshold)
            );

            int failCount = gradedSubmissions.Count(s =>
                (a.GradingScale == "Scale10" && (s.FinalScore ?? 0) <= 0) ||
                (a.GradingScale == "PassFail" && s.FinalScore < a.PassThreshold)
            );

            int failStudentCount = a.CourseInstance.CourseStudents.Count(stu =>
                !studentPassStatus.TryGetValue(stu.UserId, out var status) ||
                !status.TryGetValue(a.AssignmentId, out var isPass) ||
                !isPass
            );

            // Tạo distribution
            var distribution = gradedSubmissions
                .GroupBy(s => (int)s.FinalScore)
                .Select(g => new DistributionItem
                {
                    Range = $"{g.Key} - {g.Key + 1}",
                    Count = g.Count()
                }).ToList();

            // Tạo danh sách chi tiết sinh viên
            var studentAverages = a.CourseInstance.CourseStudents
                .Select(stu =>
                {
                    var submission = a.Submissions.FirstOrDefault(s => s.UserId == stu.UserId);
                    return new StudentAverageScoreResponse
                    {
                        UserId = stu.UserId,
                        StudentName = stu.User.UserName,
                        StudentCode = stu.User.StudentCode,
                        AverageScore = submission?.FinalScore,
                        AssignmentScores = new List<StudentAssignmentScore>
                        {
                        new StudentAssignmentScore
                        {
                            AssignmentId = a.AssignmentId,
                            AssignmentTitle = a.Title,
                            Score = submission?.FinalScore
                        }
                        }
                    };
                }).ToList();

            // Tính PassAllAssignmentsCount / FailAllAssignmentsCount dựa trên studentPassStatus
            int passAllAssignmentsCount = studentPassStatus.Count(stu =>
                stu.Value.All(kv => kv.Value)
            );
            int failAllAssignmentsCount = studentPassStatus.Count - passAllAssignmentsCount;

            result.Add(new AssignmentStatisticResponse
            {
                AssignmentId = a.AssignmentId,
                AssignmentTitle = a.Title,
                CourseInstanceId = a.CourseInstanceId,
                SectionCode = a.CourseInstance.SectionCode,

                TotalStudents = totalStudents,
                TotalSubmissions = totalSubmissions,
                GradedCount = gradedCount,
                SubmissionRate = submissionRate,
                GradedRate = gradedRate,

                AverageScore = gradedSubmissions.Any() ? gradedSubmissions.Average(s => s.FinalScore) : (decimal?)null,
                MinScore = gradedSubmissions.Any() ? gradedSubmissions.Min(s => s.FinalScore) : 0,
                MaxScore = gradedSubmissions.Any() ? gradedSubmissions.Max(s => s.FinalScore) : 0,

                PassCount = passCount,
                FailCount = failCount,
                FailStudentCount = failStudentCount,
                PassAllAssignmentsCount = passAllAssignmentsCount,
                FailAllAssignmentsCount = failAllAssignmentsCount,

                Distribution = distribution,
                StudentAverages = studentAverages
            });
        }

        return new BaseResponse<IEnumerable<AssignmentStatisticResponse>>(
            "Thống kê assignment thành công",
            StatusCodeEnum.OK_200,
            result
        );
    }


    public async Task<BaseResponse<IEnumerable<AssignmentOverviewResponse>>>
 GetAssignmentOverviewAsync(int userId, int courseInstanceId)
    {
        var assignments = await _context.Assignments
            .Where(a => a.CourseInstanceId == courseInstanceId &&
                        a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId))
            .Include(a => a.Submissions)
            .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.CourseStudents)
            .ToListAsync();

        // --- Khởi tạo tổng ---
        int draftCount = 0;
        int upcomingCount = 0;
        int activeCount = 0;
        int inReviewCount = 0;
        int closedCount = 0;
        int gradesPublishedCount = 0;
        int archivedCount = 0;
        int cancelledCount = 0;

        int totalPassCount = 0;
        int totalFailCount = 0;

        foreach (var a in assignments)
        {
            var submissions = a.Submissions;
            var graded = submissions.Where(s => s.FinalScore.HasValue).ToList();

            int passCount = graded.Count(s =>
                (a.GradingScale == "Scale10" && s.FinalScore > 0) ||
                (a.GradingScale == "PassFail" && s.FinalScore >= a.PassThreshold));

            int failCount = graded.Count - passCount;

            totalPassCount += passCount;
            totalFailCount += failCount;

            switch (a.Status)
            {
                case "Draft": draftCount++; break;
                case "Upcoming": upcomingCount++; break;
                case "Active": activeCount++; break;
                case "InReview": inReviewCount++; break;
                case "Closed": closedCount++; break;
                case "GradesPublished": gradesPublishedCount++; break;
                case "Archived": archivedCount++; break;
                case "Cancelled": cancelledCount++; break;
            }
        }

        // Chỉ trả về đúng 1 record tổng
        var list = new List<AssignmentOverviewResponse>
    {
        new AssignmentOverviewResponse
        {
            AssignmentId = 0,
            AssignmentTitle = "Total Assignments",

            DraftCount = draftCount,
            UpcomingCount = upcomingCount,
            ActiveCount = activeCount,
            InReviewCount = inReviewCount,
            ClosedCount = closedCount,
            GradesPublishedCount = gradesPublishedCount,
            ArchivedCount = archivedCount,
            CancelledCount = cancelledCount,

            PassCount = totalPassCount,
            FailCount = totalFailCount
        }
    };

        return new BaseResponse<IEnumerable<AssignmentOverviewResponse>>(
            "Thống kê assignment thành công",
            StatusCodeEnum.OK_200,
            list
        );
    }


    public async Task<BaseResponse<IEnumerable<AssignmentSubmissionDetailResponse>>>
 GetSubmissionDetailsAsync(int userId, int courseInstanceId)
    {
        var assignments = await _context.Assignments
            .Where(a => a.CourseInstanceId == courseInstanceId &&
                        a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId))
            .Include(a => a.Submissions)
            .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.CourseStudents)
            .ToListAsync();

        int totalSubmitted = 0;
        int totalNotSubmitted = 0;
        int totalGraded = 0;

        foreach (var a in assignments)
        {
            var studentsInClass = a.CourseInstance.CourseStudents;

            int submittedCount = a.Submissions.Count(s =>
                s.Status == "Submitted" ||
                (s.Status == "Graded" && (s.FinalScore ?? 0) > 0)
            );

            int gradedCount = a.Submissions.Count(s => s.Status == "Graded");
            int notSubmittedCount = studentsInClass.Count - submittedCount;

            totalSubmitted += submittedCount;
            totalNotSubmitted += notSubmittedCount;
            totalGraded += gradedCount;
        }

        // ➜ Chỉ trả về đúng 1 object TOTAL
        var totalResponse = new AssignmentSubmissionDetailResponse
        {
            AssignmentId = 0,
            AssignmentTitle = "Total Assignment",
            TotalAssignment = assignments.Count,
            TotalSubmittedCount = totalSubmitted,
            TotalNotSubmittedCount = totalNotSubmitted,
            TotalGradedCount = totalGraded,

            // Không cần chi tiết từng assignment
            Submissions = new List<SubmissionStatisticResponse>(),
            SubmittedCount = 0,
            NotSubmittedCount = 0,
            GradedCount = 0
        };

        return new BaseResponse<IEnumerable<AssignmentSubmissionDetailResponse>>(
            "Lấy thống kê submission thành công",
            StatusCodeEnum.OK_200,
            new List<AssignmentSubmissionDetailResponse> { totalResponse }
        );
    }



    public async Task<BaseResponse<IEnumerable<AssignmentDistributionResponse>>>
GetAssignmentDistributionAsync(int userId, int courseInstanceId)
    {
        var assignments = await _context.Assignments
            .Where(a => a.CourseInstanceId == courseInstanceId &&
                        a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId))
            .Include(a => a.Submissions)
            .ToListAsync();

        var allScores = assignments
            .SelectMany(a => a.Submissions
                .Where(s => s.FinalScore.HasValue)
                .Select(s => (decimal)s.FinalScore))
            .ToList();

        var totalDistribution = allScores
            .GroupBy(score => (int)score)
            .Select(g => new DistributionItem
            {
                Range = $"{g.Key} - {g.Key + 1}",
                Count = g.Count()
            })
            .OrderBy(d => d.Range)
            .ToList();

        var total = new AssignmentDistributionResponse
        {
            AssignmentId = 0,
            AssignmentTitle = "Total Assignment",
            Distribution = totalDistribution,
            IsTotal = true,
            TotalAssignmentCount = assignments.Count
        };

        return new BaseResponse<IEnumerable<AssignmentDistributionResponse>>(
            "Thống kê phân phối điểm thành công",
            StatusCodeEnum.OK_200,
            new List<AssignmentDistributionResponse> { total } // :point_left: chỉ 1 item
        );
    }





    public async Task<BaseResponse<IEnumerable<ClassStatisticResponse>>> GetClassStatisticsByCourseAsync(int userId, int courseId)
    {
        var courseInstances = await _context.CourseInstances
            .Where(ci => ci.CourseId == courseId &&
                         ci.CourseInstructors.Any(i => i.UserId == userId))
            .Include(ci => ci.Assignments)
                .ThenInclude(a => a.Submissions)
            .Include(ci => ci.CourseStudents)
                .ThenInclude(cs => cs.User)
            .ToListAsync();

        var result = new List<ClassStatisticResponse>();

        foreach (var ci in courseInstances)
        {
            int totalStudents = ci.CourseStudents.Count;
            int totalAssignments = ci.Assignments.Count;
            int expectedSubmissions = totalStudents * totalAssignments;

            // Lấy tất cả submission trong lớp
            var submissions = ci.Assignments.SelectMany(a => a.Submissions).ToList();
            int totalSubmissions = submissions.Count;
            int gradedCount = submissions.Count(s => s.FinalScore.HasValue);

            var gradedScores = submissions.Where(s => s.FinalScore.HasValue).Select(s => (decimal)s.FinalScore).ToList();

            decimal submissionRate = expectedSubmissions > 0 ? (decimal)totalSubmissions / expectedSubmissions * 100 : 0;
            decimal gradedRate = expectedSubmissions > 0 ? (decimal)gradedCount / expectedSubmissions * 100 : 0;

            // Distribution
            var distribution = gradedScores
                .GroupBy(s => (int)s)
                .Select(g => new DistributionItem
                {
                    Range = $"{g.Key} - {g.Key + 1}",
                    Count = g.Count()
                })
                .ToList();

            // ============================================================
            // Logic PASS/FAIL chuẩn
            // ============================================================

            bool IsPass(Submission s)
            {
                if (!s.FinalScore.HasValue) return false;
                if (s.Assignment.GradingScale == "Scale10") return s.FinalScore > 0;
                if (s.Assignment.GradingScale == "PassFail") return s.FinalScore >= s.Assignment.PassThreshold;
                return false;
            }

            int passCount = submissions.Count(s => IsPass(s));
            int failCount = submissions.Count(s => !IsPass(s));

            // Nhóm submissions theo sinh viên
            var studentGroups = ci.CourseStudents
                .Select(stu => new
                {
                    stu.UserId,
                    Submissions = ci.Assignments
                        .Select(a => a.Submissions.FirstOrDefault(s => s.UserId == stu.UserId))
                        .Where(s => s != null)
                        .ToList()
                })
                .ToList();

            int failStudentCount = studentGroups.Count(g => g.Submissions.All(s => !IsPass(s)));

            int passAllAssignmentsCount = studentGroups.Count(g => g.Submissions.All(s => IsPass(s)));
            int failAllAssignmentsCount = studentGroups.Count(g => g.Submissions.Any(s => !IsPass(s)));

            // ============================================================
            // Student Averages
            // ============================================================

            var studentAverages = ci.CourseStudents.Select(stu =>
            {
                var stuScores = ci.Assignments
                    .Select(a => a.Submissions.FirstOrDefault(s => s.UserId == stu.UserId))
                    .Where(s => s?.FinalScore.HasValue ?? false)
                    .Select(s => (decimal)s!.FinalScore!)
                    .ToList();

                return new StudentAverageScoreResponse
                {
                    UserId = stu.UserId,
                    StudentName = stu.User.UserName,
                    StudentCode = stu.User.StudentCode,
                    AverageScore = stuScores.Any() ? stuScores.Average() : (decimal?)null,
                    AssignmentScores = ci.Assignments.Select(a =>
                    {
                        var sub = a.Submissions.FirstOrDefault(s => s.UserId == stu.UserId);
                        return new StudentAssignmentScore
                        {
                            AssignmentId = a.AssignmentId,
                            AssignmentTitle = a.Title,
                            Score = sub?.FinalScore
                        };
                    }).ToList()
                };
            }).ToList();

            // ============================================================
            // Assignment Averages
            // ============================================================

            var assignmentAverages = ci.Assignments.Select(a =>
            {
                var aScores = a.Submissions
                    .Where(s => s.FinalScore.HasValue)
                    .Select(s => (decimal)s.FinalScore)
                    .ToList();

                return new AssignmentAverageScoreResponse
                {
                    AssignmentId = a.AssignmentId,
                    AssignmentTitle = a.Title,
                    AverageScore = aScores.Any() ? aScores.Average() : (decimal?)null
                };
            }).ToList();

            // ============================================================
            // Normalized Scores (thang 10 -> 100)
            // ============================================================

            var normalizedScores = ci.Assignments.Select(a =>
            {
                var nScores = a.Submissions
                    .Where(s => s.FinalScore.HasValue)
                    .Select(s => (decimal)s.FinalScore / 10 * 100)
                    .ToList();

                return new NormalizedScoreResponse
                {
                    AssignmentId = a.AssignmentId,
                    AssignmentTitle = a.Title,
                    AverageNormalizedScore = nScores.Any() ? nScores.Average() : (decimal?)null
                };
            }).ToList();

            // ============================================================
            // Kết quả cuối cùng
            // ============================================================

            result.Add(new ClassStatisticResponse
            {
                CourseInstanceId = ci.CourseInstanceId,
                SectionCode = ci.SectionCode,
                TotalStudents = totalStudents,
                TotalAssignments = totalAssignments,
                ExpectedSubmissions = expectedSubmissions,
                TotalSubmissions = totalSubmissions,
                GradedCount = gradedCount,
                SubmissionRate = submissionRate,
                GradedRate = gradedRate,
                AverageScore = gradedScores.Any() ? gradedScores.Average() : (decimal?)null,
                MinScore = gradedScores.Any() ? gradedScores.Min() : (decimal?)null,
                MaxScore = gradedScores.Any() ? gradedScores.Max() : (decimal?)null,
                PassCount = passCount,
                FailCount = failCount,
                FailStudentCount = failStudentCount,
                PassAllAssignmentsCount = passAllAssignmentsCount,
                FailAllAssignmentsCount = failAllAssignmentsCount,
                Distribution = distribution,
                StudentAverages = studentAverages,
                AssignmentAverages = assignmentAverages,
                NormalizedScores = normalizedScores
            });
        }

        return new BaseResponse<IEnumerable<ClassStatisticResponse>>(
            "Thống kê lớp thành công",
            StatusCodeEnum.OK_200,
            result
        );
    }




}


       
    



