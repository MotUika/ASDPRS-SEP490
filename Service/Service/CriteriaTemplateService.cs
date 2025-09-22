using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CriteriaTemplate;
using Service.RequestAndResponse.Response.CriteriaTemplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CriteriaTemplateService : ICriteriaTemplateService
    {
        private readonly ICriteriaTemplateRepository _criteriaTemplateRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public CriteriaTemplateService(ICriteriaTemplateRepository criteriaTemplateRepository, ASDPRSContext context, IMapper mapper)
        {
            _criteriaTemplateRepository = criteriaTemplateRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<CriteriaTemplateResponse>> GetCriteriaTemplateByIdAsync(int id)
        {
            try
            {
                var criteriaTemplate = await _context.CriteriaTemplates
                    .Include(ct => ct.Template)
                    .Include(ct => ct.Criteria)
                    .FirstOrDefaultAsync(ct => ct.CriteriaTemplateId == id);

                if (criteriaTemplate == null)
                {
                    return new BaseResponse<CriteriaTemplateResponse>("Criteria template not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<CriteriaTemplateResponse>(criteriaTemplate);
                response.CriteriaCount = criteriaTemplate.Criteria?.Count ?? 0;
                response.TemplateTitle = criteriaTemplate.Template?.Title;

                return new BaseResponse<CriteriaTemplateResponse>("Criteria template retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaTemplateResponse>($"Error retrieving criteria template: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CriteriaTemplateResponse>>> GetAllCriteriaTemplatesAsync()
        {
            try
            {
                var criteriaTemplates = await _context.CriteriaTemplates
                    .Include(ct => ct.Template)
                    .Include(ct => ct.Criteria)
                    .ToListAsync();

                var response = criteriaTemplates.Select(ct =>
                {
                    var criteriaTemplateResponse = _mapper.Map<CriteriaTemplateResponse>(ct);
                    criteriaTemplateResponse.CriteriaCount = ct.Criteria?.Count ?? 0;
                    criteriaTemplateResponse.TemplateTitle = ct.Template?.Title;
                    return criteriaTemplateResponse;
                });

                return new BaseResponse<IEnumerable<CriteriaTemplateResponse>>("Criteria templates retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaTemplateResponse>>($"Error retrieving criteria templates: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CriteriaTemplateResponse>> CreateCriteriaTemplateAsync(CreateCriteriaTemplateRequest request)
        {
            try
            {
                var criteriaTemplate = _mapper.Map<CriteriaTemplate>(request);
                var createdCriteriaTemplate = await _criteriaTemplateRepository.AddAsync(criteriaTemplate);
                var response = _mapper.Map<CriteriaTemplateResponse>(createdCriteriaTemplate);

                return new BaseResponse<CriteriaTemplateResponse>("Criteria template created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaTemplateResponse>($"Error creating criteria template: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CriteriaTemplateResponse>> UpdateCriteriaTemplateAsync(UpdateCriteriaTemplateRequest request)
        {
            try
            {
                var existingCriteriaTemplate = await _criteriaTemplateRepository.GetByIdAsync(request.CriteriaTemplateId);
                if (existingCriteriaTemplate == null)
                {
                    return new BaseResponse<CriteriaTemplateResponse>("Criteria template not found", StatusCodeEnum.NotFound_404, null);
                }

                _mapper.Map(request, existingCriteriaTemplate);
                var updatedCriteriaTemplate = await _criteriaTemplateRepository.UpdateAsync(existingCriteriaTemplate);
                var response = _mapper.Map<CriteriaTemplateResponse>(updatedCriteriaTemplate);

                return new BaseResponse<CriteriaTemplateResponse>("Criteria template updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaTemplateResponse>($"Error updating criteria template: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCriteriaTemplateAsync(int id)
        {
            try
            {
                var criteriaTemplate = await _criteriaTemplateRepository.GetByIdAsync(id);
                if (criteriaTemplate == null)
                {
                    return new BaseResponse<bool>("Criteria template not found", StatusCodeEnum.NotFound_404, false);
                }

                await _criteriaTemplateRepository.DeleteAsync(criteriaTemplate);
                return new BaseResponse<bool>("Criteria template deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting criteria template: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<CriteriaTemplateResponse>>> GetCriteriaTemplatesByTemplateIdAsync(int templateId)
        {
            try
            {
                var criteriaTemplates = await _criteriaTemplateRepository.GetByTemplateIdAsync(templateId);
                var response = _mapper.Map<IEnumerable<CriteriaTemplateResponse>>(criteriaTemplates);

                return new BaseResponse<IEnumerable<CriteriaTemplateResponse>>("Criteria templates retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaTemplateResponse>>($"Error retrieving criteria templates: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}