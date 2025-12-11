using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public UserService(
            IUserRepository userRepository,
            ASDPRSContext context,
            IMapper mapper,
            IEmailService emailService,
            ITokenService tokenService,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userRepository = userRepository;
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _tokenService = tokenService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<BaseResponse<UserResponse>> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Campus)
                    .Include(u => u.Major)
                    .FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    return new BaseResponse<UserResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<UserResponse>(user);
                response.Roles = (await _userManager.GetRolesAsync(user)).ToList();

                return new BaseResponse<UserResponse>("User retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<UserResponse>($"Error retrieving user: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task<BaseResponse<UserDetailResponse>> GetUserByIdDetailAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Campus)
                    .Include(u => u.Major)
                    .Include(u => u.CourseStudents).ThenInclude(cs => cs.CourseInstance).ThenInclude(ci => ci.Course)
                    .Include(u => u.CourseInstructors).ThenInclude(ci => ci.CourseInstance).ThenInclude(ci => ci.Course)
                    .Include(u => u.Submissions).ThenInclude(s => s.Assignment).ThenInclude(a => a.CourseInstance).ThenInclude(ci => ci.Course)
                    .Include(u => u.Submissions).ThenInclude(s => s.Assignment).ThenInclude(a => a.CourseInstance).ThenInclude(ci => ci.Semester)
                    .Include(u => u.ReviewAssignments)
                    .FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    return new BaseResponse<UserDetailResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }
                var response = _mapper.Map<UserDetailResponse>(user);
                response.Roles = (await _userManager.GetRolesAsync(user)).ToList();

                // Populate enrolled courses (for students)
                if (response.Roles.Contains("Student"))
                {
                    response.EnrolledCourses = user.CourseStudents.Select(cs => new EnrolledCourseDetail
                    {
                        CourseInstanceId = cs.CourseInstanceId,
                        CourseName = cs.CourseInstance?.Course?.CourseName,
                        Status = cs.Status,
                        FinalGrade = cs.FinalGrade,
                        IsPassed = cs.IsPassed
                    }).ToList();
                }

                // Populate taught courses (for instructors)
                if (response.Roles.Contains("Instructor"))
                {
                    response.TaughtCourses = user.CourseInstructors.Select(ci => new TaughtCourseDetail
                    {
                        CourseInstanceId = ci.CourseInstanceId,
                        CourseName = ci.CourseInstance?.Course?.CourseName
                    }).ToList();
                }

                response.SubmissionsHistory = user.Submissions.Select(s => new SubmissionHistory
                {
                    AssignmentId = s.AssignmentId,
                    AssignmentTitle = s.Assignment?.Title,
                    CourseInstanceId = s.Assignment?.CourseInstanceId ?? 0,
                    CourseName = s.Assignment?.CourseInstance?.Course?.CourseName,
                    SemesterName = s.Assignment?.CourseInstance?.Semester?.Name,
                    SubmittedAt = s.SubmittedAt,
                    Status = s.Status,
                    FinalScore = s.FinalScore
                }).ToList();

                return new BaseResponse<UserDetailResponse>("User retrieved successfully with full history", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<UserDetailResponse>($"Error retrieving user: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<UserResponse>>> GetAllUsersAsync()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Campus)
                    .Include(u => u.Major)
                    .ToListAsync();

                var responses = new List<UserResponse>();

                foreach (var user in users)
                {
                    var mapped = _mapper.Map<UserResponse>(user);
                    mapped.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                    responses.Add(mapped);
                }

                return new BaseResponse<IEnumerable<UserResponse>>("Users retrieved successfully", StatusCodeEnum.OK_200, responses);
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
                    var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
                    if (userWithSameEmail != null && userWithSameEmail.Id != request.UserId)
                    {
                        return new BaseResponse<UserResponse>("Another user with this email already exists", StatusCodeEnum.Conflict_409, null);
                    }
                }
                if (existingUser.UserName != request.Username)
                {
                    var userWithSameUsername = await _userManager.FindByNameAsync(request.Username);
                    if (userWithSameUsername != null && userWithSameUsername.Id != request.UserId)
                    {
                        return new BaseResponse<UserResponse>("Another user with this username already exists", StatusCodeEnum.Conflict_409, null);
                    }
                }
                if (!string.IsNullOrEmpty(request.StudentCode) && existingUser.StudentCode != request.StudentCode)
                {
                    var userWithSameCode = await _context.Users.FirstOrDefaultAsync(u => u.StudentCode == request.StudentCode);
                    if (userWithSameCode != null && userWithSameCode.Id != request.UserId)
                    {
                        return new BaseResponse<UserResponse>("Another user with this student code already exists", StatusCodeEnum.Conflict_409, null);
                    }
                }
                _mapper.Map(request, existingUser);
                var result = await _userManager.UpdateAsync(existingUser);
                if (!result.Succeeded)
                {
                    return new BaseResponse<UserResponse>($"Error updating user: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, null);
                }
                var response = _mapper.Map<UserResponse>(existingUser);
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
                var user = await _context.Users
                    .Include(u => u.CourseStudents)
                    .Include(u => u.CourseInstructors)
                    .Include(u => u.Submissions)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);

                if (user.CourseStudents.Any() || user.CourseInstructors.Any() || user.Submissions.Any())
                {
                    return new BaseResponse<bool>("Cannot delete user who has enrolled courses, teaching assignments, or submissions. Please deactivate the user instead.", StatusCodeEnum.Conflict_409, false);
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded) 
                {
                    return new BaseResponse<bool>($"Error deleting user: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, false);
                }

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
                var user = await _context.Users
                    .Include(u => u.Campus)
                    .Include(u => u.Major)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return new BaseResponse<UserResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<UserResponse>(user);
                response.Roles = (await _userManager.GetRolesAsync(user)).ToList();

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
                var user = await _context.Users
                    .Include(u => u.Campus)
                    .Include(u => u.Major)
                    .FirstOrDefaultAsync(u => u.UserName == username);

                if (user == null)
                {
                    return new BaseResponse<UserResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<UserResponse>(user);
                response.Roles = (await _userManager.GetRolesAsync(user)).ToList();

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
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                var userIds = usersInRole.Select(u => u.Id).ToList();

                var users = await _context.Users
                    .Include(u => u.Campus)
                    .Include(u => u.Major)
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                var responses = new List<UserResponse>();
                foreach (var user in users)
                {
                    var mapped = _mapper.Map<UserResponse>(user);
                    mapped.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                    responses.Add(mapped);
                }

                return new BaseResponse<IEnumerable<UserResponse>>("Users retrieved successfully", StatusCodeEnum.OK_200, responses);
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
                var users = await _context.Users
                    .Include(u => u.Campus)
                    .Include(u => u.Major)
                    .Where(u => u.CampusId == campusId)
                    .ToListAsync();

                var responses = new List<UserResponse>();
                foreach (var user in users)
                {
                    var mapped = _mapper.Map<UserResponse>(user);
                    mapped.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                    responses.Add(mapped);
                }

                return new BaseResponse<IEnumerable<UserResponse>>("Users retrieved successfully", StatusCodeEnum.OK_200, responses);
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
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
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
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return new BaseResponse<string>($"Error updating avatar: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, null);
                }

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
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                {
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);
                }
                var hasNumber = new Regex(@"[0-9]+");
                var hasUpperChar = new Regex(@"[A-Z]+");
                var hasMinimum8Chars = new Regex(@".{8,}");
                var hasSpecialChar = new Regex(@"[@#$%^&*!]+");

                if (!hasMinimum8Chars.IsMatch(request.NewPassword))
                    return new BaseResponse<bool>("Password must be at least 8 characters.", StatusCodeEnum.BadRequest_400, false);
                if (!hasUpperChar.IsMatch(request.NewPassword))
                    return new BaseResponse<bool>("Password must contain at least one uppercase letter.", StatusCodeEnum.BadRequest_400, false);
                if (!hasNumber.IsMatch(request.NewPassword))
                    return new BaseResponse<bool>("Password must contain at least one number.", StatusCodeEnum.BadRequest_400, false);
                if (!hasSpecialChar.IsMatch(request.NewPassword))
                    return new BaseResponse<bool>("Password must contain at least one special character (@, #, $, etc.).", StatusCodeEnum.BadRequest_400, false);

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (!result.Succeeded)
                {
                    return new BaseResponse<bool>($"Error changing password: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, false);
                }

                return new BaseResponse<bool>("Password changed successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error changing password: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }
        public async Task<BaseResponse<bool>> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return new BaseResponse<bool>("If the email exists, a new password has been sent.", StatusCodeEnum.OK_200, true);
                }

                // Tạo mật khẩu mới thỏa mãn điều kiện
                string newPassword = GenerateStrongPassword();

                // Tạo token reset password (cần thiết vì ta không biết password cũ)
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Reset password
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!result.Succeeded)
                {
                    return new BaseResponse<bool>($"Error resetting password: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.InternalServerError_500, false);
                }

                string subject = "ASDPRS System - Password Reset";
                string htmlContent = $@"
                <html>
                <body>
                    <h2>Password Reset Request</h2>
                    <p>Dear {user.FirstName} {user.LastName},</p>
                    <p>Your password has been reset successfully.</p>
                    <p><strong>New Password:</strong> {newPassword}</p>
                    <p>Please log in and change your password immediately for security reasons.</p>
                    <br>
                    <p>Best regards,<br>ASDPRS Team</p>
                </body>
                </html>";

                await _emailService.SendEmail(user.Email, subject, htmlContent);

                return new BaseResponse<bool>("New password has been sent to your email.", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error processing request: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<bool>> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);
                }

                user.IsActive = false;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return new BaseResponse<bool>($"Error deactivating user: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, false);
                }

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
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);
                }

                user.IsActive = true;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return new BaseResponse<bool>($"Error activating user: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, false);
                }

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
                var users = await _userManager.Users.ToListAsync();

                var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
                var studentCount = (await _userManager.GetUsersInRoleAsync("Student")).Count;
                var instructorCount = (await _userManager.GetUsersInRoleAsync("Instructor")).Count;

                var response = new AccountStatisticsResponse
                {
                    TotalAccounts = users.Count,
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

        private async Task<bool> SendWelcomeEmail(User user, string password, string role)
        {
            try
            {
                string subject;
                string htmlContent;

                if (role == "Instructor")
                {
                    subject = "Welcome to ASDPRS System - Instructor Account Created";
                    htmlContent = $@"
                <html>
                <body>
                    <h2>Welcome to ASDPRS System</h2>
                    <p>Dear {user.FirstName} {user.LastName},</p>
                    <p>Your instructor account has been successfully created in the ASDPRS system.</p>
                    <p><strong>You can now login using Google authentication with your email:</strong> {user.Email}</p>
                    <p>Simply click on the Google login button on the login page and use your Google account associated with this email.</p>
                    <br>
                    <p><strong>No password is required for Google login.</strong></p>
                    <br>
                    <p>If you have any issues, please contact the system administrator.</p>
                    <br>
                    <p>Best regards,<br>ASDPRS Team</p>
                </body>
                </html>";
                }
                else
                {
                    subject = "Welcome to ASDPRS System - Your Account Credentials";
                    htmlContent = $@"
                <html>
                <body>
                    <h2>Welcome to ASDPRS System</h2>
                    <p>Dear {user.FirstName} {user.LastName},</p>
                    <p>Your account has been successfully created by the administrator.</p>
                    <p><strong>Username:</strong> {user.UserName}</p>
                    <p><strong>Password:</strong> {password}</p>
                    <p>Please log in and change your password as soon as possible for security reasons.</p>
                    <br>
                    <p>Best regards,<br>ASDPRS Team</p>
                </body>
                </html>";
                }

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

                // Check email exists
                var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
                if (existingUserByEmail != null)
                {
                    return new BaseResponse<UserResponse>("User with this email already exists", StatusCodeEnum.Conflict_409, null);
                }

                // Check username exists
                var existingUserByUsername = await _userManager.FindByNameAsync(request.Username);
                if (existingUserByUsername != null)
                {
                    return new BaseResponse<UserResponse>("User with this username already exists", StatusCodeEnum.Conflict_409, null);
                }

                // Check student code exists
                if (!string.IsNullOrWhiteSpace(request.StudentCode))
                {
                    var existingStudent = await _context.Users.FirstOrDefaultAsync(u => u.StudentCode == request.StudentCode);
                    if (existingStudent != null)
                        if (request.Role == "Instructor")
                        {
                            request.StudentCode = $"INS_{DateTime.Now:yyyyMMddHHmmssfff}";
                        }
                        else if (request.Role == "Student")
                        {
                            return new BaseResponse<UserResponse>("A user with this StudentCode already exists", StatusCodeEnum.Conflict_409, null);
                        }
                        else
                        {
                            return new BaseResponse<UserResponse>("StudentCode is required for students", StatusCodeEnum.BadRequest_400, null);
                        }
                }

                var user = _mapper.Map<User>(request);

                // Generate random password if not provided
                string password = string.IsNullOrEmpty(request.Password)
                    ? GenerateStrongPassword()
                    : request.Password;

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    return new BaseResponse<UserResponse>($"Error creating user: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, null);
                }

                // Assign student role by default
                if (string.IsNullOrEmpty(request.Role))
                {
                    request.Role = "Student";
                }

                var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
                if (!roleResult.Succeeded)
                {
                    // If role assignment fails, delete the user
                    await _userManager.DeleteAsync(user);
                    return new BaseResponse<UserResponse>($"Error assigning role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, null);
                }

                // Send welcome email với role
                await SendWelcomeEmail(user, password, request.Role);

                var response = _mapper.Map<UserResponse>(user);
                return new BaseResponse<UserResponse>("User created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<UserResponse>($"Error creating user: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(request.Username);
                if (user == null)
                {
                    return new BaseResponse<LoginResponse>("Invalid username or password", StatusCodeEnum.Unauthorized_401, null);
                }

                if (!user.IsActive)
                {
                    return new BaseResponse<LoginResponse>("Account is deactivated", StatusCodeEnum.Unauthorized_401, null);
                }

                var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordValid)
                {
                    return new BaseResponse<LoginResponse>("Invalid username or password", StatusCodeEnum.Unauthorized_401, null);
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Generate token
                var token = await _tokenService.CreateToken(user);

                var response = new LoginResponse
                {
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken,
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    StudentCode = user.StudentCode,
                    Roles = roles.ToList(),
                    CampusId = user.CampusId
                };

                Console.WriteLine($"DEBUG: LoginResponse = {System.Text.Json.JsonSerializer.Serialize(response)}");

                return new BaseResponse<LoginResponse>("Login successful", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<LoginResponse>($"Error during login: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> AssignRolesAsync(AssignRoleRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                {
                    return new BaseResponse<bool>("User not found", StatusCodeEnum.NotFound_404, false);
                }

                // Get current roles
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remove current roles
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return new BaseResponse<bool>($"Error removing current roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, false);
                }

                // Add new roles
                var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (!addResult.Succeeded)
                {
                    return new BaseResponse<bool>($"Error assigning new roles: {string.Join(", ", addResult.Errors.Select(e => e.Description))}", StatusCodeEnum.BadRequest_400, false);
                }

                return new BaseResponse<bool>("Roles assigned successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error assigning roles: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<string>>> GetUserRolesAsync(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return new BaseResponse<IEnumerable<string>>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                var roles = await _userManager.GetRolesAsync(user);
                return new BaseResponse<IEnumerable<string>>("Roles retrieved successfully", StatusCodeEnum.OK_200, roles);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<string>>($"Error retrieving roles: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        private string GenerateStrongPassword()
        {
            const int length = 10; 
            const string lowers = "abcdefghijklmnopqrstuvwxyz";
            const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string specials = "@#$%^&*!";

            var random = new Random();
            var passwordChars = new List<char>();

            passwordChars.Add(lowers[random.Next(lowers.Length)]);
            passwordChars.Add(uppers[random.Next(uppers.Length)]);
            passwordChars.Add(digits[random.Next(digits.Length)]);
            passwordChars.Add(specials[random.Next(specials.Length)]);

            string allChars = lowers + uppers + digits + specials;
            for (int i = passwordChars.Count; i < length; i++)
            {
                passwordChars.Add(allChars[random.Next(allChars.Length)]);
            }

            return new string(passwordChars.OrderBy(x => random.Next()).ToArray());
        }

        public async Task<BaseResponse<List<UserResponse>>> ImportUsersFromExcelAsync(Stream fileStream)
        {
            try
            {
                var importedUsers = new List<UserResponse>();
                using (var package = new ExcelPackage(fileStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                        return new BaseResponse<List<UserResponse>>("File is empty or missing headers", StatusCodeEnum.BadRequest_400, null);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        // Mapping based on your file: Index(1), Code(2), Surname(3), Middle(4), Given(5), Email(6), Major(7), Role(8), Campus(9)
                        var code = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var surname = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                        var middleName = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                        var givenName = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                        var email = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                        var majorCode = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                        var roleName = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                        var campusVal = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(roleName)) continue;

                        // Check if user exists
                        var existingUser = await _userManager.FindByEmailAsync(email);
                        if (existingUser != null) continue;

                        if (!int.TryParse(campusVal, out int campusId)) campusId = 1;

                        int? majorId = null;
                        if (!string.IsNullOrEmpty(majorCode))
                        {
                            var major = await _context.Majors.FirstOrDefaultAsync(m => m.MajorCode == majorCode);
                            if (major != null) majorId = major.MajorId;
                        }

                        var newUser = new User
                        {
                            UserName = email.Split('@')[0],
                            Email = email,
                            StudentCode = code,
                            FirstName = givenName,
                            LastName = $"{surname} {middleName}".Trim(),
                            CampusId = campusId,
                            MajorId = majorId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        string password = GenerateStrongPassword();
                        var result = await _userManager.CreateAsync(newUser, password);

                        if (result.Succeeded)
                        {
                            // Assign Role
                            if (await _roleManager.RoleExistsAsync(roleName))
                            {
                                await _userManager.AddToRoleAsync(newUser, roleName);
                            }
                            else
                            {
                                await _userManager.AddToRoleAsync(newUser, "Student");
                            }

                            _ = SendWelcomeEmail(newUser, password, roleName);

                            importedUsers.Add(_mapper.Map<UserResponse>(newUser));
                        }
                    }
                }

                return new BaseResponse<List<UserResponse>>($"Successfully imported {importedUsers.Count} users", StatusCodeEnum.Created_201, importedUsers);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<UserResponse>>($"Error importing users: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}