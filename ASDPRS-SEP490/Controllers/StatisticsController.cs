using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Response.Statistic;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Thống kê assignment và so sánh lớp học")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        // Thống kê assignment trong 1 lớp
        [HttpGet("assignments/class")]
        [SwaggerOperation(
            Summary = "Thống kê assignment trong lớp",
            Description = "Lấy các thống kê: tổng submission, đã chấm, điểm TB, min/max, pass/fail, phân phối điểm"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<AssignmentStatisticResponse>>))]
        public async Task<IActionResult> GetAssignmentStatistics([FromQuery] int userId, [FromQuery] int courseInstanceId)
        {
            var result = await _statisticsService.GetAssignmentStatisticsByClassAsync(userId, courseInstanceId);
            return StatusCode((int)result.StatusCode, result);
        }

        // So sánh các lớp trong 1 môn
        [HttpGet("classes/course")]
        [SwaggerOperation(
            Summary = "So sánh thống kê giữa các lớp trong cùng môn",
            Description = "Lấy thống kê từng lớp: điểm TB, pass rate, tổng submissions, phân phối điểm"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<ClassStatisticResponse>>))]
        public async Task<IActionResult> GetClassStatistics([FromQuery] int userId, [FromQuery] int courseId)
        {
            var result = await _statisticsService.GetClassStatisticsByCourseAsync(userId, courseId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
