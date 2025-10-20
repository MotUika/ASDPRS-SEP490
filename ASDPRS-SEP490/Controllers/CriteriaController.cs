using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Criteria;
using Service.RequestAndResponse.Response.Criteria;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý Tiêu chí (Criteria)")]
    public class CriteriaController : ControllerBase
    {
        private readonly ICriteriaService _criteriaService;

        public CriteriaController(ICriteriaService criteriaService)
        {
            _criteriaService = criteriaService;
        }

        // Lấy chi tiết Criteria theo Id
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết tiêu chí theo ID",
            Description = "Trả về thông tin chi tiết của một tiêu chí dựa trên ID được cung cấp."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CriteriaResponse>))]
        [SwaggerResponse(404, "Không tìm thấy tiêu chí")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetCriteriaById(int id)
        {
            var result = await _criteriaService.GetCriteriaByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy tất cả Criteria
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả tiêu chí",
            Description = "Trả về một danh sách chứa tất cả các tiêu chí trong hệ thống."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CriteriaResponse>>))]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetAllCriteria()
        {
            var result = await _criteriaService.GetAllCriteriaAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        // Tạo mới Criteria
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo một tiêu chí mới",
            Description = "Tạo một tiêu chí mới trong hệ thống dựa trên thông tin được cung cấp."
        )]
        [SwaggerResponse(201, "Tạo mới thành công", typeof(BaseResponse<CriteriaResponse>))]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> CreateCriteria([FromBody] CreateCriteriaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _criteriaService.CreateCriteriaAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Cập nhật Criteria
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin tiêu chí",
            Description = "Cập nhật thông tin của một tiêu chí đã tồn tại."
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<CriteriaResponse>))]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy tiêu chí để cập nhật")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> UpdateCriteria([FromBody] UpdateCriteriaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _criteriaService.UpdateCriteriaAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Xóa Criteria theo Id
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa một tiêu chí",
            Description = "Xóa một tiêu chí khỏi hệ thống dựa trên ID."
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy tiêu chí để xóa")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> DeleteCriteria(int id)
        {
            var result = await _criteriaService.DeleteCriteriaAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy danh sách Criteria theo RubricId
        [HttpGet("rubric/{rubricId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách tiêu chí theo Rubric",
            Description = "Trả về danh sách các tiêu chí thuộc về một rubric cụ thể."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CriteriaResponse>>))]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetCriteriaByRubricId(int rubricId)
        {
            var result = await _criteriaService.GetCriteriaByRubricIdAsync(rubricId);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy danh sách Criteria theo TemplateId
        [HttpGet("template/{templateId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách tiêu chí theo mẫu (Template)",
            Description = "Trả về danh sách các tiêu chí thuộc về một mẫu tiêu chí cụ thể."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<IEnumerable<CriteriaResponse>>))]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetCriteriaByTemplateId(int templateId)
        {
            var result = await _criteriaService.GetCriteriaByTemplateIdAsync(templateId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("rubric/{rubricId}/validate-weight")]
        [SwaggerOperation(
            Summary = "Kiểm tra tổng trọng số criteria trong rubric",
            Description = "Trả về tổng phần trăm trọng số của tất cả các criteria trong rubric được chỉ định."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<decimal>))]
        public async Task<IActionResult> ValidateCriteriaWeights(int rubricId)
        {
            var result = await _criteriaService.ValidateTotalWeightAsync(rubricId);
            return Ok(result);
        }
    }
}
