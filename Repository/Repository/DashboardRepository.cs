using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;

namespace Repository.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ASDPRSContext _context;

        public DashboardRepository(ASDPRSContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalStudentsBySemesterAsync(int semesterId)
        {
            return await _context.CourseStudents
                .Where(cs => cs.CourseInstance.SemesterId == semesterId && cs.Status == "Enrolled")
                .Select(cs => cs.UserId)
                .Distinct()
                .CountAsync();
        }

        public async Task<int> GetTotalInstructorsBySemesterAsync(int semesterId)
        {
            return await _context.CourseInstructors
                .Where(ci => ci.CourseInstance.SemesterId == semesterId)
                .Select(ci => ci.UserId)
                .Distinct()
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetAssignmentStatusCountsAsync(int semesterId)
        {
            var statusCounts = await _context.Assignments
                .Where(a => a.CourseInstance.SemesterId == semesterId)
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            return statusCounts;
        }

        public async Task<(int rubrics, int criteria)> GetRubricAndCriteriaCountsAsync(int semesterId)
        {
            var assignmentsInSemester = _context.Assignments
                .Where(a => a.CourseInstance.SemesterId == semesterId && a.RubricId != null);

            var rubricCount = await assignmentsInSemester
                .Select(a => a.RubricId)
                .Distinct()
                .CountAsync();

            var criteriaCount = await _context.Criteria
                .Where(c => c.Rubric.Assignment.CourseInstance.SemesterId == semesterId)
                .CountAsync();

            return (rubricCount, criteriaCount);
        }

        public async Task<List<Assignment>> GetAssignmentsWithSubmissionsBySemesterAsync(int semesterId)
        {
            return await _context.Assignments
                .Where(a => a.CourseInstance.SemesterId == semesterId && a.Status != "Draft" && a.Status != "Cancelled")
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.Submissions)
                .Include(a => a.CourseInstance.CourseStudents)
                .ToListAsync();
        }

        public async Task<List<decimal>> GetFinalScoresBySemesterAsync(int semesterId)
        {
            return await _context.Submissions
                .Where(s => s.Assignment.CourseInstance.SemesterId == semesterId
                            && (s.Status == "Graded" || s.Status == "GradesPublished")
                            && s.FinalScore.HasValue)
                .Select(s => s.FinalScore.Value)
                .ToListAsync();
        }

        public async Task<int> GetTotalExpectedSubmissionsAsync(int semesterId)
        {
            var query = from a in _context.Assignments
                        join cs in _context.CourseStudents on a.CourseInstanceId equals cs.CourseInstanceId
                        where a.CourseInstance.SemesterId == semesterId
                              && a.Status != "Draft"
                              && a.Status != "Cancelled"
                              && cs.Status == "Enrolled"
                        select 1;

            return await query.CountAsync();
        }
    }
}

