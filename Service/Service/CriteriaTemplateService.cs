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
                // 🟩 Kiểm tra tổng trọng số hiện tại của rubric template
                var totalWeight = await _context.CriteriaTemplates
                    .Where(ct => ct.TemplateId == request.TemplateId)
                    .SumAsync(ct => ct.Weight);

                // 🟩 Nếu thêm mới mà tổng > 100% thì chặn lại
                if (totalWeight + request.Weight > 100)
                {
                    return new BaseResponse<CriteriaTemplateResponse>(
                        $"❌ Cannot add criteria template. Total weight would exceed 100%. Current total: {totalWeight}%",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // 🟩 Tạo mới criteria template
                var criteriaTemplate = _mapper.Map<CriteriaTemplate>(request);
                var createdCriteriaTemplate = await _criteriaTemplateRepository.AddAsync(criteriaTemplate);
                var response = _mapper.Map<CriteriaTemplateResponse>(createdCriteriaTemplate);

                // 🟩 Kiểm tra lại tổng trọng số sau khi thêm
                var newTotalWeight = await _context.CriteriaTemplates
                    .Where(ct => ct.TemplateId == request.TemplateId)
                    .SumAsync(ct => ct.Weight);

                var message = newTotalWeight == 100
                    ? "✅ Criteria template created successfully. Total weight = 100%"
                    : $"⚠️ Criteria template created successfully. Current total weight = {newTotalWeight}% (should be 100%)";

                return new BaseResponse<CriteriaTemplateResponse>(
                    message,
                    StatusCodeEnum.Created_201,
                    response
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaTemplateResponse>(
                    $"Error creating criteria template: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
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

                // 🟩 Validate tổng weight sau khi update (loại trừ weight cũ của template này)
                var totalWeightExcludingCurrent = await _context.CriteriaTemplates
                    .Where(ct => ct.TemplateId == existingCriteriaTemplate.TemplateId && ct.CriteriaTemplateId != existingCriteriaTemplate.CriteriaTemplateId)
                    .SumAsync(ct => ct.Weight);

                if (totalWeightExcludingCurrent + request.Weight > 100)
                {
                    return new BaseResponse<CriteriaTemplateResponse>(
                        $"❌ Cannot update criteria template. Total weight would exceed 100%. Current total (excluding this one): {totalWeightExcludingCurrent}%",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // 🟩 Map và update
                _mapper.Map(request, existingCriteriaTemplate);
                var updatedCriteriaTemplate = await _criteriaTemplateRepository.UpdateAsync(existingCriteriaTemplate);
                var response = _mapper.Map<CriteriaTemplateResponse>(updatedCriteriaTemplate);

                // 🟩 Check tổng weight sau khi update
                var (_, newTotalWeight, newMessage) = await ValidateTotalWeightAsync(existingCriteriaTemplate.TemplateId);
                newMessage = newTotalWeight == 100
                    ? "✅ Criteria template updated successfully. Total weight = 100%"
                    : $"⚠️ Criteria template updated successfully. Current total weight = {newTotalWeight}% (should be 100%)";

                return new BaseResponse<CriteriaTemplateResponse>(newMessage, StatusCodeEnum.OK_200, response);
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

        public async Task<(bool isValid, int totalWeight, string message)> ValidateTotalWeightAsync(int templateId, int? additionalWeight = null)
        {
            var totalWeight = await _context.CriteriaTemplates
                .Where(ct => ct.TemplateId == templateId)
                .SumAsync(ct => ct.Weight);

            if (additionalWeight.HasValue)
                totalWeight += additionalWeight.Value;

            if (totalWeight > 100)
                return (false, totalWeight, $"❌ Total weight would exceed 100%. Current total with addition: {totalWeight}%");

            return (true, totalWeight, $"✅ Total weight is valid: {totalWeight}%");
        }

    }
}