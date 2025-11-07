using BussinessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.RegradeRequest;
using Service.RequestAndResponse.Response.RegradeRequest;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý yêu cầu chấm lại bài tập")]
    public class RegradeRequestsController : ControllerBase
    {
        private readonly IRegradeRequestService _regradeRequestService;

        public RegradeRequestsController(IRegradeRequestService regradeRequestService)
        {
            _regradeRequestService = regradeRequestService;
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(
            Summary = "Tạo yêu cầu chấm lại",
            Description = "Học sinh tạo yêu cầu chấm lại bài tập đã nộp"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<RegradeRequestResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(403, "Không có quyền truy cập")]
        [SwaggerResponse(404, "Không tìm thấy bài nộp")]
        [SwaggerResponse(409, "Đã tồn tại yêu cầu chấm lại đang chờ xử lý")]
        public async Task<IActionResult> CreateRegradeRequest([FromBody] CreateRegradeRequestRequest request)
        {
            var result = await _regradeRequestService.CreateRegradeRequestAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{requestId}")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Lấy thông tin yêu cầu chấm lại theo ID",
            Description = "Lấy chi tiết yêu cầu chấm lại dựa trên ID"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestResponse>))]
        [SwaggerResponse(404, "Không tìm thấy yêu cầu chấm lại")]
        public async Task<IActionResult> GetRegradeRequestById(int requestId)
        {
            var request = new GetRegradeRequestByIdRequest { RequestId = requestId };
            var result = await _regradeRequestService.GetRegradeRequestByIdAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Instructor")]
        [SwaggerOperation(
            Summary = "Lấy danh sách yêu cầu chấm lại với bộ lọc",
            Description = "Lấy danh sách yêu cầu chấm lại với các bộ lọc tùy chọn (dành cho Admin và Instructor)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestListResponse>))]
        public async Task<IActionResult> GetRegradeRequestsByFilter(
            [FromQuery] int? submissionId = null,
            [FromQuery] int? studentId = null,
            [FromQuery] int? instructorId = null,
            [FromQuery] int? assignmentId = null,
            [FromQuery] string status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var request = new GetRegradeRequestsByFilterRequest
            {
                SubmissionId = submissionId,
                StudentId = studentId,
                InstructorId = instructorId,
                AssignmentId = assignmentId,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _regradeRequestService.GetRegradeRequestsByFilterAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{requestId}")]
        [Authorize(Roles = "Admin,Instructor")]
        [SwaggerOperation(
            Summary = "Cập nhật yêu cầu chấm lại",
            Description = "Cập nhật thông tin yêu cầu chấm lại (dành cho Admin và Instructor)"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<RegradeRequestResponse>))]
        [SwaggerResponse(404, "Không tìm thấy yêu cầu chấm lại")]
        public async Task<IActionResult> UpdateRegradeRequest(int requestId, [FromBody] UpdateRegradeRequestRequest request)
        {
            request.RequestId = requestId;
            var result = await _regradeRequestService.UpdateRegradeRequestAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{requestId}/status")]
        [Authorize(Roles = "Admin,Instructor")]
        [SwaggerOperation(
            Summary = "Cập nhật trạng thái yêu cầu chấm lại",
            Description = "Cập nhật trạng thái và ghi chú xử lý yêu cầu chấm lại (dành cho Admin và Instructor)"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<RegradeRequestResponse>))]
        [SwaggerResponse(404, "Không tìm thấy yêu cầu chấm lại")]
        public async Task<IActionResult> UpdateRegradeRequestStatus(int requestId, [FromBody] UpdateRegradeRequestStatusRequest request)
        {
            request.RequestId = requestId;
            var result = await _regradeRequestService.UpdateRegradeRequestStatusAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("submission/{submissionId}/pending")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Kiểm tra yêu cầu chấm lại đang chờ xử lý",
            Description = "Kiểm tra xem có yêu cầu chấm lại đang chờ xử lý cho bài nộp cụ thể không"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<bool>))]
        public async Task<IActionResult> CheckPendingRequestExists(int submissionId)
        {
            var result = await _regradeRequestService.CheckPendingRequestExistsAsync(submissionId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin,Instructor")]
        [SwaggerOperation(
            Summary = "Lấy danh sách yêu cầu chấm lại đang chờ xử lý",
            Description = "Lấy danh sách tất cả yêu cầu chấm lại có trạng thái 'Pending'"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestListResponse>))]
        public async Task<IActionResult> GetPendingRegradeRequests(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _regradeRequestService.GetPendingRegradeRequestsAsync(pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("student/{studentId}")]
        [Authorize(Roles = "Student,Admin,Instructor")]
        [SwaggerOperation(
            Summary = "Lấy yêu cầu chấm lại theo học sinh",
            Description = "Lấy danh sách yêu cầu chấm lại của một học sinh cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestListResponse>))]
        public async Task<IActionResult> GetRegradeRequestsByStudentId(
            int studentId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _regradeRequestService.GetRegradeRequestsByStudentIdAsync(studentId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("instructor/{instructorId}")]
        [Authorize(Roles = "Instructor,Admin")]
        [SwaggerOperation(
            Summary = "Lấy yêu cầu chấm lại theo giảng viên",
            Description = "Lấy danh sách yêu cầu chấm lại được xử lý bởi một giảng viên cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestListResponse>))]
        public async Task<IActionResult> GetRegradeRequestsByInstructorId(
            int instructorId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _regradeRequestService.GetRegradeRequestsByInstructorIdAsync(instructorId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}