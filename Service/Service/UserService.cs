using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.User;
using Service.RequestAndResponse.Response.User;
using Service.RequestAndResponse.Response.User.Service.RequestAndResponse.Response.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public UserService(IUserRepository userRepository, ASDPRSContext context, IMapper mapper, IEmailService emailService)
        {
            _userRepository = userRepository;
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
        }

        public async Task<BaseResponse<UserResponse>> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return new BaseResponse<UserResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<UserResponse>(user);
                return new BaseResponse<UserResponse>("User retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<UserResponse>($"Error retrieving user: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<UserResponse>>> GetAllUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var response = _mapper.Map<IEnumerable<UserResponse>>(users);
                return new BaseResponse<IEnumerable<UserResponse>>("Users retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<UserResponse>>($"Error retrieving users: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<UserResponse>> UpdateUserAsync(UpdateUserRequest request)
        {
            try
            {
                var existingUser = await _userRepository.GetByIdAsync(request.UserId);
                if (existingUser == null)
                {
                    return new BaseResponse<UserResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                if (existingUser.Email != request.Email)
                {
                    var userWithSameEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                    if (userWithSameEmail != null && userWithSameEmail.UserId != request.UserId)
                    {
                        return new BaseResponse<UserResponse>("Another user with this email already exists", StatusCodeEnum.Conflict_409, null);
                    }
                }

                if (existingUser.Username != request.Username)
                {
                    var userWithSameUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
                    if (userWithSameUsername != null && userWithSameUsername.UserId != request.UserId)
                    {
                        return new BaseResponse<UserResponse>("Another user with this username already exists", StatusCodeEnum.Conflict_409, null);
                    }
                }

                _mapper.Map(request, existingUser);
                var updatedUser = await _userRepository.UpdateAsync(existingUser);
                var response = _mapper.Map<UserResponse>(updatedUser);

                return new BaseResponse<UserResponse>("User updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<UserResponse>($"Error updating user: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);
                }

                await _userRepository.DeleteAsync(user);
                return new BaseResponse<bool>("User deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting user: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<UserResponse>> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    return new BaseResponse<UserResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<UserResponse>(user);
                return new BaseResponse<UserResponse>("User retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<UserResponse>($"Error retrieving user: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<UserResponse>> GetUserByUsernameAsync(string username)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    return new BaseResponse<UserResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<UserResponse>(user);
                return new BaseResponse<UserResponse>("User retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<UserResponse>($"Error retrieving user: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<UserResponse>>> GetUsersByRoleAsync(string roleName)
        {
            try
            {
                var users = await _userRepository.GetByRoleAsync(roleName);
                var response = _mapper.Map<IEnumerable<UserResponse>>(users);
                return new BaseResponse<IEnumerable<UserResponse>>("Users retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<UserResponse>>($"Error retrieving users: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<UserResponse>>> GetUsersByCampusAsync(int campusId)
        {
            try
            {
                var users = await _userRepository.GetByCampusIdAsync(campusId);
                var response = _mapper.Map<IEnumerable<UserResponse>>(users);
                return new BaseResponse<IEnumerable<UserResponse>>("Users retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<UserResponse>>($"Error retrieving users: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<string>> UpdateUserAvatarAsync(UpdateUserAvatarRequest request)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new BaseResponse<string>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                if (!string.IsNullOrEmpty(request.AvatarUrl) &&
                    !Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out Uri uriResult))
                {
                    return new BaseResponse<string>("Invalid avatar URL format", StatusCodeEnum.BadRequest_400, null);
                }

                user.AvatarUrl = request.AvatarUrl;
                await _userRepository.UpdateAsync(user);

                return new BaseResponse<string>("Avatar updated successfully", StatusCodeEnum.OK_200, request.AvatarUrl);
            }
            catch (Exception ex)
            {
                return new BaseResponse<string>($"Error updating avatar: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);
                }

                // Verify current password
                if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return new BaseResponse<bool>("Current password is incorrect", StatusCodeEnum.BadRequest_400, false);
                }

                // Update password
                user.PasswordHash = HashPassword(request.NewPassword);
                await _userRepository.UpdateAsync(user);

                return new BaseResponse<bool>("Password changed successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error changing password: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<bool>> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);
                }

                user.IsActive = false;
                await _userRepository.UpdateAsync(user);

                return new BaseResponse<bool>("User deactivated successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deactivating user: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<bool>> ActivateUserAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);
                }

                user.IsActive = true;
                await _userRepository.UpdateAsync(user);

                return new BaseResponse<bool>("User activated successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error activating user: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }
        public async Task<BaseResponse<AccountStatisticsResponse>> GetTotalAccountsAsync()
        {
            try
            {
                // Get all users with their roles
                var usersWithRoles = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ToListAsync();

                // Count users by role
                var adminCount = usersWithRoles.Count(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Admin"));
                var studentCount = usersWithRoles.Count(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Student"));
                var instructorCount = usersWithRoles.Count(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Instructor"));

                var totalAccounts = usersWithRoles.Count;

                var response = new AccountStatisticsResponse
                {
                    TotalAccounts = totalAccounts,
                    AdminAccounts = adminCount,
                    StudentAccounts = studentCount,
                    InstructorAccounts = instructorCount
                };

                return new BaseResponse<AccountStatisticsResponse>("Account statistics retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AccountStatisticsResponse>($"Error retrieving account statistics: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        private async Task<bool> SendWelcomeEmail(User user, string password)
        {
            try
            {
                var subject = "Welcome to ASDPRS System - Your Account Credentials";
                var htmlContent = $@"
            <html>
            <body>
                <h2>Welcome to ASDPRS System</h2>
                <p>Dear {user.FirstName} {user.LastName},</p>
                <p>Your account has been successfully created by the administrator.</p>
                <p><strong>Username:</strong> {user.Username}</p>
                <p><strong>Password:</strong> {password}</p>
                <p>Please log in and change your password as soon as possible for security reasons.</p>
                <br>
                <p>Best regards,<br>ASDPRS Team</p>
            </body>
            </html>";

                return await _emailService.SendEmail(user.Email, subject, htmlContent);
            }
            catch
            {
                return false;
            }
        }
        public async Task<BaseResponse<UserResponse>> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return new BaseResponse<UserResponse>("Email is required", StatusCodeEnum.BadRequest_400, null);
                }

                // Check if email already exists
                var existingUserByEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUserByEmail != null)
                {
                    return new BaseResponse<UserResponse>("User with this email already exists", StatusCodeEnum.Conflict_409, null);
                }

                // Check if username already exists
                var existingUserByUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
                if (existingUserByUsername != null)
                {
                    return new BaseResponse<UserResponse>("User with this username already exists", StatusCodeEnum.Conflict_409, null);
                }

                var user = _mapper.Map<User>(request);

                // Generate random password if not provided
                string password = string.IsNullOrEmpty(request.Password) ?
                    GenerateRandomPassword() : request.Password;

                // Hash the password
                user.PasswordHash = HashPassword(password);

                var createdUser = await _userRepository.AddAsync(user);
                var response = _mapper.Map<UserResponse>(createdUser);

                // Send welcome email with credentials
                await SendWelcomeEmail(createdUser, password);

                return new BaseResponse<UserResponse>("User created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<UserResponse>($"Error creating user: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_-+=[{]};:>|./?";
            var random = new Random();
            var chars = new char[length];

            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }

            return new string(chars);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }
    }
}
