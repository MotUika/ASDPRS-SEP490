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
    public class RubricRepository : BaseRepository<Rubric>, IRubricRepository
    {
        private readonly ASDPRSContext _context;

        public RubricRepository(BaseDAO<Rubric> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Rubric>> GetByTemplateIdAsync(int templateId)
        {
            return await _context.Rubrics
                .Include(r => r.Template)
                .Include(r => r.Assignment)
                .Where(r => r.TemplateId == templateId)
                .ToListAsync();
        }

        public async Task<Rubric> GetRubricWithCriteriaAsync(int rubricId)
        {
            return await _context.Rubrics
                .Include(r => r.Criteria)
                .ThenInclude(c => c.CriteriaTemplate)
                .FirstOrDefaultAsync(r => r.RubricId == rubricId);
        }
    }
}
