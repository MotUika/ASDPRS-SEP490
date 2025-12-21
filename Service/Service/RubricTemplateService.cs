using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.RubricTemplate;
using Service.RequestAndResponse.Response.CriteriaTemplate;
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
                var rubricTemplate = await _rubricTemplateRepository.GetByIdWithDetailsAsync(id);

                if (rubricTemplate == null)
                {
                    return new BaseResponse<RubricTemplateResponse>(
                        "Rubric template not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = _mapper.Map<RubricTemplateResponse>(rubricTemplate);

                var assignments = await _rubricTemplateRepository.GetAssignmentsUsingTemplateAsync(id);

                response.AssignmentsUsingTemplate = (assignments != null && assignments.Any())
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

                return new BaseResponse<RubricTemplateResponse>(
                    "Rubric template retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricTemplateResponse>(
                    $"Error retrieving rubric template: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }


        //public async Task<BaseResponse<RubricTemplateResponse>> GetRubricTemplateByIdAsync(int id)
        //{
        //    try
        //    {
        //        var rubricTemplate = await _context.RubricTemplates
        //            .Include(rt => rt.CreatedByUser)
        //            .Include(rt => rt.Rubrics)
        //            .Include(rt => rt.CriteriaTemplates)
        //            .FirstOrDefaultAsync(rt => rt.TemplateId == id);

        //        if (rubricTemplate == null)
        //        {
        //            return new BaseResponse<RubricTemplateResponse>("Rubric template not found", StatusCodeEnum.NotFound_404, null);
        //        }

        //        var response = _mapper.Map<RubricTemplateResponse>(rubricTemplate);
        //        return new BaseResponse<RubricTemplateResponse>("Rubric template retrieved successfully", StatusCodeEnum.OK_200, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<RubricTemplateResponse>($"Error retrieving rubric template: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
        //    }
        //}



        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetAllRubricTemplatesAsync()
        {
            try
            {
                var rubricTemplates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Course)
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
                var userExists = await _context.Users.AnyAsync(u => u.Id == request.CreatedByUserId);
                if (!userExists)
                {
                    return new BaseResponse<RubricTemplateResponse>("User not found", StatusCodeEnum.NotFound_404, null);
                }

                if (request.CourseId.HasValue && request.CourseId > 0)
                {
                    var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == request.CourseId);
                    if (!courseExists)
                    {
                        return new BaseResponse<RubricTemplateResponse>($"Course with Id {request.CourseId} not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                var duplicateTemplate = await _context.RubricTemplates
                    .AnyAsync(rt => rt.CreatedByUserId == request.CreatedByUserId && rt.Title == request.Title);

                if (duplicateTemplate)
                {
                    return new BaseResponse<RubricTemplateResponse>("Rubric template with the same title already exists for this user", StatusCodeEnum.BadRequest_400, null);
                }

                var rubricTemplate = _mapper.Map<RubricTemplate>(request);

                rubricTemplate.CourseId = request.CourseId;

                rubricTemplate.CreatedAt = DateTime.UtcNow.AddHours(7);
                rubricTemplate.IsPublic = true;

                var createdRubricTemplate = await _rubricTemplateRepository.AddAsync(rubricTemplate);

                var rubricTemplateWithDetails = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Course)
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
                    .Include(rt => rt.Course)
                    .FirstOrDefaultAsync(rt => rt.TemplateId == request.TemplateId);

                if (existingRubricTemplate == null)
                {
                    return new BaseResponse<RubricTemplateResponse>("Rubric template not found", StatusCodeEnum.NotFound_404, null);
                }

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

                    existingRubricTemplate.Title = request.Title;
                }


                if (request.CourseId != existingRubricTemplate.CourseId)
                {
                    if (request.CourseId.HasValue)
                    {
                        var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == request.CourseId.Value);
                        if (!courseExists)
                        {
                            return new BaseResponse<RubricTemplateResponse>("Course not found", StatusCodeEnum.NotFound_404, null);
                        }
                    }

                    existingRubricTemplate.CourseId = request.CourseId;
                }

                await _rubricTemplateRepository.UpdateAsync(existingRubricTemplate);

                var rubricTemplateWithDetails = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Course)
                    .FirstOrDefaultAsync(rt => rt.TemplateId == existingRubricTemplate.TemplateId);

                var response = _mapper.Map<RubricTemplateResponse>(rubricTemplateWithDetails);

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

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetRubricTemplatesByUserIdAsync(int userId, int courseInstanceId)
        {
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"UserId {userId} not found.", StatusCodeEnum.NotFound_404, null);
                }

                var courseInstance = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == courseInstanceId);

                if (courseInstance == null)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"CourseInstanceId {courseInstanceId} not found.", StatusCodeEnum.NotFound_404, null);
                }

                var targetCourseId = courseInstance.CourseId;

                var isInstructor = await _context.CourseInstructors
                    .AnyAsync(ci => ci.UserId == userId && ci.CourseInstanceId == courseInstanceId);

                if (!isInstructor)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} is not an instructor for CourseInstanceId {courseInstanceId}.",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                var rubricTemplates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Course)
                    .Where(rt =>
                        (rt.CreatedByUserId == userId || rt.IsPublic) &&
                        (rt.CourseId == targetCourseId || rt.CourseId == null)
                    )
                    .ToListAsync();

                if (!rubricTemplates.Any())
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"No rubric templates found for UserId {userId} in Course {courseInstance.Course?.CourseName}.",
                        StatusCodeEnum.OK_200,
                        new List<RubricTemplateResponse>());
                }

                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);

                foreach (var template in response)
                {
                    var assignments = await _rubricTemplateRepository.GetAssignmentsUsingTemplateAsync(template.TemplateId);

                    template.AssignmentsUsingTemplate = (assignments != null && assignments.Any())
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

                return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    $"Found {response.Count()} rubric template(s).",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    $"Error retrieving rubric templates: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }



        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetPublicRubricTemplatesAsync()
        {
            try
            {
                // Lấy tất cả rubric templates công khai
                var rubricTemplates = await _rubricTemplateRepository.GetPublicWithDetailsAsync();

                // Map sang response
                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);

                // Lấy assignments cho từng rubric template
                foreach (var template in response)
                {
                    var assignments = await _rubricTemplateRepository.GetAssignmentsUsingTemplateAsync(template.TemplateId);

                    template.AssignmentsUsingTemplate = (assignments != null && assignments.Any())
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

                return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    "Public rubric templates retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    $"Error retrieving public rubric templates: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
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
                    .Include(rt => rt.Course)
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

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetRubricTemplatesByUserAndCourseAsync(int userId, int courseId)
        {
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"UserId {userId} not found.", StatusCodeEnum.BadRequest_400, null);
                }

                var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == courseId);
                if (!courseExists)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"CourseId {courseId} not found.", StatusCodeEnum.BadRequest_400, null);
                }

                var isTeaching = await _context.CourseInstructors
                    .AnyAsync(ci => ci.UserId == userId && ci.CourseInstance.CourseId == courseId);

                if (!isTeaching)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} does not teach any instance of CourseId {courseId}.",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                var templates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Course)
                    .Where(rt =>
                        (rt.IsPublic && rt.CourseId == courseId) ||
                        (rt.CreatedByUserId == userId && (rt.CourseId == courseId || rt.CourseId == null))
                    )
                    .ToListAsync();

                if (!templates.Any())
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"No templates found for UserId {userId} and CourseId {courseId}.",
                        StatusCodeEnum.OK_200,
                        new List<RubricTemplateResponse>());
                }

                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(templates);

                foreach (var template in response)
                {
                    var assignments = await _rubricTemplateRepository.GetAssignmentsUsingTemplateAsync(template.TemplateId);
                    template.AssignmentsUsingTemplate = assignments?.Any() == true
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

                return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    $"Found {response.Count()} template(s) for UserId {userId} and CourseId {courseId}.",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    $"Error retrieving templates: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetPublicRubricTemplatesByUserIdAsync(int userId)
        {
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>($"UserId {userId} not found.", StatusCodeEnum.NotFound_404, null);
                }

                var taughtCourseIds = await _context.CourseInstructors
                    .Where(ci => ci.UserId == userId)
                    .Select(ci => ci.CourseInstance.CourseId)
                    .Distinct()
                    .ToListAsync();

                if (!taughtCourseIds.Any())
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} is not teaching any courses.",
                        StatusCodeEnum.OK_200,
                        new List<RubricTemplateResponse>());
                }

                var rubricTemplates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Course)
                    .Where(rt => rt.IsPublic && rt.CourseId.HasValue && taughtCourseIds.Contains(rt.CourseId.Value))
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);

                foreach (var template in response)
                {
                    var assignments = await _rubricTemplateRepository.GetAssignmentsUsingTemplateAsync(template.TemplateId);
                    template.AssignmentsUsingTemplate = (assignments != null && assignments.Any())
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

                return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    $"Found {response.Count()} public template(s) relevant to user's courses.",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                    $"Error retrieving public rubric templates: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<RubricTemplateResponse>> ToggleRubricTemplatePublicStatusAsync(int templateId, bool makePublic)
        {
            try
            {
                // 1️⃣ Lấy rubric template cùng các criteria
                var rubricTemplate = await _context.RubricTemplates
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .FirstOrDefaultAsync(rt => rt.TemplateId == templateId);

                if (rubricTemplate == null)
                {
                    return new BaseResponse<RubricTemplateResponse>(
                        $"Rubric template with ID {templateId} not found.",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // 2️⃣ Nếu muốn public, check điều kiện
                if (makePublic)
                {
                    if (!rubricTemplate.CriteriaTemplates.Any())
                    {
                        return new BaseResponse<RubricTemplateResponse>(
                            "Cannot make rubric template public: it has no criteria.",
                            StatusCodeEnum.BadRequest_400,
                            null);
                    }

                    var totalWeight = rubricTemplate.CriteriaTemplates.Sum(c => c.Weight);
                    if (totalWeight != 100)
                    {
                        return new BaseResponse<RubricTemplateResponse>(
                            $"Cannot make rubric template public: total criteria weight is {totalWeight}, must be 100.",
                            StatusCodeEnum.BadRequest_400,
                            null);
                    }
                }

                // 3️⃣ Cập nhật trạng thái IsPublic
                rubricTemplate.IsPublic = makePublic;
                await _rubricTemplateRepository.UpdateAsync(rubricTemplate);

                var response = _mapper.Map<RubricTemplateResponse>(rubricTemplate);
                return new BaseResponse<RubricTemplateResponse>(
                    $"Rubric template {(makePublic ? "made public" : "set to private")} successfully.",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<RubricTemplateResponse>(
                    $"Error updating rubric template public status: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

    }
}