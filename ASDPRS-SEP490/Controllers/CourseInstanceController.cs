using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseInstance;
using Service.RequestAndResponse.Response.CourseInstance;
using Service.Service;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý lớp học (Course Instance): CRUD, tìm kiếm theo môn học, kỳ học, campus và cập nhật Enroll Key")]
    public class CourseInstanceController : ControllerBase
    {
        private readonly ICourseInstanceService _courseInstanceService;

        public CourseInstanceController(ICourseInstanceService courseInstanceService)
        {
            _courseInstanceService = courseInstanceService;
        }

        // 🔹 Lấy chi tiết lớp học
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin lớp học theo ID",
            Description = "Trả về thông tin chi tiết của lớp học bao gồm số lượng giảng viên, sinh viên và assignment"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CourseInstanceResponse>))]
        [SwaggerResponse(404, "Không tìm thấy lớp học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseInstanceById(int id)
        {
            var result = await _courseInstanceService.GetCourseInstanceByIdAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Lấy tất cả lớp học
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả lớp học",
            Description = "Trả về danh sách toàn bộ lớp học trong hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseInstanceResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAllCourseInstances()
        {
            var result = await _courseInstanceService.GetAllCourseInstancesAsync();
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Lấy lớp học theo CourseId
        [HttpGet("course/{courseId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách lớp học theo môn học",
            Description = "Trả về danh sách các lớp học thuộc một môn học cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseInstanceResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseInstancesByCourseId(int courseId)
        {
            var result = await _courseInstanceService.GetCourseInstancesByCourseIdAsync(courseId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Lấy lớp học theo SemesterId
        [HttpGet("semester/{semesterId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách lớp học theo kỳ học",
            Description = "Trả về danh sách các lớp học thuộc kỳ học được chỉ định"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseInstanceResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseInstancesBySemesterId(int semesterId)
        {
            var result = await _courseInstanceService.GetCourseInstancesBySemesterIdAsync(semesterId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Lấy lớp học theo CampusId
        [HttpGet("campus/{campusId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách lớp học theo campus",
            Description = "Trả về danh sách các lớp học thuộc campus được chỉ định"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CourseInstanceResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCourseInstancesByCampusId(int campusId)
        {
            var result = await _courseInstanceService.GetCourseInstancesByCampusIdAsync(campusId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Tạo lớp học mới
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo lớp học mới",
            Description = "Admin tạo một lớp học mới. Hệ thống sẽ tự sinh mã Enroll Key ban đầu."
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<CourseInstanceResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateCourseInstance([FromBody] CreateCourseInstanceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseInstanceService.CreateCourseInstanceAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetCourseInstanceById),
                    new { id = result.Data?.CourseInstanceId }, result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Cập nhật lớp học
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin lớp học",
            Description = "Cập nhật thông tin chi tiết của lớp học (chỉ admin)"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<CourseInstanceResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy lớp học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateCourseInstance([FromBody] UpdateCourseInstanceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseInstanceService.UpdateCourseInstanceAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Xóa lớp học
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa lớp học",
            Description = "Xóa lớp học khỏi hệ thống (chỉ admin, chỉ khi chưa có dữ liệu liên quan)"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy lớp học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteCourseInstance(int id)
        {
            var result = await _courseInstanceService.DeleteCourseInstanceAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 🔹 Instructor cập nhật Enroll Key
        [HttpPut("{courseInstanceId}/enroll-key")]
        [SwaggerOperation(
            Summary = "Cập nhật mã Enroll Key của lớp học",
            Description = "Giảng viên của lớp có thể cập nhật mã Enroll Key. Hệ thống kiểm tra quyền trước khi cập nhật."
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<string>))]
        [SwaggerResponse(403, "Không có quyền thay đổi mã lớp")]
        [SwaggerResponse(404, "Không tìm thấy lớp học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateEnrollKey(int courseInstanceId, [FromBody] UpdateEnrollKeyRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewKey))
                return BadRequest("Enroll key không hợp lệ.");

            var result = await _courseInstanceService.UpdateEnrollKeyAsync(courseInstanceId, request.NewKey, request.UserId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.Forbidden_403 => Forbid(result.Message),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("classes-by-user/{userId}")]
        [SwaggerOperation(
    Summary = "Lấy danh sách lớp học theo user",
    Description = "Trả về danh sách lớp (CourseInstance) của user, có thể lọc theo CourseId")]
        [SwaggerResponse(200, "Danh sách lớp học", typeof(BaseResponse<IEnumerable<CourseInstanceResponse>>))]
        [SwaggerResponse(204, "Không có dữ liệu")]
        [SwaggerResponse(500, "Lỗi hệ thống")]
        public async Task<IActionResult> GetClassesByUserId(int userId, [FromQuery] int? courseId)
        {
            var result = await _courseInstanceService.GetClassesByUserIdAsync(userId, courseId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPatch("{id}/toggle-status")]
        [SwaggerOperation(
        Summary = "Bật/Tắt trạng thái lớp học (Active/Deactive)",
        Description = "Chỉ cho phép thực hiện khi lớp chưa bắt đầu HOẶC đã kết thúc. Không được đổi khi đang diễn ra.")]
        public async Task<IActionResult> ToggleCourseStatus(int id)
        {
            var result = await _courseInstanceService.ToggleCourseStatusAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost("import-excel")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
    Summary = "Import danh sách lớp học từ Excel",
    Description = "Admin import CourseInstance từ file Excel. Validate toàn bộ dữ liệu trước khi import."
)]
        [SwaggerResponse(201, "Import thành công")]
        [SwaggerResponse(400, "Excel validation failed")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> ImportCourseInstancesFromExcel(
    [FromForm] ImportCourseInstanceExcelRequest request
)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new BaseResponse<object>(
                    "File is required",
                    StatusCodeEnum.BadRequest_400,
                    null
                ));
            }

            var result = await _courseInstanceService
                .ImportCourseInstancesFromExcelAsync(request.File);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => Created(string.Empty, result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.InternalServerError_500 => StatusCode(500, result),
                _ => StatusCode(500, result)
            };
        }


        // 🔹 Lấy Enroll Key của lớp học (Dành cho Instructor)
        [HttpGet("{courseInstanceId}/enroll-key")]
        [SwaggerOperation(
            Summary = "Lấy mã Enroll Key của lớp học",
            Description = "Cho phép giảng viên của lớp xem mã Enroll Key hiện tại. Cần truyền userId để kiểm tra quyền."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<string>))]
        [SwaggerResponse(403, "Không có quyền xem mã lớp")]
        [SwaggerResponse(404, "Không tìm thấy lớp học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetEnrollKey(int courseInstanceId, [FromQuery] int userId)
        {
            // Gọi hàm service đã viết
            var result = await _courseInstanceService.GetEnrollmentPasswordAsync(courseInstanceId, userId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.Forbidden_403 => StatusCode(403, result), // Hoặc dùng Forbid(result.Message)
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }


    }
}
