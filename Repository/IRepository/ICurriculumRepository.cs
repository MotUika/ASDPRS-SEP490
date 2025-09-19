using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICurriculumRepository
    {
        Task<Curriculum> GetByIdAsync(int id);
        Task<Curriculum> AddAsync(Curriculum entity);
        Task<Curriculum> UpdateAsync(Curriculum entity);
        Task<Curriculum> DeleteAsync(Curriculum entity);
        Task<IEnumerable<Curriculum>> GetAllAsync();
        Task<IEnumerable<Curriculum>> GetByCampusIdAsync(int campusId);
    }
}
