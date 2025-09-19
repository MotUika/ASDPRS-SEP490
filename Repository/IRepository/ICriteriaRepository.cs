using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICriteriaRepository
    {
        Task<Criteria> GetByIdAsync(int id);
        Task<Criteria> AddAsync(Criteria entity);
        Task<Criteria> UpdateAsync(Criteria entity);
        Task<Criteria> DeleteAsync(Criteria entity);
        Task<IEnumerable<Criteria>> GetAllAsync();
        Task<IEnumerable<Criteria>> GetByRubricIdAsync(int rubricId);
        Task<IEnumerable<Criteria>> GetByCriteriaTemplateIdAsync(int criteriaTemplateId);
    }
}
