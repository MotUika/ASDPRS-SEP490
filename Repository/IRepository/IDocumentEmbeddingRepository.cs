using BussinessObject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IDocumentEmbeddingRepository
    {
        Task<DocumentEmbedding> GetByIdAsync(int id);
        Task<IEnumerable<DocumentEmbedding>> GetBySourceAsync(string sourceType, int sourceId);
        Task<IEnumerable<DocumentEmbedding>> GetByContentSearchAsync(string searchTerm);
        Task<IEnumerable<DocumentEmbedding>> GetBySourceTypeAsync(string sourceType);
        Task<IEnumerable<DocumentEmbedding>> GetAllAsync();
        Task AddAsync(DocumentEmbedding documentEmbedding);
        Task UpdateAsync(DocumentEmbedding documentEmbedding);
        Task DeleteAsync(DocumentEmbedding documentEmbedding);
        Task<bool> ExistsAsync(string sourceType, int sourceId);
    }
}