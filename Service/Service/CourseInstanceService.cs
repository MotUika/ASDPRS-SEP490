using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseInstance;
using Service.RequestAndResponse.Response.CourseInstance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CourseInstanceService : ICourseInstanceService
    {
        private readonly ICourseInstanceRepository _courseInstanceRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public CourseInstanceService(ICourseInstanceRepository courseInstanceRepository, ASDPRSContext context, IMapper mapper)
        {
            _courseInstanceRepository = courseInstanceRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<CourseInstanceResponse>> GetCourseInstanceByIdAsync(int id)
        {
            try
            {
                var courseInstance = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .Include(ci => ci.Semester)
                    .Include(ci => ci.Campus)
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == id);

                if (courseInstance == null)
                {
                    return new BaseResponse<CourseInstanceResponse>("Course instance not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<CourseInstanceResponse>(courseInstance);
                return new BaseResponse<CourseInstanceResponse>("Course instance retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseInstanceResponse>($"Error retrieving course instance: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetAllCourseInstancesAsync()
        {
            try
            {
                var courseInstances = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .Include(ci => ci.Semester)
                    .Include(ci => ci.Campus)
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseInstanceResponse>>(courseInstances);
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>("Course instances retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>($"Error retrieving course instances: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CourseInstanceResponse>> CreateCourseInstanceAsync(CreateCourseInstanceRequest request)
        {
            try
            {
                var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == request.CourseId);
                if (!courseExists) return new BaseResponse<CourseInstanceResponse>("Course not found", StatusCodeEnum.NotFound_404, null);

                var semester = await _context.Semesters.FirstOrDefaultAsync(s => s.SemesterId == request.SemesterId);
                if (semester == null) return new BaseResponse<CourseInstanceResponse>("Semester not found", StatusCodeEnum.NotFound_404, null);

                var campusExists = await _context.Campuses.AnyAsync(c => c.CampusId == request.CampusId);
                if (!campusExists) return new BaseResponse<CourseInstanceResponse>("Campus not found", StatusCodeEnum.NotFound_404, null);

                var now = DateTime.UtcNow;
                if (request.StartDate < semester.StartDate) return new BaseResponse<CourseInstanceResponse>("Start date cannot be before semester start date", StatusCodeEnum.BadRequest_400, null);
                if (request.StartDate <= now) return new BaseResponse<CourseInstanceResponse>("Start date cannot be in the past or present", StatusCodeEnum.BadRequest_400, null);
                if (request.EndDate <= request.StartDate) return new BaseResponse<CourseInstanceResponse>("End date must be after start date", StatusCodeEnum.BadRequest_400, null);
                if (request.EndDate > semester.EndDate) return new BaseResponse<CourseInstanceResponse>("End date cannot be after semester end date", StatusCodeEnum.BadRequest_400, null);


                var duplicateSection = await _context.CourseInstances
                    .AnyAsync(ci => ci.CourseId == request.CourseId &&
                                   ci.SemesterId == request.SemesterId &&
                                   ci.SectionCode == request.SectionCode &&
                                   ci.CampusId == request.CampusId);

                if (duplicateSection)
                {
                    return new BaseResponse<CourseInstanceResponse>("This class section already exists for this course in this semester at this campus", StatusCodeEnum.BadRequest_400, null);
                }

                var courseInstance = _mapper.Map<CourseInstance>(request);
                courseInstance.StartDate = request.StartDate;
                courseInstance.EndDate = request.EndDate;

                if (string.IsNullOrEmpty(courseInstance.EnrollmentPassword))
                {
                    courseInstance.EnrollmentPassword = GenerateEnrollKey();
                }

                var createdCourseInstance = await _courseInstanceRepository.AddAsync(courseInstance);

                var courseInstanceWithDetails = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .Include(ci => ci.Semester)
                    .Include(ci => ci.Campus)
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == createdCourseInstance.CourseInstanceId);

                var response = _mapper.Map<CourseInstanceResponse>(courseInstanceWithDetails);

                string message = string.IsNullOrEmpty(request.EnrollmentPassword)
                    ? $"Course instance created successfully with enrollment key: {courseInstance.EnrollmentPassword}"
                    : "Course instance created successfully";

                return new BaseResponse<CourseInstanceResponse>(message, StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseInstanceResponse>($"Error creating course instance: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<string>> UpdateEnrollKeyAsync(int courseInstanceId, string newKey, int userId)
        {
            try
            {
                var courseInstance = await _context.CourseInstances
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == courseInstanceId);

                if (courseInstance == null)
                {
                    return new BaseResponse<string>("Không tìm thấy lớp học", StatusCodeEnum.NotFound_404, null);
                }

                // Kiểm tra instructor có thuộc lớp không (CourseInstructor)
                var isInstructorInCourse = await _context.CourseInstructors
                    .AnyAsync(ci => ci.CourseInstanceId == courseInstanceId && ci.UserId == userId);

                if (!isInstructorInCourse)
                {
                    return new BaseResponse<string>("Bạn không có quyền đổi mã lớp này", StatusCodeEnum.Forbidden_403, null);
                }

                // Cập nhật mã mới
                courseInstance.EnrollmentPassword = newKey;
                await _courseInstanceRepository.UpdateAsync(courseInstance);

                return new BaseResponse<string>("Cập nhật mã lớp thành công", StatusCodeEnum.OK_200, newKey);
            }
            catch (Exception ex)
            {
                return new BaseResponse<string>($"Lỗi khi cập nhật mã lớp: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }


        private string GenerateEnrollKey(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var key = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return "ENROLL-" + key;
        }

        public async Task<BaseResponse<CourseInstanceResponse>> UpdateCourseInstanceAsync(UpdateCourseInstanceRequest request)
        {
            try
            {
                var existingCourseInstance = await _context.CourseInstances
                    .Include(ci => ci.CourseStudents)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == request.CourseInstanceId);

                if (existingCourseInstance == null) return new BaseResponse<CourseInstanceResponse>("Course instance not found", StatusCodeEnum.NotFound_404, null);

                if (existingCourseInstance.CourseStudents.Any())
                {
                    if ((request.CourseId > 0 && request.CourseId != existingCourseInstance.CourseId) ||
                        (request.SemesterId > 0 && request.SemesterId != existingCourseInstance.SemesterId))
                    {
                        return new BaseResponse<CourseInstanceResponse>("Cannot change Course or Semester for a class that already has enrolled students", StatusCodeEnum.Conflict_409, null);
                    }
                }
                var semesterId = request.SemesterId > 0 ? request.SemesterId : existingCourseInstance.SemesterId;
                var semester = await _context.Semesters.FirstOrDefaultAsync(s => s.SemesterId == semesterId);


                if (!string.IsNullOrEmpty(request.SectionCode) && request.SectionCode != existingCourseInstance.SectionCode)
                {
                    var targetCourseId = request.CourseId > 0 ? request.CourseId : existingCourseInstance.CourseId;
                    var targetSemesterId = request.SemesterId > 0 ? request.SemesterId : existingCourseInstance.SemesterId;
                    var targetCampusId = request.CampusId > 0 ? request.CampusId : existingCourseInstance.CampusId;

                    var duplicateSection = await _context.CourseInstances
                        .AnyAsync(ci => ci.CourseId == targetCourseId &&
                                       ci.SemesterId == targetSemesterId &&
                                       ci.SectionCode == request.SectionCode &&
                                       ci.CampusId == targetCampusId && // Thêm check CampusId
                                       ci.CourseInstanceId != request.CourseInstanceId); // Loại trừ chính nó

                    if (duplicateSection)
                    {
                        return new BaseResponse<CourseInstanceResponse>("Section code already exists for this course and semester in this campus", StatusCodeEnum.BadRequest_400, null);
                    }
                }

                if (request.CourseId > 0) existingCourseInstance.CourseId = request.CourseId;
                if (request.SemesterId > 0) existingCourseInstance.SemesterId = request.SemesterId;
                if (request.CampusId > 0) existingCourseInstance.CampusId = request.CampusId;
                if (!string.IsNullOrEmpty(request.SectionCode)) existingCourseInstance.SectionCode = request.SectionCode;

                var updatedCourseInstance = await _courseInstanceRepository.UpdateAsync(existingCourseInstance);
                var response = _mapper.Map<CourseInstanceResponse>(updatedCourseInstance);

                return new BaseResponse<CourseInstanceResponse>("Course instance updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseInstanceResponse>($"Error updating course instance: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCourseInstanceAsync(int id)
        {
            try
            {
                var courseInstance = await _context.CourseInstances
                    // Include các bảng liên quan để check constraint
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == id);

                if (courseInstance == null)
                    return new BaseResponse<bool>("Course instance not found", StatusCodeEnum.NotFound_404, false);

                var now = DateTime.UtcNow;

                if (courseInstance.StartDate <= now && now <= courseInstance.EndDate)
                {
                    return new BaseResponse<bool>("Cannot delete a course that is currently in progress.", StatusCodeEnum.Forbidden_403, false);
                }

                if (courseInstance.CourseInstructors.Any() || courseInstance.CourseStudents.Any() || courseInstance.Assignments.Any())
                {
                    return new BaseResponse<bool>("Cannot delete course instance that has linked data. Consider Deactivating it instead.", StatusCodeEnum.BadRequest_400, false);
                }

                await _courseInstanceRepository.DeleteAsync(courseInstance);
                return new BaseResponse<bool>("Course instance deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetCourseInstancesByCourseIdAsync(int courseId)
        {
            try
            {
                var courseInstances = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .Include(ci => ci.Semester)
                    .Include(ci => ci.Campus)
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .Where(ci => ci.CourseId == courseId)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseInstanceResponse>>(courseInstances);
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>("Course instances retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>($"Error retrieving course instances: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetCourseInstancesBySemesterIdAsync(int semesterId)
        {
            try
            {
                var courseInstances = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .Include(ci => ci.Semester)
                    .Include(ci => ci.Campus)
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .Where(ci => ci.SemesterId == semesterId)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseInstanceResponse>>(courseInstances);
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>("Course instances retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>($"Error retrieving course instances: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetCourseInstancesByCampusIdAsync(int campusId)
        {
            try
            {
                var courseInstances = await _context.CourseInstances
                    .Include(ci => ci.Course)
                    .Include(ci => ci.Semester)
                    .Include(ci => ci.Campus)
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .Where(ci => ci.CampusId == campusId)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseInstanceResponse>>(courseInstances);
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>("Course instances retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>($"Error retrieving course instances: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task UpdateAssignmentStatusBasedOnTimeline()
        {
            var now = DateTime.UtcNow;
            var assignments = await _context.Assignments.ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.Status = CalculateAssignmentStatus(assignment, now);
            }

            await _context.SaveChangesAsync();
        }
        private string CalculateAssignmentStatus(Assignment assignment, DateTime now)
        {
            if (assignment.StartDate.HasValue && now < assignment.StartDate.Value)
                return "Upcoming";
            if (now <= assignment.Deadline)
                return "Active";
            if (assignment.FinalDeadline.HasValue && now <= assignment.FinalDeadline.Value)
                return "LateSubmission";
            return "Closed";
        }

        public async Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetClassesByUserIdAsync(int userId, int? courseId)
        {
            try
            {
                // 🟩 Bắt đầu query từ CourseInstances
                var query = _context.CourseInstances
                    .Include(ci => ci.Course)
                    .Include(ci => ci.Semester)
                    .Include(ci => ci.Campus)
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .Where(ci => ci.CourseInstructors.Any(ciu => ciu.UserId == userId));

                // 🟩 Nếu có lọc theo CourseId thì thêm điều kiện
                if (courseId.HasValue && courseId > 0)
                    query = query.Where(ci => ci.CourseId == courseId);

                // 🟩 Lấy danh sách
                var courseInstances = await query.ToListAsync();

                if (!courseInstances.Any())
                {
                    return new BaseResponse<IEnumerable<CourseInstanceResponse>>("No classes found for this user", StatusCodeEnum.NoContent_204, null);
                }

                // 🟩 Map sang Response
                var response = _mapper.Map<IEnumerable<CourseInstanceResponse>>(courseInstances);

                return new BaseResponse<IEnumerable<CourseInstanceResponse>>("Classes retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>($"Error retrieving classes: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> ToggleCourseStatusAsync(int id)
        {
            try
            {
                var courseInstance = await _context.CourseInstances.FindAsync(id);
                if (courseInstance == null)
                {
                    return new BaseResponse<bool>("Course instance not found", StatusCodeEnum.NotFound_404, false);
                }

                var now = DateTime.UtcNow;

                if (courseInstance.StartDate <= now && now <= courseInstance.EndDate)
                {
                    return new BaseResponse<bool>("Cannot change status while the course is in progress (Ongoing).", StatusCodeEnum.BadRequest_400, false);
                }

                courseInstance.IsActive = !courseInstance.IsActive;

                await _courseInstanceRepository.UpdateAsync(courseInstance);

                var statusMsg = courseInstance.IsActive ? "Activated" : "Deactivated";
                return new BaseResponse<bool>($"Course instance {statusMsg} successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error changing status: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }
    }
}