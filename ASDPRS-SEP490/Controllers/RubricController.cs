using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.Rubric;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RubricController : ControllerBase
    {
        private readonly IRubricService _rubricService;

        public RubricController(IRubricService rubricService)
        {
            _rubricService = rubricService;
        }

        // Lấy rubric theo Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRubricById(int id)
        {
            var result = await _rubricService.GetRubricByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy tất cả rubric
        [HttpGet]
        public async Task<IActionResult> GetAllRubrics()
        {
            var result = await _rubricService.GetAllRubricsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        // Tạo rubric mới
        [HttpPost]
        public async Task<IActionResult> CreateRubric([FromBody] CreateRubricRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rubricService.CreateRubricAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Cập nhật rubric
        [HttpPut]
        public async Task<IActionResult> UpdateRubric([FromBody] UpdateRubricRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _rubricService.UpdateRubricAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Xóa rubric theo Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRubric(int id)
        {
            var result = await _rubricService.DeleteRubricAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy rubric theo TemplateId
        [HttpGet("template/{templateId}")]
        public async Task<IActionResult> GetRubricsByTemplateId(int templateId)
        {
            var result = await _rubricService.GetRubricsByTemplateIdAsync(templateId);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy rubric kèm criteria
        [HttpGet("{rubricId}/criteria")]
        public async Task<IActionResult> GetRubricWithCriteria(int rubricId)
        {
            var result = await _rubricService.GetRubricWithCriteriaAsync(rubricId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
