using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Course;
using Service.RequestAndResponse.Response.Course;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý môn học: CRUD, tìm kiếm theo chương trình đào tạo và mã môn học")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin môn học theo ID",
            Description = "Trả về thông tin chi tiết của môn học dựa trên ID được cung cấp, bao gồm số lượng course instance"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CourseResponse>))]
        [SwaggerResponse(404, "Không tìm thấy môn học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseById(int id)
        {
            var result = await _courseService.GetCourseByIdAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả môn học",
            Description = "Trả về danh sách toàn bộ môn học trong hệ thống với đầy đủ thông tin"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAllCourses()
        {
            var result = await _courseService.GetAllCoursesAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo môn học mới",
            Description = "Tạo một môn học mới với thông tin được cung cấp. Cần có curriculumId, mã môn học, tên môn học và số tín chỉ"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<CourseResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseService.CreateCourseAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetCourseById), new { id = result.Data?.CourseId }, result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin môn học",
            Description = "Cập nhật thông tin của môn học đã tồn tại trong hệ thống"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<CourseResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy môn học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateCourse([FromBody] UpdateCourseRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseService.UpdateCourseAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa môn học",
            Description = "Xóa môn học khỏi hệ thống dựa trên ID. Lưu ý: Chỉ có thể xóa môn học chưa có course instance"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy môn học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var result = await _courseService.DeleteCourseAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("curriculum/{curriculumId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách môn học theo chương trình đào tạo",
            Description = "Trả về danh sách các môn học thuộc chương trình đào tạo được chỉ định"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCoursesByCurriculum(int curriculumId)
        {
            var result = await _courseService.GetCoursesByCurriculumAsync(curriculumId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("code/{courseCode}")]
        [SwaggerOperation(
            Summary = "Tìm kiếm môn học theo mã môn học",
            Description = "Tìm kiếm các môn học dựa trên mã môn học (có thể tìm kiếm partial match)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCoursesByCode(string courseCode)
        {
            var result = await _courseService.GetCoursesByCodeAsync(courseCode);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }
    }
}