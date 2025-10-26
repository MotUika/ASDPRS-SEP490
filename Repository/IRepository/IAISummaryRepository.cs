using BussinessObject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IAISummaryRepository
    {
        Task<AISummary> GetByIdAsync(int id);
        Task<IEnumerable<AISummary>> GetBySubmissionIdAsync(int submissionId);
        Task<IEnumerable<AISummary>> GetBySummaryTypeAsync(string summaryType);
        Task<IEnumerable<AISummary>> GetBySubmissionAndTypeAsync(int submissionId, string summaryType);
        Task<IEnumerable<AISummary>> GetRecentSummariesAsync(int maxResults = 10);
        Task AddAsync(AISummary aiSummary);
        Task UpdateAsync(AISummary aiSummary);
        Task DeleteAsync(AISummary aiSummary);
        Task<bool> ExistsAsync(int submissionId, string summaryType);
        Task<IEnumerable<AISummary>> GetBySubmissionAndTypePrefixAsync(int submissionId, string typePrefix);

    }
}