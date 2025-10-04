using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Rubric;
using Service.RequestAndResponse.Response.Rubric;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [SwaggerTag("Quản lý Rubric: CRUD, tạo rubric từ template, quản lý criteria")]
    public class RubricController : ControllerBase
    {
        private readonly IRubricService _rubricService;

        public RubricController(IRubricService rubricService)
        {
            _rubricService = rubricService;
        }

        /// <summary>
        /// Lấy thông tin rubric theo ID
        /// </summary>
        /// <param name="id">ID của rubric</param>
        /// <returns>Thông tin rubric chi tiết</returns>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin rubric theo ID",
            Description = "Trả về thông tin chi tiết của rubric dựa trên ID được cung cấp"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RubricResponse>))]
        [SwaggerResponse(404, "Không tìm thấy rubric")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetRubricById(int id)
        {
            var result = await _rubricService.GetRubricByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách tất cả rubric
        /// </summary>
        /// <returns>Danh sách rubric</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả rubric",
            Description = "Trả về danh sách toàn bộ rubric trong hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAllRubrics()
        {
            var result = await _rubricService.GetAllRubricsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Tạo rubric mới
        /// </summary>
        /// <param name="request">Thông tin tạo rubric</param>
        /// <returns>Rubric vừa được tạo</returns>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo rubric mới",
            Description = "Tạo một rubric mới với thông tin được cung cấp"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<RubricResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy template hoặc assignment")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateRubric([FromBody] CreateRubricRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rubricService.CreateRubricAsync(request);

            if (result.StatusCode == StatusCodeEnum.Created_201)
                return CreatedAtAction(nameof(GetRubricById), new { id = result.Data?.RubricId }, result);

            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật thông tin rubric
        /// </summary>
        /// <param name="request">Thông tin cập nhật rubric</param>
        /// <returns>Rubric đã được cập nhật</returns>
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin rubric",
            Description = "Cập nhật thông tin của rubric đã tồn tại trong hệ thống"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<RubricResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy rubric")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateRubric([FromBody] UpdateRubricRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rubricService.UpdateRubricAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Xóa rubric theo ID
        /// </summary>
        /// <param name="id">ID của rubric cần xóa</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa rubric theo ID",
            Description = "Xóa rubric khỏi hệ thống dựa trên ID. Lưu ý: Chỉ có thể xóa rubric chưa có criteria"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(400, "Không thể xóa rubric có criteria")]
        [SwaggerResponse(404, "Không tìm thấy rubric")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteRubric(int id)
        {
            var result = await _rubricService.DeleteRubricAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách rubric theo Template ID
        /// </summary>
        /// <param name="templateId">ID của template</param>
        /// <returns>Danh sách rubric thuộc template</returns>
        [HttpGet("template/{templateId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách rubric theo Template ID",
            Description = "Trả về danh sách các rubric thuộc về một template cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetRubricsByTemplateId(int templateId)
        {
            var result = await _rubricService.GetRubricsByTemplateIdAsync(templateId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách rubric theo Assignment ID
        /// </summary>
        /// <param name="assignmentId">ID của assignment</param>
        /// <returns>Danh sách rubric thuộc assignment</returns>
        [HttpGet("assignment/{assignmentId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách rubric theo Assignment ID",
            Description = "Trả về danh sách các rubric thuộc về một assignment cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetRubricsByAssignmentId(int assignmentId)
        {
            var result = await _rubricService.GetRubricsByAssignmentIdAsync(assignmentId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Lấy rubric kèm danh sách criteria
        /// </summary>
        /// <param name="rubricId">ID của rubric</param>
        /// <returns>Rubric với đầy đủ danh sách criteria</returns>
        [HttpGet("{rubricId}/criteria")]
        [SwaggerOperation(
            Summary = "Lấy rubric kèm danh sách criteria",
            Description = "Trả về thông tin rubric cùng với toàn bộ danh sách criteria thuộc rubric đó"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RubricResponse>))]
        [SwaggerResponse(404, "Không tìm thấy rubric")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetRubricWithCriteria(int rubricId)
        {
            var result = await _rubricService.GetRubricWithCriteriaAsync(rubricId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách rubric đã được chỉnh sửa
        /// </summary>
        /// <returns>Danh sách rubric đã modified</returns>
        [HttpGet("modified")]
        [SwaggerOperation(
            Summary = "Lấy danh sách rubric đã được chỉnh sửa",
            Description = "Trả về danh sách các rubric đã được đánh dấu là đã chỉnh sửa (IsModified = true)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetModifiedRubrics()
        {
            var result = await _rubricService.GetModifiedRubricsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Tạo rubric mới từ template
        /// </summary>
        /// <param name="templateId">ID của template</param>
        /// <param name="assignmentId">ID của assignment (optional)</param>
        /// <returns>Rubric vừa được tạo từ template</returns>
        [HttpPost("template/{templateId}")]
        [SwaggerOperation(
            Summary = "Tạo rubric mới từ template",
            Description = "Tạo một rubric mới dựa trên template có sẵn, bao gồm cả việc sao chép các criteria từ template"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<RubricResponse>))]
        [SwaggerResponse(404, "Không tìm thấy template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateRubricFromTemplate(
            int templateId,
            [FromQuery] int? assignmentId = null)
        {
            var result = await _rubricService.CreateRubricFromTemplateAsync(templateId, assignmentId);

            if (result.StatusCode == StatusCodeEnum.Created_201)
                return CreatedAtAction(nameof(GetRubricById), new { id = result.Data?.RubricId }, result);

            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Tìm kiếm rubric theo tiêu đề
        /// </summary>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách rubric phù hợp</returns>
        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Tìm kiếm rubric theo tiêu đề",
            Description = "Tìm kiếm các rubric dựa trên tiêu đề (có thể tìm kiếm partial match)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<RubricResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> SearchRubrics([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new BaseResponse<IEnumerable<RubricResponse>>("Search term is required", StatusCodeEnum.BadRequest_400, null));
            }

            // Note: You'll need to add SearchRubricsAsync method to your service
            // For now, we'll use existing methods and filter in memory
            var allResult = await _rubricService.GetAllRubricsAsync();

            if (allResult.StatusCode != StatusCodeEnum.OK_200)
                return StatusCode((int)allResult.StatusCode, allResult);

            var filteredData = allResult.Data?.Where(r =>
                r.Title.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase) ||
                r.TemplateTitle?.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase) == true ||
                r.AssignmentTitle?.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase) == true
            );

            var response = new BaseResponse<IEnumerable<RubricResponse>>(
                "Rubrics retrieved successfully",
                StatusCodeEnum.OK_200,
                filteredData
            );

            return Ok(response);
        }
    }
}