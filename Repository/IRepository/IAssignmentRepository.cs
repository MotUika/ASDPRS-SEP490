using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IAssignmentRepository
    {
        Task<Assignment> GetByIdAsync(int id);
        Task<Assignment> AddAsync(Assignment entity);
        Task<Assignment> UpdateAsync(Assignment entity);
        Task<Assignment> DeleteAsync(Assignment entity);
        Task<IEnumerable<Assignment>> GetAllAsync();
        Task<IEnumerable<Assignment>> GetByCourseInstanceIdAsync(int courseInstanceId);
        Task<Assignment> GetAssignmentWithRubricAsync(int assignmentId);
    }
}
