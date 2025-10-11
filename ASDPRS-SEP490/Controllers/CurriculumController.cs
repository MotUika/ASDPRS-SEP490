using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Curriculum;
using Service.RequestAndResponse.Response.Curriculum;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý Chương trình học (Curriculum)")]
    public class CurriculumController : ControllerBase
    {
        private readonly ICurriculumService _curriculumService;

        public CurriculumController(ICurriculumService curriculumService)
        {
            _curriculumService = curriculumService;
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết chương trình học theo ID",
            Description = "Trả về thông tin chi tiết của một chương trình học dựa trên ID được cung cấp."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CurriculumResponse>))]
        [SwaggerResponse(404, "Không tìm thấy chương trình học")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetCurriculumById(int id)
        {
            var result = await _curriculumService.GetCurriculumByIdAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả chương trình học",
            Description = "Trả về một danh sách chứa tất cả các chương trình học trong hệ thống."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CurriculumResponse>>))]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetAllCurriculums()
        {
            var result = await _curriculumService.GetAllCurriculumsAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo một chương trình học mới",
            Description = "Tạo một chương trình học mới trong hệ thống dựa trên thông tin được cung cấp."
        )]
        [SwaggerResponse(201, "Tạo mới thành công", typeof(BaseResponse<CurriculumResponse>))]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> CreateCurriculum([FromBody] CreateCurriculumRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _curriculumService.CreateCurriculumAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetCurriculumById), new { id = result.Data?.CurriculumId }, result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin chương trình học",
            Description = "Cập nhật thông tin của một chương trình học đã tồn tại."
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<CurriculumResponse>))]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy chương trình học để cập nhật")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> UpdateCurriculum([FromBody] UpdateCurriculumRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _curriculumService.UpdateCurriculumAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa một chương trình học",
            Description = "Xóa một chương trình học khỏi hệ thống dựa trên ID."
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy chương trình học để xóa")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> DeleteCurriculum(int id)
        {
            var result = await _curriculumService.DeleteCurriculumAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("campus/{campusId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách chương trình học theo cơ sở (campus)",
            Description = "Trả về danh sách các chương trình học thuộc một cơ sở cụ thể."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CurriculumResponse>>))]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetCurriculumsByCampus(int campusId)
        {
            var result = await _curriculumService.GetCurriculumsByCampusAsync(campusId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("major/{majorId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách chương trình học theo chuyên ngành (major)",
            Description = "Trả về danh sách các chương trình học thuộc một chuyên ngành cụ thể."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CurriculumResponse>>))]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetCurriculumsByMajor(int majorId)
        {
            var result = await _curriculumService.GetCurriculumsByMajorAsync(majorId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
