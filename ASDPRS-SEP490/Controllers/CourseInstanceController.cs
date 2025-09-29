using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseInstance;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseInstanceController : ControllerBase
    {
        private readonly ICourseInstanceService _courseInstanceService;

        public CourseInstanceController(ICourseInstanceService courseInstanceService)
        {
            _courseInstanceService = courseInstanceService;
        }

        // Lấy chi tiết 1 lớp học
        [HttpGet("{id}")]
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

        // Lấy tất cả lớp học
        [HttpGet]
        public async Task<IActionResult> GetAllCourseInstances()
        {
            var result = await _courseInstanceService.GetAllCourseInstancesAsync();
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy lớp theo CourseId
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseInstancesByCourseId(int courseId)
        {
            var result = await _courseInstanceService.GetCourseInstancesByCourseIdAsync(courseId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy lớp theo SemesterId
        [HttpGet("semester/{semesterId}")]
        public async Task<IActionResult> GetCourseInstancesBySemesterId(int semesterId)
        {
            var result = await _courseInstanceService.GetCourseInstancesBySemesterIdAsync(semesterId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy lớp theo CampusId
        [HttpGet("campus/{campusId}")]
        public async Task<IActionResult> GetCourseInstancesByCampusId(int campusId)
        {
            var result = await _courseInstanceService.GetCourseInstancesByCampusIdAsync(campusId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Tạo lớp học mới
        [HttpPost]
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

        // Cập nhật lớp học
        [HttpPut]
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

        // Xóa lớp học
        [HttpDelete("{id}")]
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
    }
}
