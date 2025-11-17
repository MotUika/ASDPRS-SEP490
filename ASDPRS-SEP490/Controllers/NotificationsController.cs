using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Hubs;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Notification;
using Service.RequestAndResponse.Response.Notification;
using Swashbuckle.AspNetCore.Annotations;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationsController(INotificationService notificationService, IHubContext<NotificationHub> hubContext)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
    }

    [HttpGet("my-notifications")]
    [SwaggerOperation(
        Summary = "Lấy danh sách thông báo của người dùng hiện tại",
        Description = "Trả về danh sách thông báo của người dùng đang đăng nhập, có thể lọc chỉ lấy thông báo chưa đọc"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<NotificationResponse>>))]
    [SwaggerResponse(401, "Unauthorized - Token không hợp lệ")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetNotificationsByUserAsync(userId, unreadOnly);
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<IEnumerable<NotificationResponse>>(
                $"Lỗi server: {ex.Message}",
                StatusCodeEnum.InternalServerError_500,
                null
            ));
        }
    }

    [HttpPut("{notificationId}/mark-as-read")]
    [SwaggerOperation(
        Summary = "Đánh dấu thông báo là đã đọc",
        Description = "Đánh dấu một thông báo cụ thể là đã đọc"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<bool>))]
    [SwaggerResponse(404, "Không tìm thấy thông báo")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        try
        {
            var result = await _notificationService.MarkAsReadAsync(notificationId);
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

    [HttpPut("mark-all-as-read")]
    [SwaggerOperation(
        Summary = "Đánh dấu tất cả thông báo là đã đọc",
        Description = "Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<bool>))]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.MarkAllAsReadAsync(userId);
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

    [HttpGet("unread-count")]
    [SwaggerOperation(
        Summary = "Lấy số lượng thông báo chưa đọc",
        Description = "Trả về số lượng thông báo chưa đọc của người dùng hiện tại"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<int>))]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetNotificationsByUserAsync(userId, true);

            var count = notifications.Data?.Count() ?? 0;
            return Ok(new BaseResponse<int>(
                "Unread count retrieved successfully",
                StatusCodeEnum.OK_200,
                count
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<int>(
                $"Lỗi server: {ex.Message}",
                StatusCodeEnum.InternalServerError_500,
                0
            ));
        }
    }

    [HttpPost("test-notification")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Gửi thông báo test (Admin only)",
        Description = "Gửi một thông báo test đến người dùng hiện tại để kiểm tra hệ thống"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<NotificationResponse>))]
    [SwaggerResponse(403, "Forbidden - Không có quyền Admin")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> SendTestNotification()
    {
        try
        {
            var userId = GetCurrentUserId();
            var request = new CreateNotificationRequest
            {
                UserId = userId,
                Title = "Test Notification",
                Message = "This is a test notification to verify the real-time system is working properly.",
                Type = "Test"
            };

            var result = await _notificationService.CreateNotificationAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<NotificationResponse>(
                $"Lỗi server: {ex.Message}",
                StatusCodeEnum.InternalServerError_500,
                null
            ));
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}