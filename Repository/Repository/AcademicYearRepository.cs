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
    public class AcademicYearRepository : BaseRepository<AcademicYear>, IAcademicYearRepository
    {
        private readonly ASDPRSContext _context;

        public AcademicYearRepository(BaseDAO<AcademicYear> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<AcademicYear>> GetByCampusIdAsync(int campusId)
        {
            return await _context.AcademicYears
                .Include(ay => ay.Campus)
                .Where(ay => ay.CampusId == campusId)
                .ToListAsync();
        }
    }
}
