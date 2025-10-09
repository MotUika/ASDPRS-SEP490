using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Semester;
using Service.RequestAndResponse.Response.Semester;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý Học kỳ (Semester)")]
    public class SemesterController : ControllerBase
    {
        private readonly ISemesterService _semesterService;

        public SemesterController(ISemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        // Lấy chi tiết 1 semester
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết một học kỳ theo ID",
            Description = "Trả về thông tin chi tiết của một học kỳ dựa trên ID được cung cấp."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SemesterResponse>))]
        [SwaggerResponse(404, "Không tìm thấy học kỳ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetSemesterById(int id)
        {
            var result = await _semesterService.GetSemesterByIdAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy tất cả semester
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả học kỳ",
            Description = "Trả về một danh sách chứa tất cả các học kỳ trong hệ thống."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<SemesterResponse>>))]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetAllSemesters()
        {
            var result = await _semesterService.GetAllSemestersAsync();
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy semester theo AcademicYear
        [HttpGet("academic-year/{academicYearId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách học kỳ theo năm học",
            Description = "Trả về một danh sách các học kỳ thuộc về một năm học cụ thể."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<SemesterResponse>>))]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetSemestersByAcademicYear(int academicYearId)
        {
            var result = await _semesterService.GetSemestersByAcademicYearAsync(academicYearId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Tạo semester mới
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo một học kỳ mới",
            Description = "Tạo một học kỳ mới trong hệ thống dựa trên thông tin được cung cấp."
        )]
        [SwaggerResponse(201, "Tạo mới thành công", typeof(BaseResponse<SemesterResponse>))]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _semesterService.CreateSemesterAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetSemesterById), new { id = result.Data?.SemesterId }, result),
                _ => StatusCode(500, result)
            };
        }

        // Cập nhật semester
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin học kỳ",
            Description = "Cập nhật thông tin của một học kỳ đã tồn tại."
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<SemesterResponse>))]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy học kỳ để cập nhật")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> UpdateSemester([FromBody] UpdateSemesterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _semesterService.UpdateSemesterAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Xóa semester (theo id)
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa một học kỳ",
            Description = "Xóa một học kỳ khỏi hệ thống dựa trên ID."
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy học kỳ để xóa")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> DeleteSemester(int id)
        {
            var result = await _semesterService.DeleteSemesterAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
