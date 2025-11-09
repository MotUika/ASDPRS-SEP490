using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Response.DocumentEmbedding;
using Service.RequestAndResponse.Response.Search;

namespace Service.IService
{
    public interface IKeywordSearchService
    {
        Task<BaseResponse<SearchResultEFResponse>> SearchAsync(string keyword, int userId, string role);
    }
}