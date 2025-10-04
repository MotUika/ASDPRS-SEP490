using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IMajorRepository
    {
        Task<Major> GetByIdAsync(int id);
        Task<IEnumerable<Major>> GetAllAsync();
        Task<Major> AddAsync(Major entity);
        Task<Major> UpdateAsync(Major entity);
        Task<Major> DeleteAsync(Major entity);
        Task<Major> GetByCodeAsync(string majorCode);
    }
}
