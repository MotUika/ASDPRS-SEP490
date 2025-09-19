using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICourseRepository
    {
        Task<Course> GetByIdAsync(int id);
        Task<Course> AddAsync(Course entity);
        Task<Course> UpdateAsync(Course entity);
        Task<Course> DeleteAsync(Course entity);
        Task<IEnumerable<Course>> GetAllAsync();
        Task<IEnumerable<Course>> GetByCurriculumIdAsync(int curriculumId);
        Task<IEnumerable<Course>> GetByCourseCodeAsync(string courseCode);
    }
}
