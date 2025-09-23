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
    public class DocumentEmbeddingRepository : BaseRepository<DocumentEmbedding>, IDocumentEmbeddingRepository
    {
        private readonly ASDPRSContext _context;

        public DocumentEmbeddingRepository(BaseDAO<DocumentEmbedding> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<DocumentEmbedding>> GetBySourceAsync(string sourceType, int sourceId)
        {
            return await _context.DocumentEmbeddings
                .Where(de => de.SourceType == sourceType && de.SourceId == sourceId)
                .OrderByDescending(de => de.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DocumentEmbedding>> GetByContentSearchAsync(string searchTerm)
        {
            return await _context.DocumentEmbeddings
                .Where(de => de.Content.Contains(searchTerm))
                .OrderByDescending(de => de.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DocumentEmbedding>> GetBySourceTypeAsync(string sourceType)
        {
            return await _context.DocumentEmbeddings
                .Where(de => de.SourceType == sourceType)
                .OrderByDescending(de => de.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string sourceType, int sourceId)
        {
            return await _context.DocumentEmbeddings
                .AnyAsync(de => de.SourceType == sourceType && de.SourceId == sourceId);
        }

        Task IDocumentEmbeddingRepository.AddAsync(DocumentEmbedding documentEmbedding)
        {
            return AddAsync(documentEmbedding);
        }

        Task IDocumentEmbeddingRepository.UpdateAsync(DocumentEmbedding documentEmbedding)
        {
            return UpdateAsync(documentEmbedding);
        }

        Task IDocumentEmbeddingRepository.DeleteAsync(DocumentEmbedding documentEmbedding)
        {
            return DeleteAsync(documentEmbedding);
        }
    }
}