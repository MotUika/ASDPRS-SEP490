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
                // 🧩 Kiểm tra trạng thái Assignment nếu Rubric đã gán
                var rubric = await _context.Rubrics
                    .Include(r => r.Assignment)
                    .FirstOrDefaultAsync(r => r.RubricId == request.RubricId);

                if (rubric?.Assignment != null)
                {
                    var status = rubric.Assignment.Status;
                    if (status != "Draft" && status != "Upcoming")
                    {
                        return new BaseResponse<CriteriaResponse>(
                            "Criteria can only be created when the related assignment is in 'Draft' or 'Upcoming' status",
                            StatusCodeEnum.BadRequest_400,
                            null
                        );
                    }
                }
                // 🟩 Kiểm tra tổng trọng số hiện tại
                var totalWeight = await _context.Criteria
                    .Where(c => c.RubricId == request.RubricId)
                    .SumAsync(c => c.Weight);

                // 🟩 Nếu thêm vào mà vượt quá 100% thì chặn lại
                if (totalWeight + request.Weight > 100)
                {
                    return new BaseResponse<CriteriaResponse>(
                        $"❌ Cannot add criteria. Total weight would exceed 100%. Current total: {totalWeight}%",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // 🟩 Kiểm tra hệ số MaxScore
                var existingMaxScore = await _context.Criteria
                    .Where(c => c.RubricId == request.RubricId)
                    .Select(c => c.MaxScore)
                    .FirstOrDefaultAsync();

                if (existingMaxScore != 0 && existingMaxScore != request.MaxScore)
                {
                    return new BaseResponse<CriteriaResponse>(
                        $"❌ MaxScore must be consistent with existing criteria. Current MaxScore: {existingMaxScore}",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }
                var criteria = _mapper.Map<Criteria>(request);
                var createdCriteria = await _criteriaRepository.AddAsync(criteria);
                var response = _mapper.Map<CriteriaResponse>(createdCriteria);

                // Validate total weight after creation
                var validateResult = await ValidateTotalWeightAsync(createdCriteria.RubricId);
                var message = validateResult.Data == 100
                    ? "✅ Criteria created successfully. Total weight = 100%"
                    : $"⚠️ Criteria created successfully. Current total weight = {validateResult.Data}% (should be 100%)";
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

                // 🧩 Kiểm tra trạng thái Assignment nếu Criteria thuộc Rubric
                if (existingCriteria.RubricId != 0)
                {
                    var rubric = await _context.Rubrics
                        .Include(r => r.Assignment)
                        .FirstOrDefaultAsync(r => r.RubricId == existingCriteria.RubricId);

                    if (rubric?.Assignment != null)
                    {
                        var status = rubric.Assignment.Status;
                        if (status != "Draft" && status != "Upcoming")
                        {
                            return new BaseResponse<CriteriaResponse>(
                                "Criteria can only be edited when the related assignment is in 'Draft' or 'Upcoming' status",
                                StatusCodeEnum.BadRequest_400,
                                null
                            );
                        }
                    }
                }

                // 🟩 Tính tổng weight hiện tại (loại trừ chính criteria đang update)
                var currentTotalWeight = await _context.Criteria
                    .Where(c => c.RubricId == existingCriteria.RubricId && c.CriteriaId != existingCriteria.CriteriaId)
                    .SumAsync(c => c.Weight);

                // 🟩 Kiểm tra nếu update vượt quá 100%
                if (currentTotalWeight + request.Weight > 100)
                {
                    return new BaseResponse<CriteriaResponse>(
                        $"❌ Cannot update criteria. Total weight would exceed 100%. Current total (excluding this one): {currentTotalWeight}%",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }
                // 🟩 Kiểm tra hệ số MaxScore
                var existingMaxScore = await _context.Criteria
                    .Where(c => c.RubricId == request.RubricId)
                    .Select(c => c.MaxScore)
                    .FirstOrDefaultAsync();

                if (existingMaxScore != 0 && existingMaxScore != request.MaxScore)
                {
                    return new BaseResponse<CriteriaResponse>(
                        $"❌ MaxScore must be consistent with existing criteria. Current MaxScore: {existingMaxScore}",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                _mapper.Map(request, existingCriteria);
                var updatedCriteria = await _criteriaRepository.UpdateAsync(existingCriteria);
                var response = _mapper.Map<CriteriaResponse>(updatedCriteria);

                // Validate total weight after update
                var validateResult = await ValidateTotalWeightAsync(updatedCriteria.RubricId);
                var message = validateResult.Data == 100
                    ? "✅ Criteria updated successfully. Total weight = 100%"
                    : $"⚠️ Criteria updated successfully. Current total weight = {validateResult.Data}% (should be 100%)";

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
                // 🧩 Kiểm tra trạng thái Assignment nếu Criteria thuộc Rubric đã gán Assignment
                var rubric = await _context.Rubrics
                    .Include(r => r.Assignment)
                    .FirstOrDefaultAsync(r => r.RubricId == criteria.RubricId);

                if (rubric?.Assignment != null)
                {
                    var status = rubric.Assignment.Status;
                    if (status != "Draft" && status != "Upcoming")
                    {
                        return new BaseResponse<bool>(
                            "Criteria can only be deleted when the related assignment is in 'Draft' or 'Upcoming' status",
                            StatusCodeEnum.BadRequest_400,
                            false
                        );
                    }
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
                var criteriaList = await _criteriaRepository.GetByRubricIdAsync(rubricId);

                var response = criteriaList.Select(c => new CriteriaResponse
                {
                    CriteriaId = c.CriteriaId,
                    RubricId = c.RubricId,
                    RubricTitle = c.Rubric?.Title,
                    CriteriaTemplateId = c.CriteriaTemplateId,
                    CriteriaTemplateTitle = c.CriteriaTemplate?.Title,
                    Title = c.Rubric.Assignment?.Title,
                    Description = c.Description,
                    Weight = c.Weight,
                    MaxScore = c.MaxScore,
                    ScoringType = c.ScoringType,
                    ScoreLabel = c.ScoreLabel,
                    IsModified = c.IsModified,
                    CriteriaFeedbackCount = c.CriteriaFeedbacks?.Count ?? 0,

                    // 🟩 Thêm 2 dòng này:
                    CourseName = c.Rubric?.Assignment?.CourseInstance?.Course?.CourseName,
                    ClassName = c.Rubric?.Assignment?.CourseInstance?.SectionCode,
                    AssignmentStatus = c.Rubric?.Assignment?.Status,
                    AssignmentTitle = c.Rubric?.Assignment?.Title
                });

                return new BaseResponse<IEnumerable<CriteriaResponse>>(
                    "Criteria retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaResponse>>(
                    $"Error retrieving criteria: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
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

        public async Task<BaseResponse<decimal>> ValidateTotalWeightAsync(int rubricId)
        {
            try
            {
                var totalWeight = await _context.Criteria
                    .Where(c => c.RubricId == rubricId)
                    .SumAsync(c => c.Weight);

                var message = totalWeight == 100
                    ? "✅ Total criteria weight equals 100%"
                    : $"⚠️ Current total weight is {totalWeight}% (should be 100%)";

                return new BaseResponse<decimal>(
                    message,
                    StatusCodeEnum.OK_200,
                    totalWeight
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<decimal>(
                    $"Error calculating total weight: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    0
                );
            }
        }


        public async Task<BaseResponse<IEnumerable<CriteriaResponse>>> GetCriteriaByAssignmentIdAsync(int assignmentId)
        {
            try
            {
                // 🟩 Kiểm tra Assignment tồn tại
                var assignment = await _context.Assignments
                    .Include(a => a.Rubric)
                    .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

                if (assignment == null)
                {
                    return new BaseResponse<IEnumerable<CriteriaResponse>>(
                        $"AssignmentId {assignmentId} not found.",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                // 🟩 Lấy danh sách tiêu chí thuộc rubric của assignment
                if (assignment.RubricId == null)
                {
                    return new BaseResponse<IEnumerable<CriteriaResponse>>(
                        $"Assignment {assignment.Title} does not have an assigned rubric.",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                var criteriaList = await _context.Criteria
                    .Where(c => c.RubricId == assignment.RubricId)
                    .Include(c => c.CriteriaTemplate)
                    .Include(c => c.CriteriaFeedbacks)
                    .ToListAsync();

                if (!criteriaList.Any())
                {
                    return new BaseResponse<IEnumerable<CriteriaResponse>>(
                        $"No criteria found for AssignmentId {assignmentId}.",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                // 🟩 Map sang response
                var response = criteriaList.Select(c => new CriteriaResponse
                {
                    CriteriaId = c.CriteriaId,
                    RubricId = c.RubricId,
                    RubricTitle = c.Rubric?.Title,
                    CriteriaTemplateId = c.CriteriaTemplateId,
                    CriteriaTemplateTitle = c.CriteriaTemplate?.Title,
                    Title = c.Title,
                    Description = c.Description,
                    Weight = c.Weight,
                    MaxScore = c.MaxScore,
                    ScoringType = c.ScoringType,
                    ScoreLabel = c.ScoreLabel,
                    IsModified = c.IsModified,
                    CriteriaFeedbackCount = c.CriteriaFeedbacks?.Count ?? 0
                });

                return new BaseResponse<IEnumerable<CriteriaResponse>>(
                    "Criteria retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaResponse>>(
                    $"Error retrieving criteria for assignment: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }

    }
}