using BussinessObject.IdentityModel;
using BussinessObject.Models;
using Repository.IBaseRepository;
using Service.IService;
using System.Threading.Tasks;

namespace Service.Service
{
    public class TokenService : ITokenService
    {
        private readonly ITokenRepository _tokenRepository;

        public TokenService(ITokenRepository tokenRepository)
        {
            _tokenRepository = tokenRepository;
        }

        public async Task<TokenModel> CreateToken(User user)
        {
            return await _tokenRepository.CreateToken(user);
        }

        public async Task<ApiResponse> RenewToken(TokenModel model)
        {
            return await _tokenRepository.RenewToken(model);
        }

        public async Task<ApiResponse> RefreshToken(string refreshToken)
        {
            return await _tokenRepository.RenewToken(new TokenModel { RefreshToken = refreshToken });
        }

        public async Task RevokeRefreshToken(int userId)
        {
            // Implement logic revoke token ở đây
            // Ví dụ: xóa refresh token từ database
            await Task.CompletedTask;
        }
    }
}