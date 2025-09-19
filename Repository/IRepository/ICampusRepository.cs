using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICampusRepository
    {
        Task<Campus> GetByIdAsync(int id);
        Task<Campus> AddAsync(Campus entity);
        Task<Campus> UpdateAsync(Campus entity);
        Task<Campus> DeleteAsync(Campus entity);
        Task<IEnumerable<Campus>> GetAllAsync();
    }
}
