using BussinessObject.IdentityModel;
using BussinessObject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Repository.IBaseRepository
{
    public interface ITokenRepository
    {
        public Task<TokenModel> CreateToken(User user);
        public Task<ApiResponse> RenewToken(TokenModel model);
    }
}