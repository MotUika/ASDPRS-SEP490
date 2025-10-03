using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.User;
using Service.RequestAndResponse.Response.User;
using Service.RequestAndResponse.Response.User.Service.RequestAndResponse.Response.User;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IUserService
    {
        Task<BaseResponse<UserResponse>> GetUserByIdAsync(int id);
        Task<BaseResponse<IEnumerable<UserResponse>>> GetAllUsersAsync();
        Task<BaseResponse<UserResponse>> CreateUserAsync(CreateUserRequest request);
        Task<BaseResponse<UserResponse>> UpdateUserAsync(UpdateUserRequest request);
        Task<BaseResponse<bool>> DeleteUserAsync(int id);
        Task<BaseResponse<UserResponse>> GetUserByEmailAsync(string email);
        Task<BaseResponse<UserResponse>> GetUserByUsernameAsync(string username);
        Task<BaseResponse<IEnumerable<UserResponse>>> GetUsersByRoleAsync(string roleName);
        Task<BaseResponse<IEnumerable<UserResponse>>> GetUsersByCampusAsync(int campusId);
        Task<BaseResponse<string>> UpdateUserAvatarAsync(UpdateUserAvatarRequest request);
        Task<BaseResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request);
        Task<BaseResponse<bool>> DeactivateUserAsync(int userId);
        Task<BaseResponse<bool>> ActivateUserAsync(int userId);
        Task<BaseResponse<AccountStatisticsResponse>> GetTotalAccountsAsync();
        Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<BaseResponse<bool>> AssignRolesAsync(AssignRoleRequest request);
        Task<BaseResponse<IEnumerable<string>>> GetUserRolesAsync(int userId);
    }
}