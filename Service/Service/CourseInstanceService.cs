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
                // Validate if course exists
                var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == request.CourseId);
                if (!courseExists)
                {
                    return new BaseResponse<CourseInstanceResponse>("Course not found", StatusCodeEnum.NotFound_404, null);
                }

                // Validate if semester exists
                var semesterExists = await _context.Semesters.AnyAsync(s => s.SemesterId == request.SemesterId);
                if (!semesterExists)
                {
                    return new BaseResponse<CourseInstanceResponse>("Semester not found", StatusCodeEnum.NotFound_404, null);
                }

                // Validate if campus exists
                var campusExists = await _context.Campuses.AnyAsync(c => c.CampusId == request.CampusId);
                if (!campusExists)
                {
                    return new BaseResponse<CourseInstanceResponse>("Campus not found", StatusCodeEnum.NotFound_404, null);
                }

                // Check for duplicate section code in the same course and semester
                var duplicateSection = await _context.CourseInstances
                    .AnyAsync(ci => ci.CourseId == request.CourseId &&
                                   ci.SemesterId == request.SemesterId &&
                                   ci.SectionCode == request.SectionCode);

                if (duplicateSection)
                {
                    return new BaseResponse<CourseInstanceResponse>("Section code already exists for this course and semester", StatusCodeEnum.BadRequest_400, null);
                }

                var courseInstance = _mapper.Map<CourseInstance>(request);

                // Generate enrollment password if not provided
                if (string.IsNullOrEmpty(courseInstance.EnrollmentPassword))
                {
                    courseInstance.EnrollmentPassword = GenerateEnrollKey();
                }

                var createdCourseInstance = await _courseInstanceRepository.AddAsync(courseInstance);

                // Reload with related data for response
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
                    .Include(ci => ci.Course)
                    .Include(ci => ci.Semester)
                    .Include(ci => ci.Campus)
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == request.CourseInstanceId);

                if (existingCourseInstance == null)
                {
                    return new BaseResponse<CourseInstanceResponse>("Course instance not found", StatusCodeEnum.NotFound_404, null);
                }

                // Validate course if provided
                if (request.CourseId > 0 && request.CourseId != existingCourseInstance.CourseId)
                {
                    var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == request.CourseId);
                    if (!courseExists)
                    {
                        return new BaseResponse<CourseInstanceResponse>("Course not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                // Validate semester if provided
                if (request.SemesterId > 0 && request.SemesterId != existingCourseInstance.SemesterId)
                {
                    var semesterExists = await _context.Semesters.AnyAsync(s => s.SemesterId == request.SemesterId);
                    if (!semesterExists)
                    {
                        return new BaseResponse<CourseInstanceResponse>("Semester not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                // Validate campus if provided
                if (request.CampusId > 0 && request.CampusId != existingCourseInstance.CampusId)
                {
                    var campusExists = await _context.Campuses.AnyAsync(c => c.CampusId == request.CampusId);
                    if (!campusExists)
                    {
                        return new BaseResponse<CourseInstanceResponse>("Campus not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                // Check for duplicate section code if provided
                if (!string.IsNullOrEmpty(request.SectionCode) && request.SectionCode != existingCourseInstance.SectionCode)
                {
                    var duplicateSection = await _context.CourseInstances
                        .AnyAsync(ci => ci.CourseId == (request.CourseId > 0 ? request.CourseId : existingCourseInstance.CourseId) &&
                                       ci.SemesterId == (request.SemesterId > 0 ? request.SemesterId : existingCourseInstance.SemesterId) &&
                                       ci.SectionCode == request.SectionCode &&
                                       ci.CourseInstanceId != request.CourseInstanceId);

                    if (duplicateSection)
                    {
                        return new BaseResponse<CourseInstanceResponse>("Section code already exists for this course and semester", StatusCodeEnum.BadRequest_400, null);
                    }
                }

                // Update only provided fields
                if (request.CourseId > 0) existingCourseInstance.CourseId = request.CourseId;
                if (request.SemesterId > 0) existingCourseInstance.SemesterId = request.SemesterId;
                if (request.CampusId > 0) existingCourseInstance.CampusId = request.CampusId;
                if (!string.IsNullOrEmpty(request.SectionCode)) existingCourseInstance.SectionCode = request.SectionCode;
                if (!string.IsNullOrEmpty(request.EnrollmentPassword)) existingCourseInstance.EnrollmentPassword = request.EnrollmentPassword;

                existingCourseInstance.RequiresApproval = request.RequiresApproval;

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
                    .Include(ci => ci.CourseInstructors)
                    .Include(ci => ci.CourseStudents)
                    .Include(ci => ci.Assignments)
                    .FirstOrDefaultAsync(ci => ci.CourseInstanceId == id);

                if (courseInstance == null)
                {
                    return new BaseResponse<bool>("Course instance not found", StatusCodeEnum.NotFound_404, false);
                }

                // Check if there are any related records that would prevent deletion
                if (courseInstance.CourseInstructors.Any() || courseInstance.CourseStudents.Any() || courseInstance.Assignments.Any())
                {
                    return new BaseResponse<bool>("Cannot delete course instance that has instructors, students, or assignments", StatusCodeEnum.BadRequest_400, false);
                }

                await _courseInstanceRepository.DeleteAsync(courseInstance);
                return new BaseResponse<bool>("Course instance deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting course instance: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
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
                return "Scheduled";
            if (now <= assignment.Deadline)
                return "Active";
            if (assignment.FinalDeadline.HasValue && now <= assignment.FinalDeadline.Value)
                return "LateSubmission";
            return "Closed";
        }
    }
}