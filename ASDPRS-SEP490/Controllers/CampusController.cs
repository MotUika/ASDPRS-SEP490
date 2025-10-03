using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Campus;
using Service.RequestAndResponse.Response.Campus;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý campus: CRUD, thống kê số lượng người dùng, năm học, chương trình đào tạo")]
    public class CampusController : ControllerBase
    {
        private readonly ICampusService _campusService;

        public CampusController(ICampusService campusService)
        {
            _campusService = campusService;
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin campus theo ID",
            Description = "Trả về thông tin chi tiết của campus dựa trên ID được cung cấp, bao gồm số lượng người dùng, năm học, chương trình đào tạo"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CampusResponse>))]
        [SwaggerResponse(404, "Không tìm thấy campus")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCampusById(int id)
        {
            var result = await _campusService.GetCampusByIdAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả campus",
            Description = "Trả về danh sách toàn bộ campus trong hệ thống với đầy đủ thông tin thống kê"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CampusResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAllCampuses()
        {
            var result = await _campusService.GetAllCampusesAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo campus mới",
            Description = "Tạo một campus mới với tên và địa chỉ được cung cấp"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<CampusResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateCampus([FromBody] CreateCampusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _campusService.CreateCampusAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetCampusById), new { id = result.Data?.CampusId }, result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin campus",
            Description = "Cập nhật thông tin của campus đã tồn tại trong hệ thống"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<CampusResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy campus")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateCampus([FromBody] UpdateCampusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _campusService.UpdateCampusAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa campus",
            Description = "Xóa campus khỏi hệ thống dựa trên ID. Lưu ý: Chỉ có thể xóa campus chưa có dữ liệu liên quan (người dùng, năm học, chương trình đào tạo)"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy campus")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteCampus(int id)
        {
            var result = await _campusService.DeleteCampusAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}