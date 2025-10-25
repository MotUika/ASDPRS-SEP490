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
    public class RubricTemplateRepository : BaseRepository<RubricTemplate>, IRubricTemplateRepository
    {
        private readonly ASDPRSContext _context;

        public RubricTemplateRepository(BaseDAO<RubricTemplate> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<RubricTemplate>> GetByCreatedByUserIdAsync(int userId)
        {
            return await _context.RubricTemplates
                .Include(rt => rt.CreatedByUser)
                .Where(rt => rt.CreatedByUserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsUsingTemplateAsync(int rubricTemplateId)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Campus)
                .Where(a => a.RubricTemplateId == rubricTemplateId)
                .ToListAsync();
        }

        public async Task<RubricTemplate> GetByIdWithDetailsAsync(int templateId)
        {
            return await _context.RubricTemplates
                .Include(rt => rt.CreatedByUser)
                .Include(rt => rt.Rubrics)
                .Include(rt => rt.CriteriaTemplates)
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.TemplateId == templateId);
        }

        public async Task<IEnumerable<RubricTemplate>> GetByUserIdWithDetailsAsync(int userId)
        {
            return await _context.RubricTemplates
                .Include(rt => rt.CreatedByUser)
                .Include(rt => rt.Rubrics)
                .Include(rt => rt.CriteriaTemplates)
                .Where(rt => rt.CreatedByUserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<RubricTemplate>> GetPublicWithDetailsAsync()
        {
            return await _context.RubricTemplates
                .Include(rt => rt.CreatedByUser)
                .Include(rt => rt.Rubrics)
                .Include(rt => rt.CriteriaTemplates)
                .Where(rt => rt.IsPublic)
                .AsNoTracking()
                .ToListAsync();
        }



    }
}
