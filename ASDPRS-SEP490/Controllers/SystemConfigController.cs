using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.SystemConfig;
using Service.RequestAndResponse.Response.SystemConfig;
using Swashbuckle.AspNetCore.Annotations;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    [SwaggerTag("Quản lý cấu hình hệ thống (Admin only)")]
    public class SystemConfigController : ControllerBase
    {
        private readonly ISystemConfigService _systemConfigService;

        public SystemConfigController(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy tất cả cấu hình hệ thống",
            Description = "Trả về danh sách tất cả cấu hình hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<SystemConfigResponse>>))]
        public async Task<IActionResult> GetAllConfigs()
        {
            var result = await _systemConfigService.GetAllConfigsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{key}")]
        [SwaggerOperation(
            Summary = "Lấy cấu hình theo key",
            Description = "Trả về thông tin cấu hình hệ thống theo key"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SystemConfigResponse>))]
        [SwaggerResponse(404, "Không tìm thấy cấu hình")]
        public async Task<IActionResult> GetConfigByKey(string key)
        {
            var result = await _systemConfigService.GetConfigByKeyAsync(key);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật cấu hình hệ thống",
            Description = "Cập nhật giá trị cấu hình hệ thống (chỉ cho phép các config có sẵn)"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<SystemConfigResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy cấu hình")]
        public async Task<IActionResult> UpdateConfig([FromBody] UpdateSystemConfigRequest request)
        {
            var userId = GetCurrentUserId();
            request.UpdatedByUserId = userId;

            var result = await _systemConfigService.UpdateConfigAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("important-configs")]
        [SwaggerOperation(
            Summary = "Lấy các cấu hình quan trọng",
            Description = "Trả về các cấu hình quan trọng cho hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<ImportantConfigsResponse>))]
        public async Task<IActionResult> GetImportantConfigs()
        {
            try
            {
                var scorePrecision = await _systemConfigService.GetConfigValueAsync("ScorePrecision", 0.5m);
                var aiSummaryMaxTokens = await _systemConfigService.GetConfigValueAsync("AISummaryMaxTokens", 1000);
                var aiSummaryMaxWords = await _systemConfigService.GetConfigValueAsync("AISummaryMaxWords", 200);
                var defaultPassThreshold = await _systemConfigService.GetConfigValueAsync("DefaultPassThreshold", 50m);

                var response = new ImportantConfigsResponse
                {
                    ScorePrecision = scorePrecision,
                    AISummaryMaxTokens = aiSummaryMaxTokens,
                    AISummaryMaxWords = aiSummaryMaxWords,
                    DefaultPassThreshold = defaultPassThreshold
                };

                return Ok(new BaseResponse<ImportantConfigsResponse>(
                    "Important configs retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse<ImportantConfigsResponse>(
                    $"Error retrieving important configs: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null));
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
}
