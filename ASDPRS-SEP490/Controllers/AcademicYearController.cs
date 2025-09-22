using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.AcademicYear;
using Service.RequestAndResponse.Enums;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AcademicYearController : ControllerBase
    {
        private readonly IAcademicYearService _academicYearService;

        public AcademicYearController(IAcademicYearService academicYearService)
        {
            _academicYearService = academicYearService;
        }

        [HttpGet("{id}")]
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
        public async Task<IActionResult> CreateAcademicYear([FromBody] CreateAcademicYearRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _academicYearService.CreateAcademicYearAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetAcademicYearById), new { id = result.Data?.AcademicYearId }, result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAcademicYear([FromBody] UpdateAcademicYearRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _academicYearService.UpdateAcademicYearAsync(request);

            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpDelete("{id}")]
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