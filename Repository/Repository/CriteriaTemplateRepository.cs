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
    public class CriteriaTemplateRepository : BaseRepository<CriteriaTemplate>, ICriteriaTemplateRepository
    {
        private readonly ASDPRSContext _context;

        public CriteriaTemplateRepository(BaseDAO<CriteriaTemplate> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<CriteriaTemplate>> GetByTemplateIdAsync(int templateId)
        {
            return await _context.CriteriaTemplates
                .Include(ct => ct.Template)
                .Where(ct => ct.TemplateId == templateId)
                .ToListAsync();
        }
    }
}
