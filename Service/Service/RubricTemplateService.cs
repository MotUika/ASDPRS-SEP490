using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.RubricTemplate;
using Service.RequestAndResponse.Response.RubricTemplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class RubricTemplateService : IRubricTemplateService
    {
        private readonly IRubricTemplateRepository _rubricTemplateRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public RubricTemplateService(IRubricTemplateRepository rubricTemplateRepository, ASDPRSContext context, IMapper mapper)
        {
            _rubricTemplateRepository = rubricTemplateRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<RubricTemplateResponse>> GetRubricTemplateByIdAsync(int id)
        {
            try
            {
                var rubricTemplate = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .FirstOrDefaultAsync(rt => rt.TemplateId == id);

                if (rubricTemplate == null)
                {
                    return new BaseResponse<RubricTemplateResponse>("Rubric template not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<RubricTemplateResponse>(rubricTemplate);
                return new BaseResponse<RubricTemplateResponse>("Rubric template retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricTemplateResponse>($"Error retrieving rubric template: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetAllRubricTemplatesAsync()
        {
            try
            {
                var rubricTemplates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>("Rubric templates retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"Error retrieving rubric templates: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<RubricTemplateResponse>> CreateRubricTemplateAsync(CreateRubricTemplateRequest request)
        {
            try
            {
                // Validate if user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == request.CreatedByUserId);
                if (!userExists)
                {
                    return new BaseResponse<RubricTemplateResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                // Check for duplicate title for the same user
                var duplicateTemplate = await _context.RubricTemplates
                    .AnyAsync(rt => rt.CreatedByUserId == request.CreatedByUserId && rt.Title == request.Title);

                if (duplicateTemplate)
                {
                    return new BaseResponse<RubricTemplateResponse>("Rubric template with the same title already exists for this user", StatusCodeEnum.BadRequest_400, null);
                }

                var rubricTemplate = _mapper.Map<RubricTemplate>(request);
                rubricTemplate.CreatedAt = DateTime.UtcNow;

                var createdRubricTemplate = await _rubricTemplateRepository.AddAsync(rubricTemplate);

                // Reload with related data for response
                var rubricTemplateWithDetails = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .FirstOrDefaultAsync(rt => rt.TemplateId == createdRubricTemplate.TemplateId);

                var response = _mapper.Map<RubricTemplateResponse>(rubricTemplateWithDetails);
                return new BaseResponse<RubricTemplateResponse>("Rubric template created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricTemplateResponse>($"Error creating rubric template: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<RubricTemplateResponse>> UpdateRubricTemplateAsync(UpdateRubricTemplateRequest request)
        {
            try
            {
                var existingRubricTemplate = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .FirstOrDefaultAsync(rt => rt.TemplateId == request.TemplateId);

                if (existingRubricTemplate == null)
                {
                    return new BaseResponse<RubricTemplateResponse>("Rubric template not found", StatusCodeEnum.NotFound_404, null);
                }

                // Check for duplicate title if provided
                if (!string.IsNullOrEmpty(request.Title) && request.Title != existingRubricTemplate.Title)
                {
                    var duplicateTemplate = await _context.RubricTemplates
                        .AnyAsync(rt => rt.CreatedByUserId == existingRubricTemplate.CreatedByUserId &&
                                       rt.Title == request.Title &&
                                       rt.TemplateId != request.TemplateId);

                    if (duplicateTemplate)
                    {
                        return new BaseResponse<RubricTemplateResponse>("Rubric template with the same title already exists for this user", StatusCodeEnum.BadRequest_400, null);
                    }
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(request.Title))
                    existingRubricTemplate.Title = request.Title;

                existingRubricTemplate.IsPublic = request.IsPublic;

                var updatedRubricTemplate = await _rubricTemplateRepository.UpdateAsync(existingRubricTemplate);
                var response = _mapper.Map<RubricTemplateResponse>(updatedRubricTemplate);

                return new BaseResponse<RubricTemplateResponse>("Rubric template updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricTemplateResponse>($"Error updating rubric template: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteRubricTemplateAsync(int id)
        {
            try
            {
                var rubricTemplate = await _context.RubricTemplates
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .FirstOrDefaultAsync(rt => rt.TemplateId == id);

                if (rubricTemplate == null)
                {
                    return new BaseResponse<bool>("Rubric template not found", StatusCodeEnum.NotFound_404, false);
                }

                // Check if rubric template has rubrics or criteria templates
                if (rubricTemplate.Rubrics.Any() || rubricTemplate.CriteriaTemplates.Any())
                {
                    return new BaseResponse<bool>("Cannot delete rubric template that has rubrics or criteria templates", StatusCodeEnum.BadRequest_400, false);
                }

                await _rubricTemplateRepository.DeleteAsync(rubricTemplate);
                return new BaseResponse<bool>("Rubric template deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting rubric template: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetRubricTemplatesByUserIdAsync(int userId)
        {
            try
            {
                var rubricTemplates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Where(rt => rt.CreatedByUserId == userId)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>("Rubric templates retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"Error retrieving rubric templates: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetPublicRubricTemplatesAsync()
        {
            try
            {
                var rubricTemplates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Where(rt => rt.IsPublic)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>("Public rubric templates retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"Error retrieving public rubric templates: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> SearchRubricTemplatesAsync(string searchTerm)
        {
            try
            {
                var rubricTemplates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Where(rt => rt.Title.Contains(searchTerm) ||
                                 (rt.CreatedByUser.FirstName + " " + rt.CreatedByUser.LastName).Contains(searchTerm))
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>("Rubric templates retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"Error searching rubric templates: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}