using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Enums;
using System.Threading.Tasks;
using System.IO;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseStudentController : ControllerBase
    {
        private readonly ICourseStudentService _courseStudentService;

        public CourseStudentController(ICourseStudentService courseStudentService)
        {
            _courseStudentService = courseStudentService;
        }

        // Lấy chi tiết 1 CourseStudent
        [HttpGet("{id}")]
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
