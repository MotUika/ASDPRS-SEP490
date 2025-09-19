using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IRubricTemplateRepository
    {
        Task<RubricTemplate> GetByIdAsync(int id);
        Task<RubricTemplate> AddAsync(RubricTemplate entity);
        Task<RubricTemplate> UpdateAsync(RubricTemplate entity);
        Task<RubricTemplate> DeleteAsync(RubricTemplate entity);
        Task<IEnumerable<RubricTemplate>> GetAllAsync();
        Task<IEnumerable<RubricTemplate>> GetByCreatedByUserIdAsync(int userId);
    }
}
