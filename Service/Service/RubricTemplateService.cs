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
                // 🧩 Lấy rubric template đầy đủ thông tin (CreatedByUser, Rubrics, CriteriaTemplates)
                var rubricTemplate = await _rubricTemplateRepository.GetByIdWithDetailsAsync(id);

                if (rubricTemplate == null)
                {
                    return new BaseResponse<RubricTemplateResponse>(
                        "Rubric template not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // 🧠 Dùng AutoMapper để map toàn bộ dữ liệu gốc
                var response = _mapper.Map<RubricTemplateResponse>(rubricTemplate);

                // 📘 Lấy danh sách assignments đang sử dụng rubric template này qua repository
                var assignments = await _rubricTemplateRepository.GetAssignmentsUsingTemplateAsync(id);

                // 🧩 Gán danh sách assignments vào response (luôn trả về [] thay vì null)
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
                    .Include(rt => rt.Major)
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
                var userExists = await _context.Users.AnyAsync(u => u.Id == request.CreatedByUserId);
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
                rubricTemplate.IsPublic = false;

                var createdRubricTemplate = await _rubricTemplateRepository.AddAsync(rubricTemplate);

                // Reload with related data for response
                var rubricTemplateWithDetails = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                     .Include(rt => rt.Major)
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
                // Lấy RubricTemplate hiện tại với các navigation property
                var existingRubricTemplate = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Major)
                    .FirstOrDefaultAsync(rt => rt.TemplateId == request.TemplateId);

                if (existingRubricTemplate == null)
                {
                    return new BaseResponse<RubricTemplateResponse>("Rubric template not found", StatusCodeEnum.NotFound_404, null);
                }

                // Check duplicate title nếu Title được thay đổi
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

                // Bỏ update IsPublic ở đây

                // Update MajorId nếu được cung cấp và khác với MajorId hiện tại
                if (request.MajorId.HasValue && request.MajorId.Value != existingRubricTemplate.MajorId)
                {
                    // Kiểm tra MajorId hợp lệ
                    var majorExists = await _context.Majors.AnyAsync(m => m.MajorId == request.MajorId.Value);
                    if (!majorExists)
                        return new BaseResponse<RubricTemplateResponse>("Major not found", StatusCodeEnum.NotFound_404, null);

                    existingRubricTemplate.MajorId = request.MajorId.Value;
                }

                // Lưu thay đổi
                await _rubricTemplateRepository.UpdateAsync(existingRubricTemplate);

                // Reload entity với navigation properties để map đầy đủ dữ liệu
                var rubricTemplateWithDetails = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Major)
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

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetRubricTemplatesByUserIdAsync(int userId)
        {
            try
            {
                // 🧩 Bước 1: Lấy user + toàn bộ thông tin major
                var user = await _context.Users
                    .Include(u => u.CourseInstructors)
                        .ThenInclude(ci => ci.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                                .ThenInclude(c => c.Curriculum)
                                    .ThenInclude(cur => cur.Major)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} not found.",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // 🧠 Bước 2: Xác định tất cả các major mà user liên quan
                var userMajorIds = new List<int>();

                // major trực tiếp của user (nếu có)
                if (user.MajorId.HasValue)
                    userMajorIds.Add(user.MajorId.Value);

                // major từ các khóa học user dạy
                var courseMajors = user.CourseInstructors
                    .Where(ci => ci.CourseInstance?.Course?.Curriculum?.Major != null)
                    .Select(ci => ci.CourseInstance.Course.Curriculum.Major.MajorId)
                    .Distinct()
                    .ToList();

                userMajorIds.AddRange(courseMajors);
                userMajorIds = userMajorIds.Distinct().ToList();

                if (!userMajorIds.Any())
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} has no associated major.",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                // 🧩 Bước 3: Lấy các RubricTemplate có Major phù hợp
                var rubricTemplates = await _context.RubricTemplates
                 .Include(rt => rt.CreatedByUser)
                 .Include(rt => rt.Rubrics)
                 .Include(rt => rt.CriteriaTemplates)
                 .Include(rt => rt.Major) // ✅ thêm dòng này
                 .Where(rt =>
                     (rt.CreatedByUserId == userId &&
                      (rt.MajorId == null || (rt.MajorId.HasValue && userMajorIds.Contains(rt.MajorId.Value))))
                     || (rt.IsPublic && rt.MajorId.HasValue && userMajorIds.Contains(rt.MajorId.Value))
                 )
                 .ToListAsync();

                if (!rubricTemplates.Any())
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"No rubric templates found for UserId {userId} with their major(s).",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // 🧠 Bước 4: Map sang response
                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);

                // 🧩 Bước 5: Gán danh sách assignment có CourseName và ClassName
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
                    $"Found {response.Count()} rubric template(s) accessible to UserId {userId}.",
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

        public async Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetRubricTemplatesByUserAndMajorAsync(int userId, int majorId)
        {
            try
            {
                // ✅ Bước 1: Kiểm tra userId tồn tại
                var user = await _context.Users
                    .Include(u => u.CourseInstructors)
                        .ThenInclude(ci => ci.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                                .ThenInclude(c => c.Curriculum)
                                    .ThenInclude(cur => cur.Major)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} not found in the system.",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // ✅ Bước 2: Kiểm tra MajorId tồn tại
                var majorExists = await _context.Majors.AnyAsync(m => m.MajorId == majorId);
                if (!majorExists)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"MajorId {majorId} does not exist in the system.",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // ✅ Bước 3: Kiểm tra user có dạy môn thuộc major này không
                var userMajorIds = user.CourseInstructors
                    .Select(ci => ci.CourseInstance.Course.Curriculum.Major.MajorId)
                    .Distinct()
                    .ToList();

                if (!userMajorIds.Contains(majorId))
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} does not teach any course in MajorId {majorId}.",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                // ✅ Bước 4: Lấy danh sách rubric template
                var templates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.CriteriaTemplates)
                    .Where(rt =>
                        (rt.IsPublic && rt.MajorId == majorId) ||     // Public rubric đúng ngành
                        (rt.CreatedByUserId == userId && (rt.MajorId == majorId || rt.MajorId == null)) // Riêng user
                    )
                    .ToListAsync();

                if (!templates.Any())
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"No templates found for UserId {userId} and MajorId {majorId}.",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // ✅ Bước 5: Map response + lấy các assignment sử dụng template
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
                    $"Found {response.Count()} template(s) for UserId {userId} and MajorId {majorId}.",
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
                // 1️⃣ Lấy user + các thông tin course + major
                var user = await _context.Users
                    .Include(u => u.CourseInstructors)
                        .ThenInclude(ci => ci.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                                .ThenInclude(c => c.Curriculum)
                                    .ThenInclude(cur => cur.Major)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} not found.",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // 2️⃣ Xác định tất cả các major mà user liên quan
                var userMajorIds = new List<int>();
                if (user.MajorId.HasValue)
                    userMajorIds.Add(user.MajorId.Value);

                var courseMajors = user.CourseInstructors
                    .Where(ci => ci.CourseInstance?.Course?.Curriculum?.Major != null)
                    .Select(ci => ci.CourseInstance.Course.Curriculum.Major.MajorId)
                    .Distinct()
                    .ToList();

                userMajorIds.AddRange(courseMajors);
                userMajorIds = userMajorIds.Distinct().ToList();

                if (!userMajorIds.Any())
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"UserId {userId} has no associated major.",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                // 3️⃣ Lấy các RubricTemplate public và major hợp lệ
                var rubricTemplates = await _context.RubricTemplates
                    .Include(rt => rt.CreatedByUser)
                    .Include(rt => rt.Rubrics)
                    .Include(rt => rt.CriteriaTemplates)
                    .Include(rt => rt.Major)
                    .Where(rt => rt.IsPublic && rt.MajorId.HasValue && userMajorIds.Contains(rt.MajorId.Value))
                    .ToListAsync();

                if (!rubricTemplates.Any())
                {
                    return new BaseResponse<IEnumerable<RubricTemplateResponse>>(
                        $"No public rubric templates found for UserId {userId} with their major(s).",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // 4️⃣ Map sang response
                var response = _mapper.Map<IEnumerable<RubricTemplateResponse>>(rubricTemplates);

                // 5️⃣ Gán danh sách assignments đang sử dụng template
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
                    $"Found {response.Count()} public template(s) accessible to UserId {userId}.",
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
                    .Include(rt => rt.Major)
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