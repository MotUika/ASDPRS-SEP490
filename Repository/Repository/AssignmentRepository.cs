using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repository.Repository
{
    public class AssignmentRepository : BaseRepository<Assignment>, IAssignmentRepository
    {
        private readonly ASDPRSContext _context;

        public AssignmentRepository(BaseDAO<Assignment> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        // *Lấy danh sách assignment theo course instance với ràng buộc timeline
        public async Task<IEnumerable<Assignment>> GetActiveAssignmentsByCourseInstanceAsync(int courseInstanceId)
        {
            var now = DateTime.UtcNow.AddHours(7);
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Include(a => a.Rubric)
                .Where(a => a.CourseInstanceId == courseInstanceId &&
                           a.Status != "Draft" &&
                           a.Status != "Archived" &&
                           (a.StartDate == null || a.StartDate <= now) &&
                           (a.FinalDeadline == null || now <= a.FinalDeadline))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // *Lấy assignment với đầy đủ thông tin clone
        public async Task<Assignment> GetAssignmentWithCloneInfoAsync(int assignmentId)
        {
            return await _context.Assignments
                .Include(a => a.Rubric)
                    .ThenInclude(r => r.Criteria)
                    .ThenInclude(c => c.CriteriaTemplate)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Include(a => a.ClonedFromAssignment) // Thông tin assignment gốc nếu là clone
                .Include(a => a.ClonedAssignments) // Các assignment được clone từ assignment này
                    .ThenInclude(ca => ca.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
        }

        // *Lấy assignments theo trạng thái
        public async Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(int courseInstanceId, string status)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Include(a => a.Rubric)
                .Where(a => a.CourseInstanceId == courseInstanceId && a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // *Lấy ID của các assignment đang active (sử dụng HashSet cho performance)
        public async Task<HashSet<int>> GetActiveAssignmentIdsAsync(int courseInstanceId)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var activeAssignmentIds = await _context.Assignments
                .Where(a => a.CourseInstanceId == courseInstanceId &&
                           a.Status != "Draft" &&
                           a.Status != "Archived" &&
                           (a.StartDate == null || a.StartDate <= now) &&
                           (a.FinalDeadline == null || now <= a.FinalDeadline))
                .Select(a => a.AssignmentId)
                .ToListAsync();

            return activeAssignmentIds.ToHashSet();
        }

        // *Lấy assignments sắp đến hạn (cho notification)
        public async Task<IEnumerable<Assignment>> GetUpcomingDeadlineAssignmentsAsync(int daysBefore = 1)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var targetDate = now.AddDays(daysBefore);

            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Where(a => a.Status == "Active" &&
                           a.Deadline >= now &&
                           a.Deadline <= targetDate)
                .OrderBy(a => a.Deadline)
                .ToListAsync();
        }

        // *Kiểm tra xem assignment có thể clone được không
        public async Task<bool> CanCloneAssignmentAsync(int sourceAssignmentId, int targetCourseInstanceId)
        {
            var sourceAssignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.AssignmentId == sourceAssignmentId);

            var targetCourseInstance = await _context.CourseInstances
                .FirstOrDefaultAsync(ci => ci.CourseInstanceId == targetCourseInstanceId);

            return sourceAssignment != null &&
                   targetCourseInstance != null &&
                   sourceAssignment.CourseInstanceId != targetCourseInstanceId;
        }

        // *Lấy các assignment đã được clone từ assignment gốc
        public async Task<IEnumerable<Assignment>> GetClonedAssignmentsAsync(int originalAssignmentId)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Where(a => a.ClonedFromAssignmentId == originalAssignmentId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        //*Thêm filter theo timeline
        public async Task<IEnumerable<Assignment>> GetByCourseInstanceIdAsync(int courseInstanceId)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Semester)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                .Include(a => a.Rubric)
                    .ThenInclude(r => r.Criteria)
                .Where(a => a.CourseInstanceId == courseInstanceId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        //*Thêm thông tin clone
        public async Task<Assignment> GetAssignmentWithRubricAsync(int assignmentId)
        {
            return await _context.Assignments
                .Include(a => a.Rubric)
                    .ThenInclude(r => r.Criteria)
                    .ThenInclude(c => c.CriteriaTemplate)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Include(a => a.ClonedFromAssignment) // Thêm thông tin clone
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
        }

        //*Thêm filter theo timeline cho instructor
        public async Task<IEnumerable<Assignment>> GetAssignmentsByInstructorAsync(int instructorId, bool includeDrafts = false)
        {
            var query = _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Where(a => a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == instructorId));

            if (!includeDrafts)
            {
                query = query.Where(a => a.Status != "Draft");
            }

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        //*Chỉ lấy assignments active cho student
        public async Task<IEnumerable<Assignment>> GetAssignmentsByStudentAsync(int studentId)
        {
            var now = DateTime.UtcNow.AddHours(7);
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Where(a => a.CourseInstance.CourseStudents.Any(cs => cs.UserId == studentId) &&
                           a.Status != "Draft" &&
                           a.Status != "Archived" &&
                           (a.StartDate == null || a.StartDate <= now) &&
                           (a.FinalDeadline == null || now <= a.FinalDeadline))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        //*Sử dụng trạng thái mới và timeline 3 mốc
        public async Task<IEnumerable<Assignment>> GetActiveAssignmentsAsync()
        {
            var now = DateTime.UtcNow.AddHours(7);
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Where(a => a.Status == "Active" ||
                           (a.Status == "LateSubmission" && a.FinalDeadline >= now))
                .OrderBy(a => a.Deadline)
                .ToListAsync();
        }

        //*Sử dụng trạng thái mới
        public async Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync()
        {
            var now = DateTime.UtcNow.AddHours(7);
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Where(a => a.Status == "Closed" ||
                           (a.FinalDeadline.HasValue && a.FinalDeadline < now))
                .OrderByDescending(a => a.Deadline)
                .ToListAsync();
        }

        // *Cập nhật trạng thái assignment dựa trên timeline
        public async Task UpdateAssignmentStatusBasedOnTimelineAsync()
        {
            var now = DateTime.UtcNow.AddHours(7);
            var assignments = await _context.Assignments
                .Where(a => a.Status != "Draft" && a.Status != "Archived")
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                var newStatus = CalculateAssignmentStatus(assignment, now);
                if (assignment.Status != newStatus)
                {
                    assignment.Status = newStatus;
                }
            }

            await _context.SaveChangesAsync();
        }

        // Helper method để tính toán trạng thái
        private string CalculateAssignmentStatus(Assignment assignment, DateTime now)
        {
            if (assignment.StartDate.HasValue && now < assignment.StartDate.Value)
                return "Upcoming";
            if (now <= assignment.Deadline)
                return "Active";
            if (assignment.FinalDeadline.HasValue && now <= assignment.FinalDeadline.Value)
                return "LateSubmission";
            return "Closed";
        }

        public async Task<bool> ExistsAsync(int assignmentId)
        {
            return await _context.Assignments.AnyAsync(a => a.AssignmentId == assignmentId);
        }

        // *Kiểm tra xem student có thể submit assignment không
        public async Task<bool> CanStudentSubmitAssignmentAsync(int assignmentId, int studentId)
        {
            var assignment = await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.CourseStudents)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (assignment == null) return false;

            var now = DateTime.UtcNow.AddHours(7);
            var isStudentInCourse = assignment.CourseInstance.CourseStudents
                .Any(cs => cs.UserId == studentId && cs.Status == "Enrolled");

            var canSubmit = isStudentInCourse &&
                           assignment.Status != "Draft" &&
                           assignment.Status != "Archived" &&
                           (assignment.StartDate == null || assignment.StartDate <= now) &&
                           (assignment.FinalDeadline == null || now <= assignment.FinalDeadline);

            return canSubmit;
        }

        public async Task<List<Assignment>> GetAssignmentsByRubricTemplateIdAsync(int rubricTemplateId)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Include(a => a.Rubric)
                .Where(a => a.RubricTemplateId == rubricTemplateId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAllAsync()
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Include(a => a.Rubric)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        Task IAssignmentRepository.AddAsync(Assignment assignment)
        {
            return AddAsync(assignment);
        }

        Task IAssignmentRepository.UpdateAsync(Assignment assignment)
        {
            return UpdateAsync(assignment);
        }

        Task IAssignmentRepository.DeleteAsync(Assignment assignment)
        {
            return DeleteAsync(assignment);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByStudentAndSemesterAndStatusAsync(int studentId, int semesterId, List<string> statuses)
        {
            var now = DateTime.UtcNow.AddHours(7);

            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Where(a =>
                    a.CourseInstance.SemesterId == semesterId &&
                    a.CourseInstance.CourseStudents.Any(cs => cs.UserId == studentId && cs.Status == "Enrolled") &&
                    statuses.Contains(a.Status)
                )
                .OrderBy(a => a.Deadline)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsWithSubmissionByStudentAndSemesterAsync(int studentId, int semesterId)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.Submissions.Where(s => s.UserId == studentId && s.Status == "Graded"))
                .Where(a =>
                    a.CourseInstance.SemesterId == semesterId &&
                    a.CourseInstance.CourseStudents.Any(cs => cs.UserId == studentId && cs.Status == "Enrolled") &&
                    a.Status == "GradesPublished"
                )
                .OrderBy(a => a.CourseInstance.Course.CourseCode)
                .ThenBy(a => a.Deadline)
                .ToListAsync();
        }
    }
}