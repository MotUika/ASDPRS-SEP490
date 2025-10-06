using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseInstructor;
using Service.RequestAndResponse.Response.CourseInstructor;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý giảng viên lớp học: gán, xóa, xem danh sách giảng viên theo lớp hoặc theo người dạy")]
    public class CourseInstructorController : ControllerBase
    {
        private readonly ICourseInstructorService _courseInstructorService;

        public CourseInstructorController(ICourseInstructorService courseInstructorService)
        {
            _courseInstructorService = courseInstructorService;
        }

        // 🔹 Lấy chi tiết 1 CourseInstructor
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin chi tiết mối quan hệ giữa lớp học và giảng viên",
            Description = "Trả về thông tin cụ thể về việc giảng viên nào đang dạy lớp nào"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CourseInstructorResponse>))]
        [SwaggerResponse(404, "Không tìm thấy thông tin")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseInstructorById(int id)
        {
            var result = await _courseInstructorService.GetCourseInstructorByIdAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Lấy danh sách giảng viên trong một lớp học
        [HttpGet("course-instance/{courseInstanceId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách giảng viên trong một lớp học",
            Description = "Trả về tất cả giảng viên đang dạy lớp học được chỉ định"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseInstructorResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseInstructorsByCourseInstance(int courseInstanceId)
        {
            var result = await _courseInstructorService.GetCourseInstructorsByCourseInstanceAsync(courseInstanceId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Lấy danh sách lớp học mà một giảng viên đang dạy
        [HttpGet("instructor/{instructorId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách lớp học mà một giảng viên đang dạy",
            Description = "Trả về danh sách tất cả lớp học mà giảng viên này được gán"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseInstructorResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseInstructorsByInstructor(int instructorId)
        {
            var result = await _courseInstructorService.GetCourseInstructorsByInstructorAsync(instructorId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Gán một giảng viên vào lớp học
        [HttpPost]
        [SwaggerOperation(
            Summary = "Gán giảng viên vào lớp học",
            Description = "Admin thêm một giảng viên cụ thể vào lớp học. Nếu giảng viên đã được gán, sẽ trả về lỗi Conflict (409)"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<CourseInstructorResponse>))]
        [SwaggerResponse(409, "Giảng viên đã được gán vào lớp này")]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateCourseInstructor([FromBody] CreateCourseInstructorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseInstructorService.CreateCourseInstructorAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetCourseInstructorById), new { id = result.Data?.Id }, result),
                StatusCodeEnum.Conflict_409 => Conflict(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Gán nhiều giảng viên cùng lúc vào lớp học
        [HttpPost("bulk-assign")]
        [SwaggerOperation(
            Summary = "Gán nhiều giảng viên vào một lớp học",
            Description = "Admin có thể thêm nhiều giảng viên cùng lúc vào cùng một lớp học. Những người đã tồn tại sẽ được bỏ qua."
        )]
        [SwaggerResponse(201, "Thành công", typeof(BaseResponse<IEnumerable<CourseInstructorResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> BulkAssignInstructors([FromBody] BulkAssignInstructorsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseInstructorService.BulkAssignInstructorsAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => Created("", result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Đổi giảng viên chính trong lớp học
        [HttpPut("{courseInstanceId}/main-instructor/{mainInstructorId}")]
        [SwaggerOperation(
            Summary = "Cập nhật giảng viên chính trong lớp học",
            Description = "Admin chỉ định một giảng viên làm giảng viên chính của lớp học (tính năng đang chờ triển khai)"
        )]
        [SwaggerResponse(501, "Tính năng chưa được hỗ trợ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateMainInstructor(int courseInstanceId, int mainInstructorId)
        {
            var result = await _courseInstructorService.UpdateMainInstructorAsync(courseInstanceId, mainInstructorId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                StatusCodeEnum.NotImplemented_501 => StatusCode(501, result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Xóa giảng viên khỏi lớp học
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa giảng viên khỏi lớp học",
            Description = "Loại bỏ một giảng viên ra khỏi lớp học. Thường chỉ dùng cho admin hoặc quản lý học vụ."
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy bản ghi")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteCourseInstructor(int id)
        {
            var result = await _courseInstructorService.DeleteCourseInstructorAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
