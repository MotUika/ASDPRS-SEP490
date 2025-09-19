using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICourseInstructorRepository
    {
        Task<CourseInstructor> GetByIdAsync(int id);
        Task<CourseInstructor> AddAsync(CourseInstructor entity);
        Task<CourseInstructor> UpdateAsync(CourseInstructor entity);
        Task<CourseInstructor> DeleteAsync(CourseInstructor entity);
        Task<IEnumerable<CourseInstructor>> GetAllAsync();
        Task<IEnumerable<CourseInstructor>> GetByCourseInstanceIdAsync(int courseInstanceId);
        Task<IEnumerable<CourseInstructor>> GetByUserIdAsync(int userId);
    }
}
