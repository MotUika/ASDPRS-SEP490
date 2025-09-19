using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IRubricRepository
    {
        Task<Rubric> GetByIdAsync(int id);
        Task<Rubric> AddAsync(Rubric entity);
        Task<Rubric> UpdateAsync(Rubric entity);
        Task<Rubric> DeleteAsync(Rubric entity);
        Task<IEnumerable<Rubric>> GetAllAsync();
        Task<IEnumerable<Rubric>> GetByTemplateIdAsync(int templateId);
        Task<Rubric> GetRubricWithCriteriaAsync(int rubricId);
    }
}
