using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/cross-class")]
    [Produces("application/json")]
    [Authorize(Roles = "Instructor")]
    [SwaggerTag("Quản lý và gợi ý Cross-Class Peer Review")]
    public class CrossClassController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly ASDPRSContext _context;

        public CrossClassController(IAssignmentService assignmentService, ASDPRSContext context)
        {
            _assignmentService = assignmentService;
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirst("userId")!.Value);

        /// <summary>
        /// Lấy danh sách Cross-Class Tag mà giảng viên này đã từng sử dụng
        /// </summary>
        /// <remarks>
        /// Dùng để hiển thị autocomplete/gợi ý khi tạo assignment mới có bật Cross-Class
        /// </remarks>
        [HttpGet("my-tags")]
        [SwaggerOperation(
            Summary = "Lấy danh sách tag cross-class đã dùng",
            Description = "Trả về tối đa 20 tag phổ biến nhất mà giảng viên đang đăng nhập đã dùng cho các assignment có bật AllowCrossClass"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<string>>))]
        public async Task<IActionResult> GetMyCrossClassTags()
        {
            var userId = CurrentUserId;

            var tags = await _context.Assignments
                .Where(a => a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId)
                            && a.AllowCrossClass == true
                            && !string.IsNullOrEmpty(a.CrossClassTag))
                .Select(a => a.CrossClassTag!)
                .Distinct()
                .OrderBy(t => t)
                .Take(20)
                .ToListAsync();

            return Ok(new BaseResponse<List<string>>(
                message: "Lấy danh sách tag thành công",
                statusCode: StatusCodeEnum.OK_200,
                data: tags
            ));
        }

        /// <summary>
        /// Kiểm tra tag cross-class đã được dùng chưa và ở những lớp nào
        /// </summary>
        [HttpGet("tag-exists")]
        [SwaggerOperation(
            Summary = "Kiểm tra tag cross-class đã tồn tại chưa",
            Description = "Trả về thông tin tag đã được chuẩn hóa, có tồn tại không, và danh sách assignment đang dùng tag đó (nếu có)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<object>))]
        public async Task<IActionResult> CheckTagExists([FromQuery] string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return BadRequest(new BaseResponse<object>(
                    message: "Tag không được để trống",
                    statusCode: StatusCodeEnum.BadRequest_400,
                    data: null
                ));
            }

            var normalized = tag.Trim();
            if (!normalized.StartsWith("#"))
                normalized = "#" + normalized;

            var userId = CurrentUserId;

            // THÊM Include ĐỂ LOAD DỮ LIỆU LIÊN QUAN
            var baseQuery = _context.Assignments
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.CourseInstructors)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                    .ThenInclude(ci => ci.CourseStudents)
                .Where(a => a.AllowCrossClass == true
                            && a.CrossClassTag == normalized
                            && a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId));

            var exists = await baseQuery.AnyAsync();

            var usedInAssignments = new List<object>();

            if (exists)
            {
                usedInAssignments = await baseQuery
                    .Select(a => new
                    {
                        AssignmentId = a.AssignmentId,
                        Title = a.Title,
                        Course = a.CourseInstance.Course.CourseName + " - " + a.CourseInstance.SectionCode,
                        Deadline = a.Deadline.ToString("dd/MM/yyyy"),
                        StudentCount = a.CourseInstance.CourseStudents.Count
                    })
                    .Take(10)
                    .ToListAsync<object>(); // ép kiểu sang object
            }

            var result = new
            {
                NormalizedTag = normalized,
                Exists = exists,
                Message = exists
                    ? $"Tag này đã được dùng ở {usedInAssignments.Count} assignment"
                    : "Đây là tag mới – sẽ được tạo khi lưu assignment đầu tiên",
                UsedInAssignments = usedInAssignments
            };

            return Ok(new BaseResponse<object>(
                message: "Kiểm tra tag thành công",
                statusCode: StatusCodeEnum.OK_200,
                data: result
            ));
        }
    }
}