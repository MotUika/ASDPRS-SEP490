using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Major;
using Service.RequestAndResponse.Response.Major;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý ngành học: CRUD")]
    public class MajorController : ControllerBase
    {
        private readonly IMajorService _majorService;

        public MajorController(IMajorService majorService)
        {
            _majorService = majorService;
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin ngành học theo ID",
            Description = "Trả về thông tin chi tiết của ngành học dựa trên ID được cung cấp"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<MajorResponse>))]
        [SwaggerResponse(404, "Không tìm thấy ngành học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetMajorById(int id)
        {
            var result = await _majorService.GetMajorByIdAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả ngành học",
            Description = "Trả về danh sách toàn bộ ngành học trong hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<MajorResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAllMajors()
        {
            var result = await _majorService.GetAllMajorsAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo ngành học mới",
            Description = "Tạo một ngành học mới với thông tin được cung cấp. Cần có tên ngành và mã ngành duy nhất"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<MajorResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateMajor([FromBody] CreateMajorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _majorService.CreateMajorAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetMajorById), new { id = result.Data?.MajorId }, result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin ngành học",
            Description = "Cập nhật thông tin của ngành học đã tồn tại trong hệ thống"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<MajorResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy ngành học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateMajor([FromBody] UpdateMajorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _majorService.UpdateMajorAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa ngành học",
            Description = "Xóa ngành học khỏi hệ thống dựa trên ID. Lưu ý: Chỉ có thể xóa ngành học chưa có curriculum liên quan"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy ngành học")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteMajor(int id)
        {
            var result = await _majorService.DeleteMajorAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
