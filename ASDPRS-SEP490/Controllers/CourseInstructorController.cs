using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.CourseInstructor;
using Service.RequestAndResponse.Enums;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseInstructorController : ControllerBase
    {
        private readonly ICourseInstructorService _courseInstructorService;

        public CourseInstructorController(ICourseInstructorService courseInstructorService)
        {
            _courseInstructorService = courseInstructorService;
        }

        // Lấy chi tiết 1 CourseInstructor
        [HttpGet("{id}")]
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

        // Lấy instructors trong 1 lớp
        [HttpGet("course-instance/{courseInstanceId}")]
        public async Task<IActionResult> GetCourseInstructorsByCourseInstance(int courseInstanceId)
        {
            var result = await _courseInstructorService.GetCourseInstructorsByCourseInstanceAsync(courseInstanceId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy lớp mà 1 instructor dạy
        [HttpGet("instructor/{instructorId}")]
        public async Task<IActionResult> GetCourseInstructorsByInstructor(int instructorId)
        {
            var result = await _courseInstructorService.GetCourseInstructorsByInstructorAsync(instructorId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Thêm instructor vào lớp
        [HttpPost]
        public async Task<IActionResult> CreateCourseInstructor([FromBody] CreateCourseInstructorRequest request)
        {
            var result = await _courseInstructorService.CreateCourseInstructorAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201
                    => CreatedAtAction(nameof(GetCourseInstructorById), new { id = result.Data?.Id }, result),
                StatusCodeEnum.Conflict_409 => Conflict(result),
                _ => StatusCode(500, result)
            };
        }


        // Bulk gán nhiều instructors vào lớp
        [HttpPost("bulk-assign")]
        public async Task<IActionResult> BulkAssignInstructors([FromBody] BulkAssignInstructorsRequest request)
        {
            var result = await _courseInstructorService.BulkAssignInstructorsAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => Created("", result),
                _ => StatusCode(500, result)
            };
        }

        // Đổi giảng viên chính trong lớp
        [HttpPut("{courseInstanceId}/main-instructor/{mainInstructorId}")]
        public async Task<IActionResult> UpdateMainInstructor(int courseInstanceId, int mainInstructorId)
        {
            var result = await _courseInstructorService.UpdateMainInstructorAsync(courseInstanceId, mainInstructorId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Xóa instructor khỏi lớp
        [HttpDelete("{id}")]
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
