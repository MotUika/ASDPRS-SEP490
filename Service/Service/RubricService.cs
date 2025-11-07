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
using Service.RequestAndResponse.Response.RubricTemplate;
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

                var response = _mapper.Map<IEnumerable<RubricResponse>>(rubrics);
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
                // Validate template if provided
                if (request.TemplateId.HasValue && request.TemplateId > 0)
                {
                    var templateExists = await _context.RubricTemplates.AnyAsync(rt => rt.TemplateId == request.TemplateId);
                    if (!templateExists)
                    {
                        return new BaseResponse<RubricResponse>("Rubric template not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                // Validate assignment if provided
                if (request.AssignmentId.HasValue && request.AssignmentId > 0)
                {
                    var assignmentExists = await _context.Assignments.AnyAsync(a => a.AssignmentId == request.AssignmentId);
                    if (!assignmentExists)
                    {
                        return new BaseResponse<RubricResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                // Check for duplicate title for the same template/assignment
                var duplicateRubric = await _context.Rubrics
                    .AnyAsync(r =>
                        (r.TemplateId == request.TemplateId ||
                         r.AssignmentId == request.AssignmentId) &&
                        r.Title == request.Title);

                if (duplicateRubric)
                {
                    return new BaseResponse<RubricResponse>("Rubric with the same title already exists for this template or assignment", StatusCodeEnum.BadRequest_400, null);
                }

                var rubric = _mapper.Map<Rubric>(request);
                var createdRubric = await _rubricRepository.AddAsync(rubric);

                // Reload with related data for response
                var rubricWithDetails = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                    .FirstOrDefaultAsync(r => r.RubricId == createdRubric.RubricId);

                var response = _mapper.Map<RubricResponse>(rubricWithDetails);
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
                var existingRubric = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                    .FirstOrDefaultAsync(r => r.RubricId == request.RubricId);

                if (existingRubric == null)
                {
                    return new BaseResponse<RubricResponse>("Rubric not found", StatusCodeEnum.NotFound_404, null);
                }

                // 🧩 Kiểm tra trạng thái assignment nếu rubric thuộc assignment
                if (existingRubric.AssignmentId.HasValue)
                {
                    var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.AssignmentId == existingRubric.AssignmentId);
                    if (assignment != null)
                    {
                        if (assignment.Status != "Upcoming" && assignment.Status != "Draft")
                        {
                            return new BaseResponse<RubricResponse>(
                                "Rubric can only be edited when the related assignment is in 'Draft' or 'Upcoming' status",
                                StatusCodeEnum.BadRequest_400,
                                null);
                        }
                    }
                }

                // Validate template if provided
                if (request.TemplateId.HasValue && request.TemplateId > 0 && request.TemplateId != existingRubric.TemplateId)
                {
                    var templateExists = await _context.RubricTemplates.AnyAsync(rt => rt.TemplateId == request.TemplateId);
                    if (!templateExists)
                    {
                        return new BaseResponse<RubricResponse>("Rubric template not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                // Validate assignment if provided
                if (request.AssignmentId.HasValue && request.AssignmentId > 0 && request.AssignmentId != existingRubric.AssignmentId)
                {
                    var assignmentExists = await _context.Assignments.AnyAsync(a => a.AssignmentId == request.AssignmentId);
                    if (!assignmentExists)
                    {
                        return new BaseResponse<RubricResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                // Check for duplicate title if provided
                if (!string.IsNullOrEmpty(request.Title) && request.Title != existingRubric.Title)
                {
                    var templateIdToCheck = request.TemplateId ?? existingRubric.TemplateId;
                    var assignmentIdToCheck = request.AssignmentId ?? existingRubric.AssignmentId;

                    var duplicateRubric = await _context.Rubrics
                        .AnyAsync(r =>
                            (r.TemplateId == templateIdToCheck ||
                             r.AssignmentId == assignmentIdToCheck) &&
                            r.Title == request.Title &&
                            r.RubricId != request.RubricId);

                    if (duplicateRubric)
                    {
                        return new BaseResponse<RubricResponse>("Rubric with the same title already exists for this template or assignment", StatusCodeEnum.BadRequest_400, null);
                    }
                }

                // Update only provided fields
                if (request.TemplateId.HasValue) existingRubric.TemplateId = request.TemplateId;
                if (request.AssignmentId.HasValue) existingRubric.AssignmentId = request.AssignmentId;
                if (!string.IsNullOrEmpty(request.Title)) existingRubric.Title = request.Title;

                existingRubric.IsModified = request.IsModified;

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
                var rubric = await _context.Rubrics
                    .Include(r => r.Criteria)
                    .FirstOrDefaultAsync(r => r.RubricId == id);

                if (rubric == null)
                {
                    return new BaseResponse<bool>("Rubric not found", StatusCodeEnum.NotFound_404, false);
                }

                // Check if rubric has criteria
                if (rubric.Criteria.Any())
                {
                    return new BaseResponse<bool>("Cannot delete rubric that has criteria", StatusCodeEnum.BadRequest_400, false);
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
                var rubrics = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                    .Where(r => r.TemplateId == templateId)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<RubricResponse>>(rubrics);
                return new BaseResponse<IEnumerable<RubricResponse>>("Rubrics retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricResponse>>($"Error retrieving rubrics: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<RubricResponse>>> GetRubricsByAssignmentIdAsync(int assignmentId)
        {
            try
            {
                var rubrics = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                    .Where(r => r.AssignmentId == assignmentId)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<RubricResponse>>(rubrics);
                return new BaseResponse<IEnumerable<RubricResponse>>("Rubrics retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricResponse>>($"Error retrieving rubrics: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

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

                // Map criteria list
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

        public async Task<BaseResponse<IEnumerable<RubricResponse>>> GetModifiedRubricsAsync()
        {
            try
            {
                var rubrics = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                    .Where(r => r.IsModified)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<RubricResponse>>(rubrics);
                return new BaseResponse<IEnumerable<RubricResponse>>("Modified rubrics retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricResponse>>($"Error retrieving modified rubrics: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<RubricResponse>> CreateRubricFromTemplateAsync(
    int templateId, int? assignmentId = null)
        {
            try
            {
                // 🔹 Load template + criteria templates
                var template = await _context.RubricTemplates
                    .Include(t => t.CriteriaTemplates)
                    .FirstOrDefaultAsync(t => t.TemplateId == templateId);

                if (template == null)
                    return new BaseResponse<RubricResponse>(
                        "Rubric template not found",
                        StatusCodeEnum.NotFound_404,
                        null);

                // 🔹 Validate assignment nếu có
                Assignment? assignment = null;
                if (assignmentId.HasValue)
                {
                    assignment = await _context.Assignments
                        .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId.Value);

                    if (assignment == null)
                        return new BaseResponse<RubricResponse>(
                            "Assignment not found",
                            StatusCodeEnum.NotFound_404,
                            null);
                }

                // 🔹 Tạo rubric mới
                var rubric = new Rubric
                {
                    TemplateId = templateId,
                    AssignmentId = assignmentId,
                    Title = template.Title ?? "Untitled Rubric",
                    IsModified = false
                };

                await _context.Rubrics.AddAsync(rubric);
                await _context.SaveChangesAsync(); // ✅ Tạo RubricId thật

                // 🔹 Clone toàn bộ criteria từ template
                if (template.CriteriaTemplates?.Any() == true)
                {
                    var criteriaToAdd = template.CriteriaTemplates.Select(ct => new Criteria
                    {
                        RubricId = rubric.RubricId,
                        CriteriaTemplateId = ct.CriteriaTemplateId,
                        Title = ct.Title,
                        Description = ct.Description,
                        MaxScore = ct.MaxScore,
                        Weight = ct.Weight,
                        ScoringType = ct.ScoringType ?? "Scale",
                        ScoreLabel = ct.ScoreLabel ?? "Points",
                        IsModified = false
                    }).ToList();

                    await _context.Criteria.AddRangeAsync(criteriaToAdd);
                    await _context.SaveChangesAsync();
                }

                // 🔹 Cập nhật assignment (nếu có) để gán rubric mới
                if (assignment != null)
                {
                    assignment.RubricId = rubric.RubricId;
                    assignment.RubricTemplateId = templateId;

                    _context.Assignments.Update(assignment);
                    await _context.SaveChangesAsync(); // ✅ Gán rubric mới cho assignment
                }

                // 🔹 Load rubric đầy đủ để trả về
                var rubricWithDetails = await _context.Rubrics
                    .Include(r => r.Template)
                    .Include(r => r.Assignment)
                    .Include(r => r.Criteria)
                        .ThenInclude(c => c.CriteriaTemplate)
                    .FirstOrDefaultAsync(r => r.RubricId == rubric.RubricId);

                var response = _mapper.Map<RubricResponse>(rubricWithDetails);

                return new BaseResponse<RubricResponse>(
                    "Rubric created from template successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return new BaseResponse<RubricResponse>(
                    $"Error creating rubric from template: {msg}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }



        //public async Task<BaseResponse<IEnumerable<RubricResponse>>> GetPublicRubricsAsync()
        //{
        //    var rubrics = await _rubricRepository
        //        .GetAllAsync()
        //        .Where(r => r.IsPublic)   // Cột IsPublic trong Rubric
        //        .Include(r => r.Criteria)
        //        .ToListAsync();

        //    var response = _mapper.Map<IEnumerable<RubricResponse>>(rubrics);

        //    return new BaseResponse<IEnumerable<RubricResponse>>(
        //        "Public rubrics retrieved successfully",
        //        StatusCodeEnum.OK_200,
        //        response
        //    );
        //}

        public async Task<BaseResponse<IEnumerable<RubricResponse>>> GetRubricsByUserIdAsync(int userId)
        {
            try
            {
                // 🔹 Lấy danh sách rubric có template do user này tạo
                var rubrics = await _context.Rubrics
                    .Include(r => r.Template)
                        .ThenInclude(t => t.CreatedByUser)
                    .Include(r => r.Assignment)
                        .ThenInclude(a => a.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                    .Include(r => r.Assignment)
                        .ThenInclude(a => a.CourseInstance)
                            .ThenInclude(ci => ci.Campus)
                    .Include(r => r.Criteria)
                    .Where(r => r.Template.CreatedByUserId == userId)
                    .ToListAsync();

                if (rubrics == null || !rubrics.Any())
                {
                    return new BaseResponse<IEnumerable<RubricResponse>>(
                        "No rubrics found for this user",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                // 🔹 Map sang response
                var response = _mapper.Map<IEnumerable<RubricResponse>>(rubrics);

                // 🔹 Gán danh sách assignment đang sử dụng rubric (giống bên RubricTemplateService)
                foreach (var rubric in response)
                {
                    var assignments = await _context.Assignments
                        .Include(a => a.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                        .Include(a => a.CourseInstance)
                            .ThenInclude(ci => ci.Campus)
                        .Where(a => a.RubricId == rubric.RubricId)
                        .ToListAsync();

                    rubric.AssignmentsUsingTemplate = (assignments != null && assignments.Any())
                        ? assignments.Select(a => new AssignmentUsingTemplateResponse
                        {
                            AssignmentId = a.AssignmentId,
                            Title = a.Title,
                            CourseName = a.CourseInstance?.Course?.CourseName,
                            ClassName = $"{a.CourseInstance?.Course?.CourseName} - {a.CourseInstance?.SectionCode}",
                            CampusName = a.CourseInstance?.Campus?.CampusName,
                            Deadline = a.Deadline
                        }).ToList()
                        : new List<AssignmentUsingTemplateResponse>();
                }

                return new BaseResponse<IEnumerable<RubricResponse>>(
                    "Rubrics retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricResponse>>(
                    $"Error retrieving rubrics by user: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }



    }
}