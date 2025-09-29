using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.Criteria;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CriteriaController : ControllerBase
    {
        private readonly ICriteriaService _criteriaService;

        public CriteriaController(ICriteriaService criteriaService)
        {
            _criteriaService = criteriaService;
        }

        // Lấy chi tiết Criteria theo Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCriteriaById(int id)
        {
            var result = await _criteriaService.GetCriteriaByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy tất cả Criteria
        [HttpGet]
        public async Task<IActionResult> GetAllCriteria()
        {
            var result = await _criteriaService.GetAllCriteriaAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        // Tạo mới Criteria
        [HttpPost]
        public async Task<IActionResult> CreateCriteria([FromBody] CreateCriteriaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _criteriaService.CreateCriteriaAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Cập nhật Criteria
        [HttpPut]
        public async Task<IActionResult> UpdateCriteria([FromBody] UpdateCriteriaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _criteriaService.UpdateCriteriaAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Xóa Criteria theo Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCriteria(int id)
        {
            var result = await _criteriaService.DeleteCriteriaAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy danh sách Criteria theo RubricId
        [HttpGet("rubric/{rubricId}")]
        public async Task<IActionResult> GetCriteriaByRubricId(int rubricId)
        {
            var result = await _criteriaService.GetCriteriaByRubricIdAsync(rubricId);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy danh sách Criteria theo TemplateId
        [HttpGet("template/{templateId}")]
        public async Task<IActionResult> GetCriteriaByTemplateId(int templateId)
        {
            var result = await _criteriaService.GetCriteriaByTemplateIdAsync(templateId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
