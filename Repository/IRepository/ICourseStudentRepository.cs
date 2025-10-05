using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICourseStudentRepository
    {
        Task<CourseStudent> GetByIdAsync(int id);
        Task<CourseStudent> AddAsync(CourseStudent entity);
        Task<CourseStudent> UpdateAsync(CourseStudent entity);
        Task<CourseStudent> DeleteAsync(CourseStudent entity);
        Task<IEnumerable<CourseStudent>> GetAllAsync();
        Task<IEnumerable<CourseStudent>> GetByCourseInstanceIdAsync(int courseInstanceId);
        Task<IEnumerable<CourseStudent>> GetByUserIdAsync(int userId);
        Task<CourseStudent> GetByCourseInstanceAndUserAsync(int courseInstanceId, int userId);
        Task<List<CourseStudent>> GetByStudentIdAsync(int studentId);

    }
}
