using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Response.CourseStudent;
using Swashbuckle.AspNetCore.Annotations;
using System.IO;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý sinh viên trong lớp học: đăng ký, import Excel, cập nhật điểm, trạng thái")]
    public class CourseStudentController : ControllerBase
    {
        private readonly ICourseStudentService _courseStudentService;

        public CourseStudentController(ICourseStudentService courseStudentService)
        {
            _courseStudentService = courseStudentService;
        }

        // Lấy chi tiết 1 CourseStudent
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin chi tiết sinh viên trong lớp",
            Description = "Trả về thông tin chi tiết của một bản ghi CourseStudent theo ID"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CourseStudentResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bản ghi")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseStudentById(int id)
        {
            var result = await _courseStudentService.GetCourseStudentByIdAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy danh sách SV trong lớp
        [HttpGet("course-instance/{courseInstanceId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách sinh viên trong lớp học",
            Description = "Trả về danh sách tất cả sinh viên đã đăng ký trong một lớp học cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<CourseStudentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseStudentsByCourseInstance(int courseInstanceId)
        {
            var result = await _courseStudentService.GetCourseStudentsByCourseInstanceAsync(courseInstanceId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy danh sách lớp của SV
        [HttpGet("student/{studentId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách lớp của sinh viên",
            Description = "Trả về danh sách tất cả lớp học mà một sinh viên đã đăng ký"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<CourseStudentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseStudentsByStudent(int studentId)
        {
            var result = await _courseStudentService.GetCourseStudentsByStudentAsync(studentId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Thêm sinh viên vào lớp học",
            Description = "Thêm một sinh viên vào lớp học với trạng thái và điểm số được chỉ định"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<CourseStudentResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(409, "Sinh viên đã tồn tại trong lớp")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateCourseStudent([FromBody] CreateCourseStudentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseStudentService.CreateCourseStudentAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetCourseStudentById), new { id = result.Data?.CourseStudentId }, result),
                StatusCodeEnum.Conflict_409 => Conflict(result),
                _ => StatusCode(500, result)
            };
        }

        // SV nhập key để enroll
        [HttpPost("{courseInstanceId}/enroll")]
        [SwaggerOperation(
            Summary = "Sinh viên đăng ký lớp học bằng key",
            Description = "Sinh viên sử dụng enrollment key để đăng ký vào lớp học (kích hoạt từ trạng thái Pending sang Enrolled)"
        )]
        [SwaggerResponse(200, "Đăng ký thành công", typeof(BaseResponse<CourseStudentResponse>))]
        [SwaggerResponse(400, "Key không đúng")]
        [SwaggerResponse(404, "Không tìm thấy sinh viên trong lớp hoặc lớp không tồn tại")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> EnrollStudent(int courseInstanceId, [FromQuery] int studentUserId, [FromQuery] string enrollKey)
        {
            var result = await _courseStudentService.EnrollStudentAsync(courseInstanceId, studentUserId, enrollKey);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Import SV từ Excel
        [HttpPost("{courseInstanceId}/import")]
        [SwaggerOperation(
            Summary = "Import sinh viên từ file Excel",
            Description = "Import danh sách sinh viên vào lớp học từ file Excel. File cần có cột StudentCode và IsRetaking"
        )]
        [SwaggerResponse(201, "Import thành công", typeof(BaseResponse<List<CourseStudentResponse>>))]
        [SwaggerResponse(400, "File không hợp lệ hoặc rỗng")]
        [SwaggerResponse(404, "Không tìm thấy lớp học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> ImportStudentsFromExcel(int courseInstanceId, IFormFile file, [FromQuery] int? changedByUserId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            using var stream = file.OpenReadStream();
            var result = await _courseStudentService.ImportStudentsFromExcelAsync(courseInstanceId, stream, changedByUserId);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => Created("", result),
                _ => StatusCode(500, result)
            };
        }

        // Update trạng thái SV
        [HttpPut("{id}/status")]
        [SwaggerOperation(
            Summary = "Cập nhật trạng thái sinh viên",
            Description = "Cập nhật trạng thái của sinh viên trong lớp (Pending, Enrolled, Completed, Dropped, etc.)"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<CourseStudentResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bản ghi")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateCourseStudentStatus(int id, [FromQuery] string status, [FromQuery] int changedByUserId)
        {
            var result = await _courseStudentService.UpdateCourseStudentStatusAsync(id, status, changedByUserId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Update điểm cuối kỳ
        [HttpPut("{id}/grade")]
        [SwaggerOperation(
            Summary = "Cập nhật điểm số sinh viên",
            Description = "Cập nhật điểm cuối kỳ và trạng thái đậu/rớt của sinh viên"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<CourseStudentResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bản ghi")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateCourseStudentGrade(int id, [FromQuery] decimal? finalGrade, [FromQuery] bool isPassed, [FromQuery] int changedByUserId)
        {
            var result = await _courseStudentService.UpdateCourseStudentGradeAsync(id, finalGrade, isPassed, changedByUserId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Xóa SV khỏi lớp
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa sinh viên khỏi lớp học",
            Description = "Xóa bản ghi CourseStudent, loại bỏ sinh viên khỏi lớp học"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy bản ghi")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteCourseStudent(int id)
        {
            var result = await _courseStudentService.DeleteCourseStudentAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
