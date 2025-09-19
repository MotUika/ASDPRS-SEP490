using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ICriteriaFeedbackRepository
    {
        Task<CriteriaFeedback> GetByIdAsync(int id);
        Task<CriteriaFeedback> AddAsync(CriteriaFeedback entity);
        Task<CriteriaFeedback> UpdateAsync(CriteriaFeedback entity);
        Task<CriteriaFeedback> DeleteAsync(CriteriaFeedback entity);
        Task<IEnumerable<CriteriaFeedback>> GetAllAsync();
        Task<IEnumerable<CriteriaFeedback>> GetByReviewIdAsync(int reviewId);
        Task<IEnumerable<CriteriaFeedback>> GetByCriteriaIdAsync(int criteriaId);
    }
}
