using BussinessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.User;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id}")]
        /*[Authorize]*/
        public async Task<IActionResult> GetUser(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet]
        /*[Authorize(Roles = "Admin")]*/
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _userService.CreateUserAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            if (id != request.UserId)
                return BadRequest(new { message = "User ID mismatch" });

            var result = await _userService.UpdateUserAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("email/{email}")]
        [Authorize]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var result = await _userService.GetUserByEmailAsync(email);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("username/{username}")]
        [Authorize]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var result = await _userService.GetUserByUsernameAsync(username);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("role/{roleName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
            var result = await _userService.GetUsersByRoleAsync(roleName);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("campus/{campusId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersByCampus(int campusId)
        {
            var result = await _userService.GetUsersByCampusAsync(campusId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}/avatar")]
        [Authorize]
        public async Task<IActionResult> UpdateAvatar(int id, [FromBody] string avatarUrl)
        {
            var request = new UpdateUserAvatarRequest
            {
                UserId = id,
                AvatarUrl = avatarUrl
            };

            var result = await _userService.UpdateUserAvatarAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            request.UserId = id;
            var result = await _userService.ChangePasswordAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var result = await _userService.DeactivateUserAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var result = await _userService.ActivateUserAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAccountStatistics()
        {
            var result = await _userService.GetTotalAccountsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("instructor-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddInstructorEmail([FromBody] string email)
        {
            if (!email.EndsWith("@fpt.edu.vn"))
                return BadRequest(new { message = "Only @fpt.edu.vn emails are allowed" });

            var existingUser = await _userService.GetUserByEmailAsync(email);
            if (existingUser.StatusCode.ToString().StartsWith("2"))
                return BadRequest(new { message = "Email already exists in system" });

            var createRequest = new CreateUserRequest
            {
                Email = email,
                Username = email.Split('@')[0],
                FirstName = "Instructor",
                LastName = "FPT",
                CampusId = 1,
                IsActive = true,
                Role = "Instructor"
            };

            var result = await _userService.CreateUserAsync(createRequest);
            return StatusCode((int)result.StatusCode, result);
        }
        [HttpGet("test-auth")]
        [Authorize] // Chỉ cần authenticated
        public IActionResult TestAuth()
        {
            var userInfo = new
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                UserId = User.FindFirst("userId")?.Value,
                UserName = User.Identity.Name,
                // ✅ SỬA: Dùng "role" thay vì ClaimTypes.Role
                Roles = User.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList(),
                AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            return Ok(userInfo);
        }

        [HttpGet("test-admin")]
        [Authorize(Roles = "Admin")] // Sẽ tìm claim với type "role" và value "Admin"
        public IActionResult TestAdmin()
        {
            return Ok("Admin access successful!");
        }

        [HttpGet("test-roles")]
        public IActionResult TestRoles()
        {
            // Kiểm tra tất cả các cách để lấy roles
            var roleClaims1 = User.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
            var roleClaims2 = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var roleClaims3 = User.Claims.Where(c => c.Type.EndsWith("role")).Select(c => c.Value).ToList();

            var isInRoleAdmin = User.IsInRole("Admin");

            return Ok(new
            {
                RolesFromRoleClaim = roleClaims1,
                RolesFromClaimTypesRole = roleClaims2,
                RolesFromAnyRole = roleClaims3,
                IsInRoleAdmin = isInRoleAdmin,
                AllClaims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value })
            });
        }
    }
}
