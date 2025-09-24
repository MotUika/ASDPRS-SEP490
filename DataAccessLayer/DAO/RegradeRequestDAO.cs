using BussinessObject.Models;
using DataAccessLayer.BaseDAO;
using DataAccessLayer.IBaseDAO;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.DAO
{
    public interface IRegradeRequestDAO : IBaseDAO<RegradeRequest>
    {
        IQueryable<RegradeRequest> GetAll();
        Task<RegradeRequest> GetByIdWithDetailsAsync(int id);
    }

    public class RegradeRequestDAO : BaseDAO<RegradeRequest>, IRegradeRequestDAO
    {
        private readonly ASDPRSContext _context;

        public RegradeRequestDAO(ASDPRSContext context) : base(context)
        {
            _context = context;
        }

        public IQueryable<RegradeRequest> GetAll()
        {
            return _context.RegradeRequests.AsQueryable();
        }

        public override async Task<RegradeRequest> GetByIdAsync(int id)
        {
            return await _context.RegradeRequests
                .Include(r => r.Submission)
                    .ThenInclude(s => s.Assignment)
                .Include(r => r.Submission.User)
                .Include(r => r.ReviewedByInstructor)
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }

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
