using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IReviewRepository
    {
        Task<Review> GetByIdAsync(int id);
        Task<Review> AddAsync(Review entity);
        Task<Review> UpdateAsync(Review entity);
        Task<Review> DeleteAsync(Review entity);
        Task<IEnumerable<Review>> GetAllAsync();
        Task<IEnumerable<Review>> GetByReviewAssignmentIdAsync(int reviewAssignmentId);
        Task<IEnumerable<Review>> GetBySubmissionIdAsync(int submissionId);
        Task<IEnumerable<Review>> GetByReviewerIdAsync(int reviewerId);
    }
}
