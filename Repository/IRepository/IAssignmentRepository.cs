using BussinessObject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IAssignmentRepository
    {
        Task<Assignment> GetByIdAsync(int id);
        Task<IEnumerable<Assignment>> GetByCourseInstanceIdAsync(int courseInstanceId);
        Task<Assignment> GetAssignmentWithCloneInfoAsync(int assignmentId);
        Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(int courseInstanceId, string status);
        Task<Assignment> GetAssignmentWithRubricAsync(int assignmentId);
        Task<HashSet<int>> GetActiveAssignmentIdsAsync(int courseInstanceId);
        Task<IEnumerable<Assignment>> GetUpcomingDeadlineAssignmentsAsync(int daysBefore = 1);
        Task<bool> CanCloneAssignmentAsync(int sourceAssignmentId, int targetCourseInstanceId);
        Task<IEnumerable<Assignment>> GetClonedAssignmentsAsync(int originalAssignmentId);
        Task<IEnumerable<Assignment>> GetAssignmentsByInstructorAsync(int instructorId, bool includeDrafts = false);
        Task<IEnumerable<Assignment>> GetAssignmentsByStudentAsync(int studentId);
        Task<IEnumerable<Assignment>> GetActiveAssignmentsAsync();
        Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync();
        Task UpdateAssignmentStatusBasedOnTimelineAsync();
        Task AddAsync(Assignment assignment);
        Task UpdateAsync(Assignment assignment);
        Task DeleteAsync(Assignment assignment);
        Task<bool> CanStudentSubmitAssignmentAsync(int assignmentId, int studentId);
        Task<bool> ExistsAsync(int assignmentId);
        Task<List<Assignment>> GetAssignmentsByRubricTemplateIdAsync(int rubricTemplateId);
        Task<IEnumerable<Assignment>> GetAllAsync();
        Task<IEnumerable<Assignment>> GetAssignmentsByStudentAndSemesterAndStatusAsync(int studentId, int semesterId, List<string> statuses);
        Task<IEnumerable<Assignment>> GetAssignmentsWithSubmissionByStudentAndSemesterAsync(int studentId, int semesterId);
    }
}