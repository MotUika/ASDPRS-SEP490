using BussinessObject.Models;
using DataAccessLayer;
using Google.Apis.Auth;
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
            Description = "Thu hồi refresh token, xóa cookies, và kết thúc phiên đăng nhập (bao gồm Identity và external schemes)"
        )]
        [SwaggerResponse(200, "Đăng xuất thành công")]
        [SwaggerResponse(401, "Chưa đăng nhập")]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _tokenService.RevokeRefreshToken(userId);

            await _signInManager.SignOutAsync();

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            Response.Cookies.Delete("ASDPRS_Access", new CookieOptions { Secure = true, SameSite = SameSiteMode.None });
            Response.Cookies.Delete("ASDPRS_Refresh", new CookieOptions { Secure = true, SameSite = SameSiteMode.None });

            Response.Cookies.Delete(".AspNetCore.Identity.Application", new CookieOptions { Secure = true, SameSite = SameSiteMode.None });

            return Ok(new BaseResponse<string>("Logged out successfully", StatusCodeEnum.OK_200, null));
        }

        [HttpPost("google-login-mobile")]
        [SwaggerOperation(
    Summary = "Đăng nhập Google cho mobile",
    Description = "Nhận ID Token từ Google Sign-In SDK trên mobile, verify và trả access/refresh token trong response body"
)]
        [SwaggerResponse(200, "Đăng nhập thành công", typeof(BaseResponse<LoginResponse>))]
        [SwaggerResponse(400, "ID Token không hợp lệ")]
        public async Task<IActionResult> GoogleLoginMobile([FromBody] GoogleMobileLoginRequest request)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
                if (payload == null)
                    return BadRequest(new BaseResponse<string>("Invalid Google ID Token", StatusCodeEnum.BadRequest_400, null));

                var googleEmail = payload.Email;
                if (string.IsNullOrEmpty(googleEmail))
                    return BadRequest(new BaseResponse<string>("No email from Google", StatusCodeEnum.BadRequest_400, null));

                var user = await _userManager.FindByEmailAsync(googleEmail);
                if (user == null)
                    return BadRequest(new BaseResponse<string>("User not found", StatusCodeEnum.BadRequest_400, null));

                if (!user.IsActive)
                    return BadRequest(new BaseResponse<string>("Account inactive", StatusCodeEnum.BadRequest_400, null));

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Instructor"))
                    return BadRequest(new BaseResponse<string>("Only instructors allowed", StatusCodeEnum.BadRequest_400, null));

                var tokenResponse = await _tokenService.CreateToken(user);
                if (tokenResponse == null)
                    return BadRequest(new BaseResponse<string>("Failed to create token", StatusCodeEnum.BadRequest_400, null));

                return Ok(new BaseResponse<LoginResponse>(
                    "Login successful",
                    StatusCodeEnum.OK_200,
                    new LoginResponse
                    {
                        AccessToken = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken,
                    }
                ));
            }
            catch (Exception ex)
            {
                return BadRequest(new BaseResponse<string>($"Error: {ex.Message}", StatusCodeEnum.BadRequest_400, null));
            }
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
        [SwaggerResponse(302, "Chuyển hướng về frontend với token hoặc lỗi trong URL")]
        public async Task<IActionResult> GoogleCallback()
        {
            string returnUrl = "http://localhost:5173/";

            IActionResult RedirectError(string errCode, string msg)
            {
                var safeMessage = Uri.EscapeDataString(msg ?? "Unknown error");
                return Redirect($"{returnUrl}?error={errCode}&message={safeMessage}");
            }

            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return RedirectError("auth_info_null", "Google authentication info is null (correlation/state failed)");
                }

                // Restore returnUrl
                if (info.AuthenticationProperties != null && info.AuthenticationProperties.Items.ContainsKey("returnUrl"))
                {
                    returnUrl = info.AuthenticationProperties.Items["returnUrl"];
                }

                // Validate returnUrl
                var allowedOrigins = new[]
                {
            "http://localhost:5173", // FE
            "https://localhost:7104" // BE test
        };
                try
                {
                    var ruri = new Uri(returnUrl);
                    var origin = $"{ruri.Scheme}://{ruri.Host}{(ruri.IsDefaultPort ? "" : ":" + ruri.Port)}";
                    if (!allowedOrigins.Contains(origin))
                    {
                        returnUrl = "http://localhost:5173/";
                    }
                }
                catch
                {
                    returnUrl = "http://localhost:5173/";
                }

                // Get email
                var googleEmail = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(googleEmail))
                    return RedirectError("no_email", "Could not get email from Google");

                var user = await _userManager.FindByEmailAsync(googleEmail);
                if (user == null)
                    return RedirectError("user_not_found", "This Google account is not registered in the system.");

                if (!user.IsActive)
                    return RedirectError("inactive", "Account is deactivated");

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Instructor"))
                    return RedirectError("unauthorized_role", "Only instructors can login with Google");

                // Generate tokens
                var tokenResponse = await _tokenService.CreateToken(user);
                if (tokenResponse == null)
                    return RedirectError("token_failed", "Failed to create authentication token");

                await _signInManager.SignInAsync(user, new AuthenticationProperties { IsPersistent = true });
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                // Cookies
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    Path = "/"
                };
                Response.Cookies.Append("ASDPRS_Access", tokenResponse.AccessToken, cookieOptions);
                Response.Cookies.Append("ASDPRS_Refresh", tokenResponse.RefreshToken, cookieOptions);

                // Success redirect to FE
                var redirectUrl =
                    $"{returnUrl}?accessToken={Uri.EscapeDataString(tokenResponse.AccessToken)}" +
                    $"&refreshToken={Uri.EscapeDataString(tokenResponse.RefreshToken)}";

                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                return RedirectError("exception", $"Error during Google login: {ex.Message}");
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

            // Lấy thông tin user từ UserService
            var result = await _userService.GetUserByIdAsync(userId);
            if (!result.StatusCode.ToString().StartsWith("2"))
                return StatusCode((int)result.StatusCode, result);

            // Lấy đối tượng User từ UserManager để lấy roles
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return StatusCode(401, new BaseResponse<UserResponse>(
                    "User not found",
                    StatusCodeEnum.Unauthorized_401,
                    null
                ));
            }

            // Sử dụng UserManager để lấy roles thay vì UserRoles navigation property
            var roles = await _userManager.GetRolesAsync(user);

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