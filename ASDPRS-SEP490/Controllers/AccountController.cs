using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.User;
using Service.RequestAndResponse.Response.User;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/account")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý xác thực người dùng: đăng nhập, đăng xuất, refresh token, đăng nhập Google")]
    public class AccountController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ASDPRSContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(IUserService userService, ITokenService tokenService, ASDPRSContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userService = userService;
            _tokenService = tokenService;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("login")]
        [SwaggerOperation(
        Summary = "Đăng nhập vào hệ thống",
        Description = "Xác thực người dùng bằng username và password, trả về access token và refresh token"
    )]
        [SwaggerResponse(200, "Đăng nhập thành công", typeof(BaseResponse<LoginResponse>))]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(401, "Sai tên đăng nhập hoặc mật khẩu")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _userService.LoginAsync(request);

            // Trả về luôn BaseResponse
            return StatusCode((int)result.StatusCode, result);
        }



        [HttpPost("refresh-token")]
        [SwaggerOperation(
        Summary = "Làm mới access token",
        Description = "Sử dụng refresh token để lấy access token mới khi token cũ hết hạn"
    )]
        [SwaggerResponse(200, "Làm mới token thành công")]
        [SwaggerResponse(400, "Refresh token không hợp lệ")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            try
            {
                var response = await _tokenService.RefreshToken(refreshToken);
                if (response == null || !response.Success)
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
        [SwaggerOperation(
        Summary = "Đăng xuất khỏi hệ thống",
        Description = "Thu hồi refresh token của người dùng và kết thúc phiên đăng nhập"
    )]
        [SwaggerResponse(200, "Đăng xuất thành công")]
        [SwaggerResponse(401, "Chưa đăng nhập")]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _tokenService.RevokeRefreshToken(userId);
            return Ok("Logged out successfully");
        }

        [HttpGet("google-login")]
        [SwaggerOperation(
                Summary = "Đăng nhập bằng Google",
                Description = "Chuyển hướng đến Google OAuth để xác thực người dùng"
            )]
        [SwaggerResponse(302, "Chuyển hướng đến trang đăng nhập Google")]
        public IActionResult GoogleLogin([FromQuery] string returnUrl = "http://localhost:5173/")
        {
            // Force https for callback generation (ensure Google redirect_uri is registered)
            var redirectUri = Url.Action("GoogleCallback", "Account", null, "https");

            var props = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUri);

            // store returnUrl so we'll redirect to it after completing external login
            props.Items["returnUrl"] = returnUrl ?? "http://localhost:5173/";

            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        [SwaggerOperation(
        Summary = "Callback xác thực Google",
        Description = "Endpoint callback từ Google OAuth sau khi xác thực thành công, tạo token và trả về frontend"
    )]
        [SwaggerResponse(302, "Chuyển hướng về frontend với token trong cookie")]
        [SwaggerResponse(400, "Xác thực Google thất bại")]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return BadRequest(new { message = "Google authentication info is null (correlation/state failed)." });
                }

                // Read returnUrl (saved during GoogleLogin)
                string returnUrl = "http://localhost:5173/";
                if (info.AuthenticationProperties != null && info.AuthenticationProperties.Items.ContainsKey("returnUrl"))
                {
                    returnUrl = info.AuthenticationProperties.Items["returnUrl"];
                }

                // Validate returnUrl origin against allow-list to avoid open redirect
                var allowedOrigins = new[]
                {
                    "http://localhost:5173", // FE (update for prod)
                    "https://localhost:7104"  // BE (so you can test BE-only)
                };

                try
                {
                    var ruri = new Uri(returnUrl);
                    var origin = $"{ruri.Scheme}://{ruri.Host}{(ruri.IsDefaultPort ? "" : ":" + ruri.Port)}";
                    if (!allowedOrigins.Contains(origin))
                    {
                        // fallback to safe default
                        returnUrl = "http://localhost:5173/";
                    }
                }
                catch
                {
                    returnUrl = "http://localhost:5173/";
                }

                // Get claims returned by Google
                var googleEmail = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
                var givenName = info.Principal.FindFirst(ClaimTypes.GivenName)?.Value;
                var surname = info.Principal.FindFirst(ClaimTypes.Surname)?.Value;
                var googleId = info.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(googleEmail))
                    return BadRequest(new { message = "Could not get email from Google" });

                // find or create user
                var user = await _userManager.FindByEmailAsync(googleEmail);
                if (user == null)
                {
                    return BadRequest(new { message = "Tài khoản Google này chưa được đăng ký trong hệ thống." });
                }

                if (!user.IsActive)
                    return BadRequest(new { message = "Account is deactivated" });

                // Ensure role Instructor (your policy)
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Instructor"))
                    return BadRequest(new { message = "Only instructors can login with Google" });

                // create tokens
                var tokenResponse = await _tokenService.CreateToken(user);
                if (tokenResponse == null)
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to create tokens" });

                // IMPORTANT: Sign in with Identity cookie for browser sessions
                await _signInManager.SignInAsync(user, new AuthenticationProperties { IsPersistent = true });

                // Clear external cookie
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                // Set HttpOnly secure cookies (FE will use credentials: 'include')
                var accessCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    Path = "/"
                };
                Response.Cookies.Append("ASDPRS_Access", tokenResponse.AccessToken, accessCookieOptions);

                var refreshCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    Path = "/"
                };
                Response.Cookies.Append("ASDPRS_Refresh", tokenResponse.RefreshToken, refreshCookieOptions);

                // redirect to frontend (or backend test URL)
                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error during Google login: {ex.Message}" });
            }
        }

        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [SwaggerOperation(Summary = "Lấy thông tin người dùng hiện tại", Description = "Lấy thông tin người dùng đang đăng nhập, roles và tokens của họ")]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<object>))]
        [SwaggerResponse(401, "Chưa đăng nhập")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst(ClaimTypes.Name)
                ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return StatusCode(401, new BaseResponse<UserResponse>(
                    "Unable to determine user id from token",
                    StatusCodeEnum.Unauthorized_401,
                    null
                ));
            }

            // Lấy thông tin user
            var result = await _userService.GetUserByIdAsync(userId);
            if (!result.StatusCode.ToString().StartsWith("2"))
                return StatusCode((int)result.StatusCode, result);

            // Lấy roles của user
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();

            // Lấy token từ cookie (nếu đang test bằng Google Login flow)
            var accessToken = Request.Cookies["ASDPRS_Access"];
            var refreshToken = Request.Cookies["ASDPRS_Refresh"];

            // Nếu không có cookie (ví dụ test Postman bằng Bearer token) → tạo lại token mới
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                var tokenResponse = await _tokenService.CreateToken(user);
                accessToken = tokenResponse.AccessToken;
                refreshToken = tokenResponse.RefreshToken;
            }

            // Trả về user + role + token
            var response = new
            {
                User = result.Data,
                Roles = roles,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            return Ok(new BaseResponse<object>(
                "Lấy thông tin người dùng thành công",
                StatusCodeEnum.OK_200,
                response
            ));
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