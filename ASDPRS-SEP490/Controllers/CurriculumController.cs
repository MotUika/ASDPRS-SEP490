using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Curriculum;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurriculumController : ControllerBase
    {
        private readonly ICurriculumService _curriculumService;

        public CurriculumController(ICurriculumService curriculumService)
        {
            _curriculumService = curriculumService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCurriculumById(int id)
        {
            var result = await _curriculumService.GetCurriculumByIdAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCurriculums()
        {
            var result = await _curriculumService.GetAllCurriculumsAsync();

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost]
        public async Task<IActionResult> CreateCurriculum([FromBody] CreateCurriculumRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _curriculumService.CreateCurriculumAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetCurriculumById), new { id = result.Data?.CurriculumId }, result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCurriculum([FromBody] UpdateCurriculumRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _curriculumService.UpdateCurriculumAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCurriculum(int id)
        {
            var result = await _curriculumService.DeleteCurriculumAsync(id);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("campus/{campusId}")]
        public async Task<IActionResult> GetCurriculumsByCampus(int campusId)
        {
            var result = await _curriculumService.GetCurriculumsByCampusAsync(campusId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("major/{majorId}")]
        public async Task<IActionResult> GetCurriculumsByMajor(int majorId)
        {
            var result = await _curriculumService.GetCurriculumsByMajorAsync(majorId);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
