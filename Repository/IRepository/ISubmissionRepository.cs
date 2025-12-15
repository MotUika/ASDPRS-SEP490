using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface ISubmissionRepository
    {
        Task<Submission> GetByIdAsync(int id);
        Task<Submission> AddAsync(Submission entity);
        Task<Submission> UpdateAsync(Submission entity);
        Task<Submission> DeleteAsync(Submission entity);
        Task<IEnumerable<Submission>> GetAllAsync();
        Task<IEnumerable<Submission>> GetByAssignmentIdAsync(int assignmentId);
        Task<IEnumerable<Submission>> GetByUserIdAsync(int userId);
        Task<Submission> GetSubmissionWithReviewsAsync(int submissionId);
        Task<Submission> GetByAssignmentAndUserAsync(int assignmentId, int userId);
        Task<IEnumerable<Submission>> GetByCourseInstanceAndUserAsync(int courseInstanceId, int userId);
        Task<IEnumerable<Submission>> GetByUserAndSemesterAsync(int userId, int semesterId);
        Task AddRangeAsync(IEnumerable<Submission> submissions);
        Task UpdateGradedAtAsync(int submissionId);
    }
}
