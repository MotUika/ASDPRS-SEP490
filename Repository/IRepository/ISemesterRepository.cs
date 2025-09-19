using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ISemesterRepository
    {
        Task<Semester> GetByIdAsync(int id);
        Task<Semester> AddAsync(Semester entity);
        Task<Semester> UpdateAsync(Semester entity);
        Task<Semester> DeleteAsync(Semester entity);
        Task<IEnumerable<Semester>> GetAllAsync();
        Task<IEnumerable<Semester>> GetByAcademicYearIdAsync(int academicYearId);
    }
}
