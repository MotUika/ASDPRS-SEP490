using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CriteriaTemplate;
using Service.RequestAndResponse.Response.CriteriaTemplate;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý tiêu chí mẫu (Criteria Template): CRUD, lấy theo TemplateId")]
    public class CriteriaTemplateController : ControllerBase
    {
        private readonly ICriteriaTemplateService _criteriaTemplateService;

        public CriteriaTemplateController(ICriteriaTemplateService criteriaTemplateService)
        {
            _criteriaTemplateService = criteriaTemplateService;
        }

        // 🧩 Lấy Criteria Template theo ID
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin Criteria Template theo ID",
            Description = "Trả về thông tin chi tiết của Criteria Template dựa trên ID được cung cấp"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CriteriaTemplateResponse>))]
        [SwaggerResponse(404, "Không tìm thấy Criteria Template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCriteriaTemplateById(int id)
        {
            var result = await _criteriaTemplateService.GetCriteriaTemplateByIdAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // 📋 Lấy danh sách tất cả Criteria Template
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả Criteria Template",
            Description = "Trả về danh sách toàn bộ Criteria Template trong hệ thống"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CriteriaTemplateResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAllCriteriaTemplates()
        {
            var result = await _criteriaTemplateService.GetAllCriteriaTemplatesAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // 📂 Lấy danh sách Criteria Template theo TemplateId
        [HttpGet("template/{templateId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách Criteria Template theo TemplateId",
            Description = "Trả về danh sách tiêu chí mẫu thuộc Template cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CriteriaTemplateResponse>>))]
        [SwaggerResponse(404, "Không tìm thấy Template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCriteriaTemplatesByTemplateId(int templateId)
        {
            var result = await _criteriaTemplateService.GetCriteriaTemplatesByTemplateIdAsync(templateId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // ✏️ Tạo Criteria Template mới
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo Criteria Template mới",
            Description = "Tạo mới một tiêu chí mẫu thuộc một Template có sẵn"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<CriteriaTemplateResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateCriteriaTemplate([FromBody] CreateCriteriaTemplateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _criteriaTemplateService.CreateCriteriaTemplateAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetCriteriaTemplateById), new { id = result.Data?.CriteriaTemplateId }, result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }

        // 🛠️ Cập nhật Criteria Template
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật Criteria Template",
            Description = "Cập nhật thông tin của Criteria Template đã tồn tại"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<CriteriaTemplateResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy Criteria Template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateCriteriaTemplate([FromBody] UpdateCriteriaTemplateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _criteriaTemplateService.UpdateCriteriaTemplateAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }

        // ❌ Xóa Criteria Template theo ID
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa Criteria Template",
            Description = "Xóa một Criteria Template dựa trên ID được cung cấp"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy Criteria Template")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteCriteriaTemplate(int id)
        {
            var result = await _criteriaTemplateService.DeleteCriteriaTemplateAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
