using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Response.Search;
using Swashbuckle.AspNetCore.Annotations;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/instructor/[controller]")]
    [Authorize(Roles = "Instructor, Student")]
    public class SearchController : ControllerBase
    {
        private readonly IKeywordSearchService _searchService;

        public SearchController(IKeywordSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Tìm kiếm keyword trong assignments, feedback, summaries",
            Description = "Sinh viên tìm kiếm terms trong assignments, peer/instructor feedback, LLM summaries của mình"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SearchResultEFResponse>))]
        [SwaggerResponse(400, "Yêu cầu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new BaseResponse<SearchResultEFResponse>("Query required", StatusCodeEnum.BadRequest_400, null));
            }

            var studentId = GetCurrentStudentId();
            var result = await _searchService.SearchAsync(query, studentId, "Student");
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Tìm kiếm keyword trong assignments, feedback, summaries (for instructor)",
            Description = "Instructor tìm kiếm terms trong toàn bộ hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SearchResultEFResponse>))]
        [SwaggerResponse(400, "Yêu cầu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> SearchInstructor([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new BaseResponse<SearchResultEFResponse>("Query required", StatusCodeEnum.BadRequest_400, null));
            }

            var instructorId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var result = await _searchService.SearchAsync(query, instructorId, "Instructor");
            return StatusCode((int)result.StatusCode, result);
        }

        private int GetCurrentStudentId()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int studentId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }

            return studentId;
        }
    }
}