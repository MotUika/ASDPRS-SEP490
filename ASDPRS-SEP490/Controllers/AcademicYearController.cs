using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.AcademicYear;
using Service.RequestAndResponse.Response.AcademicYear;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý năm học: CRUD, tìm kiếm theo campus")]
    public class AcademicYearController : ControllerBase
    {
        private readonly IAcademicYearService _academicYearService;

        public AcademicYearController(IAcademicYearService academicYearService)
        {
            _academicYearService = academicYearService;
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin năm học theo ID",
            Description = "Trả về thông tin chi tiết của năm học dựa trên ID được cung cấp"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<AcademicYearResponse>))]
        [SwaggerResponse(404, "Không tìm thấy năm học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAcademicYearById(int id)
        {
            var result = await _academicYearService.GetAcademicYearByIdAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả năm học",
            Description = "Trả về danh sách toàn bộ năm học trong hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<AcademicYearResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAllAcademicYears()
        {
            var result = await _academicYearService.GetAllAcademicYearsAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo năm học mới",
            Description = "Tạo một năm học mới với thông tin được cung cấp. Cần có campusId, tên, ngày bắt đầu và kết thúc"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<AcademicYearResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(409, "Tên năm học đã tồn tại")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateAcademicYear([FromBody] CreateAcademicYearRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _academicYearService.CreateAcademicYearAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetAcademicYearById), new { id = result.Data?.AcademicYearId }, result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.Conflict_409 => Conflict(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin năm học",
            Description = "Cập nhật thông tin của năm học đã tồn tại trong hệ thống"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<AcademicYearResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy năm học")]
        [SwaggerResponse(409, "Tên năm học đã tồn tại")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateAcademicYear([FromBody] UpdateAcademicYearRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _academicYearService.UpdateAcademicYearAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.Conflict_409 => Conflict(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa năm học",
            Description = "Xóa năm học khỏi hệ thống dựa trên ID. Lưu ý: Chỉ có thể xóa năm học chưa có dữ liệu liên quan"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy năm học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteAcademicYear(int id)
        {
            var result = await _academicYearService.DeleteAcademicYearAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("campus/{campusId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách năm học theo campus",
            Description = "Trả về danh sách các năm học thuộc campus được chỉ định"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<AcademicYearResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAcademicYearsByCampus(int campusId)
        {
            var result = await _academicYearService.GetAcademicYearsByCampusAsync(campusId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }
    }
}