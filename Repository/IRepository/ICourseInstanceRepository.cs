using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICourseInstanceRepository
    {
        Task<CourseInstance> GetByIdAsync(int id);
        Task<CourseInstance> AddAsync(CourseInstance entity);
        Task<CourseInstance> UpdateAsync(CourseInstance entity);
        Task<CourseInstance> DeleteAsync(CourseInstance entity);
        Task<IEnumerable<CourseInstance>> GetAllAsync();
        Task<IEnumerable<CourseInstance>> GetByCourseIdAsync(int courseId);
        Task<IEnumerable<CourseInstance>> GetBySemesterIdAsync(int semesterId);
        Task<IEnumerable<CourseInstance>> GetByCampusIdAsync(int campusId);
        Task<CourseInstance> GetByIdWithRelationsAsync(int courseInstanceId);

    }
}
