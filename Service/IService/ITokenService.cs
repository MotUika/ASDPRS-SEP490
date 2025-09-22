using BussinessObject.IdentityModel;
using BussinessObject.Models;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ITokenService
    {
        Task<TokenModel> CreateToken(User user);
        Task<ApiResponse> RenewToken(TokenModel model);
    }
}