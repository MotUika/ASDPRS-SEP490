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
    public class SemesterRepository : BaseRepository<Semester>, ISemesterRepository
    {
        private readonly ASDPRSContext _context;

        public SemesterRepository(BaseDAO<Semester> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Semester>> GetByAcademicYearIdAsync(int academicYearId)
        {
            return await _context.Semesters
                .Include(s => s.AcademicYear)
                .Where(s => s.AcademicYearId == academicYearId)
                .ToListAsync();
        }
    }
}
