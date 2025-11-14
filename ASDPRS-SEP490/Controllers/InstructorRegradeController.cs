using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.RegradeRequest;
using Service.RequestAndResponse.Response.RegradeRequest;
using Swashbuckle.AspNetCore.Annotations;

namespace ASDPRS_SEP490.Controllers
{
    [Route("api/instructor/regrade-requests")]
    [ApiController]
    public class InstructorRegradeController : ControllerBase
    {
        private readonly IRegradeRequestService _regradeRequestService;

        public InstructorRegradeController(IRegradeRequestService regradeRequestService)
        {
            _regradeRequestService = regradeRequestService;
        }

        // 1️⃣ Lấy danh sách yêu cầu của GV
        [HttpGet("{userId}")]
        [SwaggerOperation(Summary = "Lấy yêu cầu chấm lại của giảng viên", Description = "Danh sách yêu cầu chấm lại GV phải xử lý")]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestListResponse>))]
        public async Task<IActionResult> GetRegradeRequestsByInstructor(int userId)
        {
            var result = await _regradeRequestService.GetRegradeRequestsByInstructorIdAsync(userId);
            return StatusCode((int)result.StatusCode, result);
        }

        // 2️⃣ Xem chi tiết yêu cầu chấm lại
        [HttpGet("detail/{requestId}")]
        [SwaggerOperation(Summary = "Xem chi tiết yêu cầu chấm lại")]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestResponse>))]
        public async Task<IActionResult> GetRegradeRequestDetail(int requestId)
        {
            var request = new GetRegradeRequestByIdRequest { RequestId = requestId };
            var result = await _regradeRequestService.GetRegradeRequestByIdAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // 3️⃣ GV chấp nhận hoặc từ chối yêu cầu
        [HttpPut("review")]
        [SwaggerOperation(Summary = "GV review yêu cầu: chấp nhận hoặc từ chối")]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestResponse>))]
        public async Task<IActionResult> ReviewRegradeRequest([FromBody] UpdateRegradeRequestStatusByUserRequest request)
        {
            var result = await _regradeRequestService.ReviewRegradeRequestAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // 4️⃣ GV xác nhận đã chấm xong (Complete)
        [HttpPut("complete")]
        [SwaggerOperation(Summary = "GV xác nhận đã chấm xong")]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestResponse>))]
        public async Task<IActionResult> CompleteRegradeRequest([FromBody] UpdateRegradeRequestStatusByUserRequest request)
        {
            // Ép status thành "Completed"
            request.Status = "Completed";
            var result = await _regradeRequestService.CompleteRegradeRequestAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }


    }
}
