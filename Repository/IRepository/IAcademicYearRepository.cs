using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IAcademicYearRepository
    {
        Task<AcademicYear> GetByIdAsync(int id);
        Task<AcademicYear> AddAsync(AcademicYear entity);
        Task<AcademicYear> UpdateAsync(AcademicYear entity);
        Task<AcademicYear> DeleteAsync(AcademicYear entity);
        Task<IEnumerable<AcademicYear>> GetAllAsync();
        Task<IEnumerable<AcademicYear>> GetByCampusIdAsync(int campusId);
    }
}
