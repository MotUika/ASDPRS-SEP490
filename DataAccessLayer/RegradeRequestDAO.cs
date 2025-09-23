using BussinessObject.Models;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    public class RegradeRequestDAO : BaseDAO<RegradeRequest>
    {
        private readonly ASDPRSContext _context;

        public RegradeRequestDAO(ASDPRSContext context) : base(context)
        {
            _context = context;
        }

        // Thêm phương thức để lấy IQueryable cho các truy vấn phức tạp
        public IQueryable<RegradeRequest> GetAll()
        {
            return _context.Set<RegradeRequest>().AsQueryable();
        }

        // Override phương thức GetByIdAsync để include các navigation properties
        public override async Task<RegradeRequest> GetByIdAsync(int id)
        {
            return await _context.RegradeRequests
                .Include(r => r.Submission)
                    .ThenInclude(s => s.Assignment)
                .Include(r => r.Submission.User)
                .Include(r => r.ReviewedByInstructor)
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }

        // Phương thức lấy RegradeRequest với đầy đủ thông tin liên quan
        public async Task<RegradeRequest> GetByIdWithDetailsAsync(int id)
        {
            return await _context.RegradeRequests
                .Include(r => r.Submission)
                    .ThenInclude(s => s.Assignment)
                .Include(r => r.Submission.User)
                .Include(r => r.ReviewedByInstructor)
                .Include(r => r.ReviewedByUser)
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }
    }
}