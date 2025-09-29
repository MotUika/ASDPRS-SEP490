using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Rubric;
using Service.RequestAndResponse.Response.Criteria;
using Service.RequestAndResponse.Response.Rubric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class RubricService : IRubricService
    {
        private readonly IRubricRepository _rubricRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public RubricService(IRubricRepository rubricRepository, ASDPRSContext context, IMapper mapper)
        {
            _rubricRepository = rubricRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<RubricResponse>> GetRubricByIdAsync(int id)
        {
            try
            {
                var rubric = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                    .FirstOrDefaultAsync(r => r.RubricId == id);

                if (rubric == null)
                {
                    return new BaseResponse<RubricResponse>("Rubric not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<RubricResponse>(rubric);
                response.CriteriaCount = rubric.Criteria?.Count ?? 0;
                response.TemplateTitle = rubric.Template?.Title;
                response.AssignmentTitle = rubric.Assignment?.Title;

                return new BaseResponse<RubricResponse>("Rubric retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricResponse>($"Error retrieving rubric: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<RubricResponse>>> GetAllRubricsAsync()
        {
            try
            {
                var rubrics = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                    .ToListAsync();

                var response = rubrics.Select(r =>
                {
                    var rubricResponse = _mapper.Map<RubricResponse>(r);
                    rubricResponse.CriteriaCount = r.Criteria?.Count ?? 0;
                    rubricResponse.TemplateTitle = r.Template?.Title;
                    rubricResponse.AssignmentTitle = r.Assignment?.Title;
                    return rubricResponse;
                });

                return new BaseResponse<IEnumerable<RubricResponse>>("Rubrics retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricResponse>>($"Error retrieving rubrics: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<RubricResponse>> CreateRubricAsync(CreateRubricRequest request)
        {
            try
            {
                var rubric = _mapper.Map<Rubric>(request);
                var createdRubric = await _rubricRepository.AddAsync(rubric);
                var response = _mapper.Map<RubricResponse>(createdRubric);

                return new BaseResponse<RubricResponse>("Rubric created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricResponse>($"Error creating rubric: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<RubricResponse>> UpdateRubricAsync(UpdateRubricRequest request)
        {
            try
            {
                var existingRubric = await _rubricRepository.GetByIdAsync(request.RubricId);
                if (existingRubric == null)
                {
                    return new BaseResponse<RubricResponse>("Rubric not found", StatusCodeEnum.NotFound_404, null);
                }

                _mapper.Map(request, existingRubric);
                var updatedRubric = await _rubricRepository.UpdateAsync(existingRubric);
                var response = _mapper.Map<RubricResponse>(updatedRubric);

                return new BaseResponse<RubricResponse>("Rubric updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricResponse>($"Error updating rubric: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteRubricAsync(int id)
        {
            try
            {
                var rubric = await _rubricRepository.GetByIdAsync(id);
                if (rubric == null)
                {
                    return new BaseResponse<bool>("Rubric not found", StatusCodeEnum.NotFound_404, false);
                }

                await _rubricRepository.DeleteAsync(rubric);
                return new BaseResponse<bool>("Rubric deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting rubric: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<RubricResponse>>> GetRubricsByTemplateIdAsync(int templateId)
        {
            try
            {
                var rubrics = await _rubricRepository.GetByTemplateIdAsync(templateId);
                var response = _mapper.Map<IEnumerable<RubricResponse>>(rubrics);

                return new BaseResponse<IEnumerable<RubricResponse>>("Rubrics retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricResponse>>($"Error retrieving rubrics: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        /*        public async Task<BaseResponse<IEnumerable<RubricResponse>>> GetRubricsByAssignmentIdAsync(int assignmentId)
                {
                    try
                    {
                        var rubrics = await _rubricRepository.GetByAssignmentIdAsync(assignmentId);
                        var response = _mapper.Map<IEnumerable<RubricResponse>>(rubrics);

                        return new BaseResponse<IEnumerable<RubricResponse>>("Rubrics retrieved successfully", StatusCodeEnum.OK_200, response);
                    }
                    catch (Exception ex)
                    {
                        return new BaseResponse<IEnumerable<RubricResponse>>($"Error retrieving rubrics: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
                    }
                }*/

        public async Task<BaseResponse<RubricResponse>> GetRubricWithCriteriaAsync(int rubricId)
        {
            try
            {
                var rubric = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                        .ThenInclude(c => c.CriteriaTemplate)
                    .FirstOrDefaultAsync(r => r.RubricId == rubricId);

                if (rubric == null)
                {
                    return new BaseResponse<RubricResponse>("Rubric not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<RubricResponse>(rubric);
                response.CriteriaCount = rubric.Criteria?.Count ?? 0;
                response.TemplateTitle = rubric.Template?.Title;
                response.AssignmentTitle = rubric.Assignment?.Title;

                // Map danh sách criteria
                if (rubric.Criteria != null)
                {
                    response.Criteria = _mapper.Map<List<CriteriaResponse>>(rubric.Criteria);
                }

                return new BaseResponse<RubricResponse>("Rubric with criteria retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricResponse>($"Error retrieving rubric with criteria: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}
