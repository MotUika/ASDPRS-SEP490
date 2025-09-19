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
    public class CurriculumRepository : BaseRepository<Curriculum>, ICurriculumRepository
    {
        private readonly ASDPRSContext _context;

        public CurriculumRepository(BaseDAO<Curriculum> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Curriculum>> GetByCampusIdAsync(int campusId)
        {
            return await _context.Curriculums
                .Include(c => c.Campus)
                .Where(c => c.CampusId == campusId)
                .ToListAsync();
        }
    }
}
