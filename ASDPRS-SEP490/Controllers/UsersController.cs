using BussinessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Notification;
using Service.RequestAndResponse.Request.User;
using Service.RequestAndResponse.Response.User;
using Service.RequestAndResponse.Response.User.Service.RequestAndResponse.Response.User;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý người dùng hệ thống: CRUD, tìm kiếm, thống kê tài khoản")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _announcementService;

        public UsersController(IUserService userService, UserManager<User> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
        Summary = "Lấy thông tin người dùng theo ID",
        Description = "Trả về thông tin chi tiết của người dùng dựa trên ID được cung cấp"
    )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<UserResponse>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> GetUser(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id}/detail")]
        [SwaggerOperation(
        Summary = "Lấy thông tin chi tiết người dùng theo ID",
        Description = "Trả về thông tin chi tiết đầy đủ của người dùng bao gồm lịch sử lớp học, điểm số, submissions, reviews dựa trên ID được cung cấp"
    )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<UserDetailResponse>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            var result = await _userService.GetUserByIdDetailAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Lấy danh sách tất cả người dùng",
        Description = "Trả về danh sách toàn bộ người dùng trong hệ thống (chỉ dành cho Admin)"
    )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<UserResponse>>))]
        [SwaggerResponse(403, "Không có quyền truy cập")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Tạo người dùng mới",
        Description = "Tạo tài khoản người dùng mới với vai trò được chỉ định. Mật khẩu sẽ được gửi qua email nếu không được cung cấp"
    )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<UserResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(409, "Email hoặc username đã tồn tại")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _userService.CreateUserAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Cập nhật thông tin người dùng",
        Description = "Cập nhật thông tin cá nhân của người dùng. Người dùng chỉ có thể cập nhật thông tin của chính mình, trừ khi là Admin"
    )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<UserResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ hoặc ID không khớp")]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            if (id != request.UserId)
                return BadRequest(new { message = "User ID mismatch" });

            var result = await _userService.UpdateUserAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Xóa người dùng (không khuyến khích xài)",
        Description = "Xóa vĩnh viễn người dùng khỏi hệ thống (chỉ dành cho Admin)"
    )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("email/{email}")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Tìm người dùng theo email",
        Description = "Tìm kiếm thông tin người dùng dựa trên địa chỉ email"
    )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<UserResponse>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var result = await _userService.GetUserByEmailAsync(email);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("username/{username}")]
        [Authorize]
        [SwaggerOperation(
        Summary = "Tìm người dùng theo username",
        Description = "Tìm kiếm thông tin người dùng dựa trên username"
    )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<UserResponse>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var result = await _userService.GetUserByUsernameAsync(username);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("role/{roleName}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Lấy danh sách người dùng theo vai trò",
        Description = "Trả về danh sách tất cả người dùng có vai trò được chỉ định"
    )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<UserResponse>>))]
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
            var result = await _userService.GetUsersByRoleAsync(roleName);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("campus/{campusId}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Lấy danh sách người dùng theo campus",
        Description = "Trả về danh sách người dùng thuộc campus được chỉ định"
    )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<UserResponse>>))]
        public async Task<IActionResult> GetUsersByCampus(int campusId)
        {
            var result = await _userService.GetUsersByCampusAsync(campusId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}/avatar")]
        [Authorize]
        [Authorize]
        [SwaggerOperation(
        Summary = "Cập nhật avatar người dùng",
        Description = "Cập nhật URL avatar cho người dùng"
    )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<string>))]
        [SwaggerResponse(400, "URL avatar không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
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
        [SwaggerOperation(
        Summary = "Đổi mật khẩu người dùng",
        Description = "Đổi mật khẩu cho người dùng (cần xác thực mật khẩu hiện tại)"
    )]
        [SwaggerResponse(200, "Đổi mật khẩu thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(400, "Mật khẩu hiện tại không đúng")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            request.UserId = id;
            var result = await _userService.ChangePasswordAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        Summary = "Vô hiệu hóa tài khoản người dùng (khuyến khích xài)",
        Description = "Vô hiệu hóa tài khoản người dùng, ngăn không cho đăng nhập"
    )]
        [SwaggerResponse(200, "Vô hiệu hóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var result = await _userService.DeactivateUserAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Kích hoạt tài khoản người dùng",
            Description = "Kích hoạt lại tài khoản người dùng đã bị vô hiệu hóa"
        )]
        [SwaggerResponse(200, "Kích hoạt thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var result = await _userService.ActivateUserAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Thống kê tài khoản",
            Description = "Lấy số liệu thống kê về tổng số tài khoản theo từng vai trò"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<AccountStatisticsResponse>))]
        public async Task<IActionResult> GetAccountStatistics()
        {
            var result = await _userService.GetTotalAccountsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("instructor-email")]
        [SwaggerOperation(
            Summary = "Thêm instructor bằng email",
            Description = "Tạo tài khoản instructor mới bằng địa chỉ email (mật khẩu mặc định sẽ được gửi qua email)"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<UserResponse>))]
        [SwaggerResponse(400, "Email đã tồn tại trong hệ thống")]
        [SwaggerResponse(409, "Xung đột dữ liệu")]
        public async Task<IActionResult> AddInstructorEmail([FromBody] string email, string firstName, string LastName, int campus)
        {

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return BadRequest(new BaseResponse<UserResponse>(
                    "Email already exists in system",
                    StatusCodeEnum.Conflict_409,
                    null));
            }

            var createRequest = new CreateUserRequest
            {
                Password = "123456789BCDAaA@",
                Email = email,
                Username = email.Split('@')[0],
                FirstName = firstName,
                LastName = LastName,
                CampusId = campus,
                IsActive = true,
                Role = "Instructor"
            };

            var result = await _userService.CreateUserAsync(createRequest);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("{id}/roles")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Gán vai trò cho người dùng",
            Description = "Gán các vai trò mới cho người dùng (sẽ ghi đè lên các vai trò cũ)"
        )]
        [SwaggerResponse(200, "Gán vai trò thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> AssignRoles(int id, [FromBody] AssignRoleRequest request)
        {
            request.UserId = id;
            var result = await _userService.AssignRolesAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id}/roles")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Lấy danh sách vai trò của người dùng",
            Description = "Trả về danh sách các vai trò của người dùng theo ID"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<string>>))]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        public async Task<IActionResult> GetUserRoles(int id)
        {
            var result = await _userService.GetUserRolesAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("send-to-all")]
        [SwaggerOperation(
           Summary = "Gửi thông báo đến tất cả người dùng",
           Description = "Gửi thông báo đến tất cả người dùng trong hệ thống"
       )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Unauthorized - Token không hợp lệ")]
        [SwaggerResponse(403, "Forbidden - Không có quyền truy cập")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> SendToAll([FromBody] SendAnnouncementRequest request)
        {
            try
            {
                var result = await _announcementService.SendAnnouncementToAllAsync(request);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse<bool>(
                    $"Lỗi server: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false
                ));
            }
        }

        [HttpPost("send-to-course/{courseInstanceId}")]
        [SwaggerOperation(
            Summary = "Gửi thông báo đến khóa học cụ thể",
            Description = "Gửi thông báo đến tất cả sinh viên và giảng viên trong một khóa học cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Unauthorized - Token không hợp lệ")]
        [SwaggerResponse(403, "Forbidden - Không có quyền truy cập")]
        [SwaggerResponse(404, "Không tìm thấy khóa học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> SendToCourse(int courseInstanceId, [FromBody] SendAnnouncementRequest request)
        {
            try
            {
                var result = await _announcementService.SendAnnouncementToCourseAsync(request, courseInstanceId);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse<bool>(
                    $"Lỗi server: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false
                ));
            }
        }

        [HttpPost("send-to-users")]
        [SwaggerOperation(
            Summary = "Gửi thông báo đến danh sách người dùng",
            Description = "Gửi thông báo đến danh sách người dùng cụ thể theo ID"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(401, "Unauthorized - Token không hợp lệ")]
        [SwaggerResponse(403, "Forbidden - Không có quyền truy cập")]
        [SwaggerResponse(404, "Không tìm thấy người dùng")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> SendToUsers([FromBody] SendAnnouncementToUsersRequest request)
        {
            try
            {
                var result = await _announcementService.SendAnnouncementToUsersAsync(request.AnnouncementRequest, request.UserIds);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse<bool>(
                    $"Lỗi server: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false
                ));
            }
        }
    }
}
