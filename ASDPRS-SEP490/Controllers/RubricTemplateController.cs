using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.RubricTemplate;
using Service.RequestAndResponse.Response.RubricTemplate;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý Rubric Template: CRUD, tìm kiếm, lọc theo người tạo hoặc public")]
    public class RubricTemplateController : ControllerBase
    {
        private readonly IRubricTemplateService _rubricTemplateService;

        public RubricTemplateController(IRubricTemplateService rubricTemplateService)
        {
            _rubricTemplateService = rubricTemplateService;
        }

        // 🔹 GET: Lấy Rubric Template theo ID
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin Rubric Template theo ID",
            Description = "Trả về thông tin chi tiết của Rubric Template dựa trên ID được cung cấp"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RubricTemplateResponse>))]
        [SwaggerResponse(404, "Không tìm thấy Rubric Template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetRubricTemplateById(int id)
        {
            var result = await _rubricTemplateService.GetRubricTemplateByIdAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 GET: Lấy tất cả Rubric Template
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả Rubric Template",
            Description = "Trả về danh sách toàn bộ Rubric Template trong hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricTemplateResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAllRubricTemplates()
        {
            var result = await _rubricTemplateService.GetAllRubricTemplatesAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 GET: Lấy Rubric Template theo UserId
        [HttpGet("user/{userId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách Rubric Template theo người tạo",
            Description = "Trả về danh sách Rubric Template được tạo bởi user cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricTemplateResponse>>))]
        [SwaggerResponse(404, "Không tìm thấy user hoặc không có template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetRubricTemplatesByUserId(int userId)
        {
            var result = await _rubricTemplateService.GetRubricTemplatesByUserIdAsync(userId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 GET: Lấy Rubric Template Public
        [HttpGet("public")]
        [SwaggerOperation(
            Summary = "Lấy danh sách Rubric Template public",
            Description = "Trả về danh sách Rubric Template được chia sẻ công khai"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricTemplateResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetPublicRubricTemplates()
        {
            var result = await _rubricTemplateService.GetPublicRubricTemplatesAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 POST: Tạo Rubric Template mới
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo Rubric Template mới",
            Description = "Tạo mới một Rubric Template cho user cụ thể"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<RubricTemplateResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ hoặc tiêu đề trùng lặp")]
        [SwaggerResponse(404, "Không tìm thấy user")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateRubricTemplate([FromBody] CreateRubricTemplateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rubricTemplateService.CreateRubricTemplateAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetRubricTemplateById), new { id = result.Data?.TemplateId }, result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 PUT: Cập nhật Rubric Template
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật Rubric Template",
            Description = "Cập nhật thông tin tiêu đề hoặc trạng thái public của Rubric Template"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<RubricTemplateResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ hoặc tiêu đề trùng lặp")]
        [SwaggerResponse(404, "Không tìm thấy Rubric Template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateRubricTemplate([FromBody] UpdateRubricTemplateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rubricTemplateService.UpdateRubricTemplateAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 DELETE: Xóa Rubric Template
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa Rubric Template",
            Description = "Xóa Rubric Template theo ID (chỉ khi chưa có Rubric hoặc Criteria Template liên quan)"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(400, "Không thể xóa Rubric Template có dữ liệu liên quan")]
        [SwaggerResponse(404, "Không tìm thấy Rubric Template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteRubricTemplate(int id)
        {
            var result = await _rubricTemplateService.DeleteRubricTemplateAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 (Optional) GET: Tìm kiếm Rubric Template theo từ khóa
        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Tìm kiếm Rubric Template",
            Description = "Tìm kiếm Rubric Template theo tiêu đề hoặc tên người tạo"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricTemplateResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> SearchRubricTemplates([FromQuery] string searchTerm)
        {
            var result = await _rubricTemplateService.SearchRubricTemplatesAsync(searchTerm);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // GET: Lấy Rubric Template theo UserId và MajorId
        [HttpGet("major/{majorId}/user/{userId}")]
        [SwaggerOperation(
            Summary = "Lấy Rubric Template theo Major và User",
            Description = "Trả về danh sách Rubric Template công khai thuộc Major cụ thể, và template riêng của user (nếu có)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricTemplateResponse>>))]
        [SwaggerResponse(400, "MajorId hoặc UserId không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy template nào cho Major và User này")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetRubricTemplatesByUserAndMajor(int majorId, int userId)
        {
            if (majorId <= 0 || userId <= 0)
                return BadRequest(new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    "MajorId and UserId must be greater than 0",
                    StatusCodeEnum.BadRequest_400,
                    null));

            var result = await _rubricTemplateService.GetRubricTemplatesByUserAndMajorAsync(userId, majorId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
