using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repository.Repository
{
    public class AISummaryRepository : BaseRepository<AISummary>, IAISummaryRepository
    {
        private readonly ASDPRSContext _context;

        public AISummaryRepository(BaseDAO<AISummary> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<AISummary>> GetBySubmissionIdAsync(int submissionId)
        {
            return await _context.AISummaries
                .Include(a => a.Submission)
                .ThenInclude(s => s.Assignment)
                .ThenInclude(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(a => a.SubmissionId == submissionId)
                .OrderByDescending(a => a.GeneratedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AISummary>> GetBySummaryTypeAsync(string summaryType)
        {
            return await _context.AISummaries
                .Include(a => a.Submission)
                .ThenInclude(s => s.Assignment)
                .ThenInclude(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(a => a.SummaryType == summaryType)
                .OrderByDescending(a => a.GeneratedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AISummary>> GetBySubmissionAndTypeAsync(int submissionId, string summaryType)
        {
            return await _context.AISummaries
                .Include(a => a.Submission)
                .ThenInclude(s => s.Assignment)
                .ThenInclude(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(a => a.SubmissionId == submissionId && a.SummaryType == summaryType)
                .OrderByDescending(a => a.GeneratedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AISummary>> GetRecentSummariesAsync(int maxResults = 10)
        {
            return await _context.AISummaries
                .Include(a => a.Submission)
                .ThenInclude(s => s.Assignment)
                .ThenInclude(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .OrderByDescending(a => a.GeneratedAt)
                .Take(maxResults)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int submissionId, string summaryType)
        {
            return await _context.AISummaries
                .AnyAsync(a => a.SubmissionId == submissionId && a.SummaryType == summaryType);
        }

        Task IAISummaryRepository.AddAsync(AISummary aiSummary)
        {
            return AddAsync(aiSummary);
        }

        Task IAISummaryRepository.UpdateAsync(AISummary aiSummary)
        {
            return UpdateAsync(aiSummary);
        }

        Task IAISummaryRepository.DeleteAsync(AISummary aiSummary)
        {
            return DeleteAsync(aiSummary);
        }
    }
}