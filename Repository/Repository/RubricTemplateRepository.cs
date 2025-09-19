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
    }
}
