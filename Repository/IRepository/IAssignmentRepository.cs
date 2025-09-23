using BussinessObject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IAssignmentRepository
    {
        Task<Assignment> GetByIdAsync(int id);
        Task<IEnumerable<Assignment>> GetByCourseInstanceIdAsync(int courseInstanceId);
        Task<Assignment> GetAssignmentWithRubricAsync(int assignmentId);
        Task<IEnumerable<Assignment>> GetAssignmentsByInstructorAsync(int instructorId);
        Task<IEnumerable<Assignment>> GetAssignmentsByStudentAsync(int studentId);
        Task<IEnumerable<Assignment>> GetActiveAssignmentsAsync();
        Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync();
        Task AddAsync(Assignment assignment);
        Task UpdateAsync(Assignment assignment);
        Task DeleteAsync(Assignment assignment);
        Task<bool> ExistsAsync(int assignmentId);
    }
}