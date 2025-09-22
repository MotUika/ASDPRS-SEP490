using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.User;
using Service.RequestAndResponse.BaseResponse;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using BussinessObject.IdentityModel;

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
        [Authorize]
        public async Task<IActionResult> GetUser(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _userService.CreateUserAsync(request);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return CreatedAtAction(nameof(GetUser), new { id = result.Data.UserId }, result.Data);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            if (id != request.UserId)
            {
                return BadRequest("User ID mismatch");
            }

            var result = await _userService.UpdateUserAsync(request);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpGet("email/{email}")]
        [Authorize]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var result = await _userService.GetUserByEmailAsync(email);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpGet("username/{username}")]
        [Authorize]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var result = await _userService.GetUserByUsernameAsync(username);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpGet("role/{roleName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
            var result = await _userService.GetUsersByRoleAsync(roleName);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpGet("campus/{campusId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersByCampus(int campusId)
        {
            var result = await _userService.GetUsersByCampusAsync(campusId);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
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
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpPut("{id}/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            request.UserId = id;
            var result = await _userService.ChangePasswordAsync(request);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var result = await _userService.DeactivateUserAsync(id);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var result = await _userService.ActivateUserAsync(id);
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAccountStatistics()
        {
            var result = await _userService.GetTotalAccountsAsync();
            if (!result.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)result.StatusCode, result.Message);
            }
            return Ok(result.Data);
        }

        [HttpPost("instructor-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddInstructorEmail([FromBody] string email)
        {
            try
            {
                if (!email.EndsWith("@fpt.edu.vn"))
                {
                    return BadRequest("Only @fpt.edu.vn emails are allowed");
                }

                // Check if email already exists
                var existingUser = await _userService.GetUserByEmailAsync(email);
                if (existingUser.StatusCode.ToString().StartsWith("2"))
                {
                    return BadRequest("Email already exists in system");
                }

                // Create a new user with instructor role
                var createRequest = new CreateUserRequest
                {
                    Email = email,
                    Username = email.Split('@')[0], // Use the part before @ as username
                    FirstName = "Instructor",
                    LastName = "FPT",
                    CampusId = 1, // Default campus
                    IsActive = true
                };

                var result = await _userService.CreateUserAsync(createRequest);

                if (!result.StatusCode.ToString().StartsWith("2"))
                {
                    return StatusCode((int)result.StatusCode, result.Message);
                }

                // Note: In a real implementation, you would also assign the instructor role here
                // This would require adding a method to your UserService to assign roles

                return Ok($"Instructor email {email} added successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding instructor email: {ex.Message}");
            }
        }
    }
}