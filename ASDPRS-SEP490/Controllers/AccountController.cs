using DataAccessLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.IService;
using Service.RequestAndResponse.Request.User;
using Service.RequestAndResponse.BaseResponse;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ASDPRSContext _context;

        public AccountController(IUserService userService, ITokenService tokenService, ASDPRSContext context)
        {
            _userService = userService;
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _userService.LoginAsync(request);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            try
            {
                var response = await _tokenService.RefreshToken(refreshToken);
                if (response == null || !response.Success) // Kiểm tra theo cấu trúc ApiResponse của bạn
                {
                    return BadRequest("Invalid refresh token");
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error refreshing token: {ex.Message}");
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _tokenService.RevokeRefreshToken(userId);
            return Ok("Logged out successfully");
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = "/api/account/google-callback" };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
                return BadRequest("Google authentication failed");

            var email = authenticateResult.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var givenName = authenticateResult.Principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var surname = authenticateResult.Principal.FindFirst(ClaimTypes.Surname)?.Value;

            if (!email.EndsWith("@fpt.edu.vn"))
            {
                return BadRequest("Only @fpt.edu.vn emails are allowed for instructor login");
            }

            var userResponse = await _userService.GetUserByEmailAsync(email);
            if (!userResponse.StatusCode.ToString().StartsWith("2"))
            {
                return BadRequest("User not found in system. Please contact administrator to add your email.");
            }

            var user = userResponse.Data;

            var userWithRoles = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == user.UserId);

            var isInstructor = userWithRoles.UserRoles.Any(ur => ur.Role.RoleName == "Instructor");
            if (!isInstructor)
            {
                return BadRequest("Only instructors can login with Google");
            }

            var token = await _tokenService.CreateToken(userWithRoles);

            return Ok(new
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = userWithRoles.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _userService.GetUserByIdAsync(userId);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }

            // Sửa lại phần này - không cần gán Roles vào UserResponse
            var userWithRoles = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var response = new
            {
                result.Data,
                Roles = userWithRoles.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            };

            return Ok(response);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            request.UserId = userId;

            var result = await _userService.ChangePasswordAsync(request);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }
    }
}