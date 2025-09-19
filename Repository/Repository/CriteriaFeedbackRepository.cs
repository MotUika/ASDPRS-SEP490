using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repository
{
    public class CriteriaFeedbackRepository : BaseRepository<CriteriaFeedback>, ICriteriaFeedbackRepository
    {
        private readonly ASDPRSContext _context;

        public CriteriaFeedbackRepository(BaseDAO<CriteriaFeedback> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<CriteriaFeedback>> GetByReviewIdAsync(int reviewId)
        {
            return await _context.CriteriaFeedbacks
                .Include(cf => cf.Review)
                .Include(cf => cf.Criteria)
                .ThenInclude(c => c.CriteriaTemplate)
                .Where(cf => cf.ReviewId == reviewId)
                .ToListAsync();
        }

        public async Task<IEnumerable<CriteriaFeedback>> GetByCriteriaIdAsync(int criteriaId)
        {
            return await _context.CriteriaFeedbacks
                .Include(cf => cf.Review)
                .Include(cf => cf.Criteria)
                .ThenInclude(c => c.CriteriaTemplate)
                .Where(cf => cf.CriteriaId == criteriaId)
                .ToListAsync();
        }
    }
}
