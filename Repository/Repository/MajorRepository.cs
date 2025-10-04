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
    public class MajorRepository : BaseRepository<Major>, IMajorRepository
    {
        private readonly ASDPRSContext _context;

        public MajorRepository(BaseDAO<Major> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Major>> GetAllAsync()
        {
            return await _context.Majors
                .Include(m => m.Curriculums)
                .ToListAsync();
        }

        public async Task<Major> GetByIdAsync(int id)
        {
            return await _context.Majors
                .Include(m => m.Curriculums)
                .FirstOrDefaultAsync(m => m.MajorId == id);
        }

        public async Task<Major> GetByCodeAsync(string majorCode)
        {
            return await _context.Majors
                .FirstOrDefaultAsync(m => m.MajorCode == majorCode);
        }
    }
}
