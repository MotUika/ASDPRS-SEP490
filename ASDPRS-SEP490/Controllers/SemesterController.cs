using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Semester;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemesterController : ControllerBase
    {
        private readonly ISemesterService _semesterService;

        public SemesterController(ISemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        // Lấy chi tiết 1 semester
        [HttpGet("{id}")]
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

        // Xóa semester
        [HttpDelete("{id}")]
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
