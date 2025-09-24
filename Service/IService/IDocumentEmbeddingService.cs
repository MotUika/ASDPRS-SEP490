using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.DocumentEmbedding;
using Service.RequestAndResponse.Response.DocumentEmbedding;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IDocumentEmbeddingService
    {
        Task<BaseResponse<DocumentEmbeddingResponse>> CreateDocumentEmbeddingAsync(CreateDocumentEmbeddingRequest request);
        Task<BaseResponse<DocumentEmbeddingResponse>> UpdateDocumentEmbeddingAsync(UpdateDocumentEmbeddingRequest request);
        Task<BaseResponse<bool>> DeleteDocumentEmbeddingAsync(int embeddingId);
        Task<BaseResponse<DocumentEmbeddingResponse>> GetDocumentEmbeddingByIdAsync(int embeddingId);
        Task<BaseResponse<List<DocumentEmbeddingResponse>>> GetDocumentEmbeddingsBySourceAsync(string sourceType, int sourceId);
        Task<BaseResponse<List<DocumentEmbeddingResponse>>> GetDocumentEmbeddingsBySourceTypeAsync(string sourceType);
        Task<BaseResponse<SearchResultResponse>> SearchDocumentEmbeddingsAsync(SearchDocumentEmbeddingRequest request);
        Task<BaseResponse<bool>> GenerateEmbeddingForSourceAsync(string sourceType, int sourceId);
        Task<BaseResponse<List<DocumentEmbeddingResponse>>> GetSimilarDocumentsAsync(int embeddingId, int maxResults = 5);
    }
}