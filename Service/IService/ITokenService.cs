using BussinessObject.IdentityModel;
using BussinessObject.Models;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ITokenService
    {
        Task<TokenModel> CreateToken(User user);
        Task<ApiResponse> RenewToken(TokenModel model);
        Task<ApiResponse> RefreshToken(string refreshToken); // Thay đổi trả về ApiResponse
        Task RevokeRefreshToken(int userId);
    }
}