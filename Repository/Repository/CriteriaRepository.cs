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
    public class CriteriaRepository : BaseRepository<Criteria>, ICriteriaRepository
    {
        private readonly ASDPRSContext _context;

        public CriteriaRepository(BaseDAO<Criteria> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Criteria>> GetByRubricIdAsync(int rubricId)
        {
            return await _context.Criteria
                .Include(c => c.Rubric)
                .Include(c => c.CriteriaTemplate)
                .Where(c => c.RubricId == rubricId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Criteria>> GetByCriteriaTemplateIdAsync(int criteriaTemplateId)
        {
            return await _context.Criteria
                .Include(c => c.Rubric)
                .Include(c => c.CriteriaTemplate)
                .Where(c => c.CriteriaTemplateId == criteriaTemplateId)
                .ToListAsync();
        }
    }
}
