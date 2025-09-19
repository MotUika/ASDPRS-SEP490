using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICriteriaTemplateRepository
    {
        Task<CriteriaTemplate> GetByIdAsync(int id);
        Task<CriteriaTemplate> AddAsync(CriteriaTemplate entity);
        Task<CriteriaTemplate> UpdateAsync(CriteriaTemplate entity);
        Task<CriteriaTemplate> DeleteAsync(CriteriaTemplate entity);
        Task<IEnumerable<CriteriaTemplate>> GetAllAsync();
        Task<IEnumerable<CriteriaTemplate>> GetByTemplateIdAsync(int templateId);
    }
}
