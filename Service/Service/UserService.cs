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

                string newPassword = GenerateStrongPassword();

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!result.Succeeded)
                {
                    return new BaseResponse<bool>($"Error resetting password: {string.Join(", ", result.Errors.Select(e => e.Description))}", StatusCodeEnum.InternalServerError_500, false);
                }

                string subject = "FASM System - Password Reset Successful";

                string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset - FASM</title>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            color: #333333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f7;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0,0,0,0.08);
        }}
        .header {{
            background-color: #ffffff;
            padding: 25px;
            text-align: center;
            border-bottom: 1px solid #eeeeee;
        }}
        .header img {{
            max-width: 150px;
            height: auto;
        }}
        .body-content {{
            padding: 40px 30px;
        }}
        .status-icon {{
            text-align: center;
            font-size: 40px;
            margin-bottom: 10px;
        }}
        .title {{
            color: #2c3e50;
            font-size: 22px;
            font-weight: bold;
            text-align: center;
            margin-bottom: 25px;
        }}
        .password-box {{
            background-color: #fff9e6;
            border: 1px solid #ffeeba;
            border-radius: 8px;
            padding: 20px;
            text-align: center;
            margin: 25px 0;
        }}
        .password-label {{
            font-size: 14px;
            color: #856404;
            margin-bottom: 5px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .password-value {{
            font-family: 'Courier New', Courier, monospace;
            font-size: 24px;
            font-weight: bold;
            color: #333;
            letter-spacing: 2px;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background-color: #28a745; /* Màu xanh lá đại diện cho thành công */
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 6px;
            font-weight: bold;
            margin: 10px 0;
        }}
        .security-note {{
            background-color: #fdf2f2;
            padding: 15px;
            border-radius: 6px;
            font-size: 13px;
            color: #a94442;
            margin-top: 25px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #777777;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://res.cloudinary.com/daf34jpxn/image/upload/v1766342084/FASM_ss094j.png"" alt=""FASM Logo"">
        </div>

        <div class=""body-content"">
            <div class=""status-icon"">🔐</div>
            <div class=""title"">Password Reset Successful</div>
    
            <p>Dear <strong>{user.FirstName} {user.LastName}</strong>,</p>
            
            <p>We have received a request to reset your password.
            Your password has been updated successfully. You can now use the temporary password below to log in:</p>
            
            <div class=""password-box"">
                <div class=""password-label"">Your New Password</div>
                <div class=""password-value"">{newPassword}</div>
            </div>

            <p style=""text-align: center;"">
                <a href=""https://fasm-fpt.site/login"" class=""button"">Log In to FASM</a>
            </p>

            <div class=""security-note"">
                <strong>Important:</strong> For security reasons, you 
                are required to change this password immediately after logging in. If you did not request this change, please contact our support team right away.
            </div>

            <p style=""margin-top: 30px; font-size: 14px;"">
                Best regards,<br>
                <strong>FASM Team</strong>
            </p>
        </div>

        <div class=""footer"">
            &copy; 2025 FASM System. All rights reserved.<br>
            This is an automated security notification.
        </div>
    </div>
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
                    subject = "Welcome to FASM System - Instructor Account Created";
                    htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to FASM</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f7;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0,0,0,0.05);
        }}
        .header {{
            background-color: #ffffff;
            padding: 30px 20px;
            text-align: center;
            border-bottom: 1px solid #eeeeee;
        }}
        .header img {{
            max-width: 180px;
            height: auto;
        }}
        .body-content {{
            padding: 40px 30px;
        }}
        .welcome-text {{
            font-size: 22px;
            color: #2c3e50;
            margin-bottom: 20px;
            font-weight: bold;
        }}
        .highlight-box {{
            background-color: #f8f9fa;
            border-left: 4px solid #007bff;
            padding: 15px;
            margin: 20px 0;
        }}
        .button {{
            display: inline-block;
            padding: 12px 25px;
            background-color: #007bff;
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            margin-top: 10px;
        }}
        .footer {{
            background-color: #f4f4f7;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #777777;
        }}
        .info-text {{
            font-size: 14px;
            color: #555555;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://res.cloudinary.com/daf34jpxn/image/upload/v1766342084/FASM_ss094j.png"" alt=""FASM Logo"">
        </div>

        <div class=""body-content"">
            <div class=""welcome-text"">Welcome to FASM System</div>
            
            <p class=""info-text"">Dear <strong>{user.FirstName} {user.LastName}</strong>,</p>
     
            <p class=""info-text"">Your instructor account has been successfully created in the <strong>FASM system</strong>.
            We are excited to have you on board!</p>
            
            <div class=""highlight-box"">
                <p class=""info-text"" style=""margin: 0;"">
                    <strong>Login Email:</strong> {user.Email}<br>
                    <strong>Method:</strong> Google Authentication
                </p>
            </div>

            <p class=""info-text"">Simply click on the Google login button on the login page and use your Google account associated with this email.</p>
            
            <p style=""text-align: center;"">
                <a href=""https://fasm-fpt.site/login"" class=""button"">Go to Login Page</a>
            </p>

            <p class=""info-text"" style=""color: #e67e22; font-weight: bold;"">
                * No password is required for Google login.
            </p>

            <p class=""info-text"" style=""margin-top: 30px;"">
                If you have any issues, please contact the system administrator.<br><br>
                Best regards,<br>
                <strong>FASM Team</strong>
            </p>
        </div>

        <div class=""footer"">
            &copy; 2025 FASM System. All rights reserved.<br>
            This is an automated email, please do not reply.
        </div>
    </div>
</body>
</html>";
                }
                else
                {
                    subject = "Welcome to FASM System - Your Account Credentials";
                    htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Account Created - FASM</title>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            color: #333333;
            margin: 0;
            padding: 0;
            background-color: #f9f9f9;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: #ffffff;
            border: 1px solid #eeeeee;
            border-radius: 12px;
            overflow: hidden;
        }}
        .header {{
            background-color: #ffffff;
            padding: 25px;
            text-align: center;
            border-bottom: 2px solid #f0f0f0;
        }}
        .header img {{
            max-width: 150px;
            height: auto;
        }}
        .content {{
            padding: 35px;
        }}
        .title {{
            color: #1a73e8;
            font-size: 22px;
            font-weight: bold;
            margin-bottom: 20px;
        }}
        .credentials-card {{
            background-color: #f0f7ff;
            border-radius: 8px;
            padding: 20px;
            margin: 25px 0;
            border: 1px dashed #1a73e8;
        }}
        .credentials-item {{
            margin: 5px 0;
            font-size: 15px;
        }}
        .button {{
            display: inline-block;
            padding: 14px 30px;
            background-color: #1a73e8;
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            margin: 10px 0;
        }}
        .warning {{
            color: #d93025;
            font-size: 13px;
            font-style: italic;
            margin-top: 15px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #70757a;
            border-top: 1px solid #eeeeee;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://res.cloudinary.com/daf34jpxn/image/upload/v1766342084/FASM_ss094j.png"" alt=""FASM Logo"">
        </div>

        <div class=""content"">
            <div class=""title"">Welcome to FASM System</div>
            
            <p>Dear <strong>{user.FirstName} {user.LastName}</strong>,</p>
            
            <p>Your student account has been automatically created following your successful course enrollment.
            Below are your login credentials:</p>
            
            <div class=""credentials-card"">
                <div class=""credentials-item""><strong>Username:</strong> <code>{user.UserName}</code></div>
                <div class=""credentials-item""><strong>Password:</strong> <code>{password}</code></div>
            </div>

            <p style=""text-align: center;"">
                <a href=""https://fasm-fpt.site/login"" class=""button"">Login to Your Account</a>
            </p>

            <p class=""warning"">
                ⚠️ For security reasons, please log in and change your password immediately after your first access.
            </p>

            <p style=""margin-top: 30px;"">
                Best regards,<br>
                <strong>FASM Team</strong>
            </p>
        </div>

        <div class=""footer"">
            © 2025 FASM System. All rights reserved.<br>
            If you did not expect this email, please ignore it or contact support.
        </div>
    </div>
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
                            CreatedAt = DateTime.UtcNow.AddHours(7)
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
        public async Task<BaseResponse<List<UserResponse>>> ImportStudentsFromExcelAsync(Stream fileStream)
        {
            return await ImportUsersInternalAsync(fileStream, "Student");
        }

        public async Task<BaseResponse<List<UserResponse>>> ImportInstructorsFromExcelAsync(Stream fileStream)
        {
            return await ImportUsersInternalAsync(fileStream, "Instructor");
        }

        private async Task<BaseResponse<List<UserResponse>>> ImportUsersInternalAsync(Stream fileStream, string targetRole)
        {
            try
            {
                var importedUsers = new List<UserResponse>();
                var errors = new List<string>();
                var warnings = new List<string>();

                using (var package = new ExcelPackage(fileStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                        return new BaseResponse<List<UserResponse>>("File is empty or missing headers", StatusCodeEnum.BadRequest_400, null);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        // Mapping: Index(1), Code(2), Surname(3), Middle(4), Given(5), Email(6), Major(7), Role(8), Campus(9)
                        var code = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var surname = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                        var middleName = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                        var givenName = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                        var email = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                        var majorCode = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                        var excelRole = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                        var campusVal = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(email)) continue;

                        if (!string.IsNullOrEmpty(excelRole) && !string.Equals(excelRole, targetRole, StringComparison.OrdinalIgnoreCase))
                        {
                            warnings.Add($"Row {row}: Skipped {email} because role '{excelRole}' does not match target '{targetRole}'.");
                            continue;
                        }

                        if (!int.TryParse(campusVal, out int campusId)) campusId = 1;

                        int? majorId = null;
                        if (targetRole == "Instructor")
                        {
                            majorId = null;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(majorCode))
                            {
                                errors.Add($"Row {row}: Major code is required for Students ({email}).");
                                continue;
                            }
                            var major = await _context.Majors.FirstOrDefaultAsync(m => m.MajorCode == majorCode);
                            if (major == null)
                            {
                                errors.Add($"Row {row}: Major code '{majorCode}' not found.");
                                continue;
                            }
                            majorId = major.MajorId;
                        }

                        var user = await _userManager.FindByEmailAsync(email);
                        bool isNew = false;

                        if (user != null)
                        {
                            if (!string.IsNullOrEmpty(code) && user.StudentCode != code)
                            {
                                errors.Add($"Row {row}: Cannot change StudentCode for user {email}. Existing: '{user.StudentCode}', Input: '{code}'.");
                                continue;
                            }

                            user.FirstName = givenName;
                            user.LastName = $"{surname} {middleName}".Trim();
                            user.CampusId = campusId;

                            if (!await _userManager.IsInRoleAsync(user, "Instructor"))
                            {
                                user.MajorId = majorId;
                            }

                            var updateResult = await _userManager.UpdateAsync(user);
                            if (!updateResult.Succeeded)
                            {
                                errors.Add($"Row {row}: Failed to update {email} - {updateResult.Errors.First().Description}");
                                continue;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(code))
                            {
                                var duplicateCodeUser = await _context.Users.FirstOrDefaultAsync(u => u.StudentCode == code);
                                if (duplicateCodeUser != null)
                                {
                                    errors.Add($"Row {row}: StudentCode '{code}' already exists.");
                                    continue;
                                }
                            }

                            user = new User
                            {
                                UserName = email.Split('@')[0],
                                Email = email,
                                StudentCode = code,
                                FirstName = givenName,
                                LastName = $"{surname} {middleName}".Trim(),
                                CampusId = campusId,
                                MajorId = majorId, // Set Major here
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow.AddHours(7)
                            };

                            string password = GenerateStrongPassword();
                            var result = await _userManager.CreateAsync(user, password);

                            if (result.Succeeded)
                            {
                                isNew = true;
                                await _userManager.AddToRoleAsync(user, targetRole);
                                _ = SendWelcomeEmail(user, password, targetRole);

                                if (majorId.HasValue && user.MajorId != majorId)
                                {
                                    user.MajorId = majorId;
                                    await _context.SaveChangesAsync();
                                }
                            }
                            else
                            {
                                errors.Add($"Row {row}: Error creating user - {string.Join(", ", result.Errors.Select(e => e.Description))}");
                                continue;
                            }
                        }

                        var response = _mapper.Map<UserResponse>(user);

                        response.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                        if (majorId.HasValue && string.IsNullOrEmpty(response.MajorName))
                        {
                            var m = await _context.Majors.FindAsync(majorId);
                            response.MajorName = m?.MajorName;
                        }

                        importedUsers.Add(response);
                    }
                }

                string message = $"Processed users successfully.";
                if (importedUsers.Count > 0) message += $" Total: {importedUsers.Count}.";
                if (warnings.Any()) message += $" Warnings: {warnings.Count}.";
                if (errors.Any()) message += $" Errors: {errors.Count}.";

                return new BaseResponse<List<UserResponse>>(message, StatusCodeEnum.Created_201, importedUsers);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<UserResponse>>($"Error importing users: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}