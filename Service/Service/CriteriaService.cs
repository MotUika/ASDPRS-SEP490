using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Criteria;
using Service.RequestAndResponse.Response.Criteria;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CriteriaService : ICriteriaService
    {
        private readonly ICriteriaRepository _criteriaRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public CriteriaService(ICriteriaRepository criteriaRepository, ASDPRSContext context, IMapper mapper)
        {
            _criteriaRepository = criteriaRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<CriteriaResponse>> GetCriteriaByIdAsync(int id)
        {
            try
            {
                var criteria = await _context.Criteria
                    .Include(c => c.Rubric)
                    .Include(c => c.CriteriaTemplate)
                    .Include(c => c.CriteriaFeedbacks)
                    .FirstOrDefaultAsync(c => c.CriteriaId == id);

                if (criteria == null)
                {
                    return new BaseResponse<CriteriaResponse>("Criteria not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<CriteriaResponse>(criteria);
                response.CriteriaFeedbackCount = criteria.CriteriaFeedbacks?.Count ?? 0;
                response.RubricTitle = criteria.Rubric?.Title;
                response.CriteriaTemplateTitle = criteria.CriteriaTemplate?.Title;

                return new BaseResponse<CriteriaResponse>("Criteria retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaResponse>($"Error retrieving criteria: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CriteriaResponse>>> GetAllCriteriaAsync()
        {
            try
            {
                var criteria = await _context.Criteria
                    .Include(c => c.Rubric)
                    .Include(c => c.CriteriaTemplate)
                    .Include(c => c.CriteriaFeedbacks)
                    .ToListAsync();

                var response = criteria.Select(c =>
                {
                    var criteriaResponse = _mapper.Map<CriteriaResponse>(c);
                    criteriaResponse.CriteriaFeedbackCount = c.CriteriaFeedbacks?.Count ?? 0;
                    criteriaResponse.RubricTitle = c.Rubric?.Title;
                    criteriaResponse.CriteriaTemplateTitle = c.CriteriaTemplate?.Title;
                    return criteriaResponse;
                });

                return new BaseResponse<IEnumerable<CriteriaResponse>>("Criteria retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaResponse>>($"Error retrieving criteria: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CriteriaResponse>> CreateCriteriaAsync(CreateCriteriaRequest request)
        {
            try
            {
                var criteria = _mapper.Map<Criteria>(request);
                var createdCriteria = await _criteriaRepository.AddAsync(criteria);
                var response = _mapper.Map<CriteriaResponse>(createdCriteria);

                return new BaseResponse<CriteriaResponse>("Criteria created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaResponse>($"Error creating criteria: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CriteriaResponse>> UpdateCriteriaAsync(UpdateCriteriaRequest request)
        {
            try
            {
                var existingCriteria = await _criteriaRepository.GetByIdAsync(request.CriteriaId);
                if (existingCriteria == null)
                {
                    return new BaseResponse<CriteriaResponse>("Criteria not found", StatusCodeEnum.NotFound_404, null);
                }

                _mapper.Map(request, existingCriteria);
                var updatedCriteria = await _criteriaRepository.UpdateAsync(existingCriteria);
                var response = _mapper.Map<CriteriaResponse>(updatedCriteria);

                return new BaseResponse<CriteriaResponse>("Criteria updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaResponse>($"Error updating criteria: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCriteriaAsync(int id)
        {
            try
            {
                var criteria = await _criteriaRepository.GetByIdAsync(id);
                if (criteria == null)
                {
                    return new BaseResponse<bool>("Criteria not found", StatusCodeEnum.NotFound_404, false);
                }

                await _criteriaRepository.DeleteAsync(criteria);
                return new BaseResponse<bool>("Criteria deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting criteria: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<CriteriaResponse>>> GetCriteriaByRubricIdAsync(int rubricId)
        {
            try
            {
                var criteria = await _criteriaRepository.GetByRubricIdAsync(rubricId);
                var response = _mapper.Map<IEnumerable<CriteriaResponse>>(criteria);

                return new BaseResponse<IEnumerable<CriteriaResponse>>("Criteria retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaResponse>>($"Error retrieving criteria: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CriteriaResponse>>> GetCriteriaByTemplateIdAsync(int criteriaTemplateId)
        {
            try
            {
                var criteria = await _criteriaRepository.GetByCriteriaTemplateIdAsync(criteriaTemplateId);
                var response = _mapper.Map<IEnumerable<CriteriaResponse>>(criteria);

                return new BaseResponse<IEnumerable<CriteriaResponse>>("Criteria retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaResponse>>($"Error retrieving criteria: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        }
    }