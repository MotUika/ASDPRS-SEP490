using DataAccessLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.IService;
using System.Security.Claims;
using System.Threading.Tasks;

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

            // Get user information from Google
            var email = authenticateResult.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var givenName = authenticateResult.Principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var surname = authenticateResult.Principal.FindFirst(ClaimTypes.Surname)?.Value;

            // Validate domain
            if (!email.EndsWith("@fpt.edu.vn"))
            {
                return BadRequest("Only @fpt.edu.vn emails are allowed for instructor login");
            }

            // Check if user exists in our database
            var userResponse = await _userService.GetUserByEmailAsync(email);

            if (!userResponse.StatusCode.ToString().StartsWith("2"))
            {
                return BadRequest("User not found in system. Please contact administrator to add your email.");
            }

            var user = userResponse.Data;

            // Check if user has instructor role
            var userWithRoles = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            var isInstructor = userWithRoles.UserRoles.Any(ur => ur.Role.RoleName == "Instructor");

            if (!isInstructor)
            {
                return BadRequest("Only instructors can login with Google");
            }

            // Generate JWT token
            var token = await _tokenService.CreateToken(userWithRoles);

            // Return token to user
            return Ok(new
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }
    }
}