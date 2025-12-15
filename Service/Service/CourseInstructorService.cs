using BussinessObject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseInstructor;
using Service.RequestAndResponse.Response.CourseInstructor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CourseInstructorService : ICourseInstructorService
    {
        private readonly ICourseInstructorRepository _courseInstructorRepository;
        private readonly ICourseInstanceRepository _courseInstanceRepository;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly ICourseStudentRepository _courseStudentRepository;
        private readonly UserManager<User> _userManager;

        public CourseInstructorService(
            ICourseInstructorRepository courseInstructorRepository,
            ICourseInstanceRepository courseInstanceRepository,
            INotificationService notificationService,
            IUserRepository userRepository,
            ICourseStudentRepository courseStudentRepository,
            UserManager<User> userManager)
        {
            _courseInstructorRepository = courseInstructorRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _courseStudentRepository = courseStudentRepository;
            _userManager = userManager;
        }

        // 🟢 Tạo mới instructor trong lớp
        public async Task<BaseResponse<CourseInstructorResponse>> CreateCourseInstructorAsync(CreateCourseInstructorRequest request)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<CourseInstructorResponse>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new BaseResponse<CourseInstructorResponse>(
                        "User not found",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }
                if (!await _userManager.IsInRoleAsync(user, "Instructor"))
                {
                    return new BaseResponse<CourseInstructorResponse>(
                        "User does not have the Instructor role",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var existing = (await _courseInstructorRepository.GetByCourseInstanceIdAsync(request.CourseInstanceId))
                    .FirstOrDefault(ci => ci.UserId == request.UserId);

                if (existing != null)
                {
                    return new BaseResponse<CourseInstructorResponse>(
                        "Instructor is already assigned to this course instance",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                var courseInstructor = new CourseInstructor
                {
                    CourseInstanceId = request.CourseInstanceId,
                    UserId = request.UserId
                };

                await _courseInstructorRepository.AddAsync(courseInstructor);
                await _notificationService.SendInstructorAssignedNotificationAsync(
                    user.Id,
                    courseInstance.CourseInstanceId
);


                var response = await MapToResponseAsync(courseInstructor);
                return new BaseResponse<CourseInstructorResponse>(
                    "Course instructor assigned successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseInstructorResponse>(
                    $"Error creating course instructor: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        // 🔴 Xóa instructor khỏi lớp
        public async Task<BaseResponse<bool>> DeleteCourseInstructorAsync(int courseInstructorId, int courseInstanceId, int instructorId)
        {
            try
            {
                // 1. Kiểm tra lớp có tồn tại không
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<bool>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // 2. Kiểm tra instructor có tồn tại không
                var instructor = await _userRepository.GetByIdAsync(instructorId);
                if (instructor == null)
                {
                    return new BaseResponse<bool>(
                        "Instructor not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // 3. Tìm courseInstructor theo id
                var courseInstructor = await _courseInstructorRepository.GetByIdAsync(courseInstructorId);
                if (courseInstructor == null)
                {
                    return new BaseResponse<bool>(
                        "Course instructor not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // 4. Kiểm tra xem có khớp cả 3 field không
                if (courseInstructor.UserId != instructorId || courseInstructor.CourseInstanceId != courseInstanceId)
                {
                    return new BaseResponse<bool>(
                        "Mismatch detected: instructor does not belong to this course instance",
                        StatusCodeEnum.BadRequest_400,
                        false);
                }

                // 5. Kiểm tra khóa học có đang diễn ra không
                var now = DateTime.UtcNow.AddHours(7);
                if (courseInstance.StartDate <= now && now <= courseInstance.EndDate)
                {
                    return new BaseResponse<bool>(
                        "Cannot remove instructor from an ongoing course",
                        StatusCodeEnum.BadRequest_400,
                        false);
                }

                // 6. Tiến hành xóa
                await _courseInstructorRepository.DeleteAsync(courseInstructor);
                return new BaseResponse<bool>(
                    "Course instructor deleted successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error deleting course instructor: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        // 🔍 Lấy instructor theo ID
        public async Task<BaseResponse<CourseInstructorResponse>> GetCourseInstructorByIdAsync(int id)
        {
            try
            {
                var courseInstructor = await _courseInstructorRepository.GetByIdAsync(id);
                if (courseInstructor == null)
                {
                    return new BaseResponse<CourseInstructorResponse>(
                        "Course instructor not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToResponseAsync(courseInstructor);
                return new BaseResponse<CourseInstructorResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseInstructorResponse>(
                    $"Error retrieving course instructor: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        // 📚 Lấy danh sách instructors của 1 lớp
        public async Task<BaseResponse<List<CourseInstructorResponse>>> GetCourseInstructorsByCourseInstanceAsync(int courseInstanceId)
        {
            try
            {
                var courseInstructors = await _courseInstructorRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                var responses = new List<CourseInstructorResponse>();

                foreach (var ci in courseInstructors)
                {
                    responses.Add(await MapToResponseAsync(ci));
                }

                return new BaseResponse<List<CourseInstructorResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseInstructorResponse>>(
                    $"Error retrieving course instructors: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        // 👨‍🏫 Lấy danh sách lớp mà 1 instructor dạy
        public async Task<BaseResponse<List<CourseInstructorResponse>>> GetCourseInstructorsByInstructorAsync(int instructorId)
        {
            try
            {
                var courseInstructors = await _courseInstructorRepository.GetByUserIdAsync(instructorId);
                var responses = new List<CourseInstructorResponse>();

                foreach (var ci in courseInstructors)
                {
                    responses.Add(await MapToResponseAsync(ci));
                }

                return new BaseResponse<List<CourseInstructorResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseInstructorResponse>>(
                    $"Error retrieving course instructors: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        // 🟡 Tạm thời chưa dùng
        public async Task<BaseResponse<bool>> UpdateMainInstructorAsync(int courseInstanceId, int mainInstructorId)
        {
            return new BaseResponse<bool>(
                "Function not implemented yet",
                StatusCodeEnum.NotImplemented_501,
                false);
        }

        // 🔁 Gán nhiều instructors vào 1 lớp
        public async Task<BaseResponse<List<CourseInstructorResponse>>> BulkAssignInstructorsAsync(BulkAssignInstructorsRequest request)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<List<CourseInstructorResponse>>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var responses = new List<CourseInstructorResponse>();
                var existingInstructors = (await _courseInstructorRepository.GetByCourseInstanceIdAsync(request.CourseInstanceId))
                    .Select(ci => ci.UserId)
                    .ToHashSet();

                foreach (var instructorId in request.InstructorIds)
                {
                    if (existingInstructors.Contains(instructorId))
                        continue;

                    var user = await _userRepository.GetByIdAsync(instructorId);
                    if (user == null)
                        continue;

                    var courseInstructor = new CourseInstructor
                    {
                        CourseInstanceId = request.CourseInstanceId,
                        UserId = instructorId
                    };

                    await _courseInstructorRepository.AddAsync(courseInstructor);
                    responses.Add(await MapToResponseAsync(courseInstructor));
                }

                return new BaseResponse<List<CourseInstructorResponse>>(
                    $"Successfully assigned {responses.Count} instructors to course instance",
                    StatusCodeEnum.Created_201,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseInstructorResponse>>(
                    $"Error bulk assigning instructors: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        // 🔹 Helper: Map dữ liệu sang response (có đếm SV & trạng thái lớp)
        private async Task<CourseInstructorResponse> MapToResponseAsync(CourseInstructor courseInstructor)
        {
           // var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseInstructor.CourseInstanceId);
            var courseInstance = await _courseInstanceRepository.GetByIdWithRelationsAsync(courseInstructor.CourseInstanceId);
            var user = await _userRepository.GetByIdAsync(courseInstructor.UserId);


            // 🔸 Đếm sinh viên trong lớp
            var studentCount = await _courseStudentRepository.CountByCourseInstanceIdAsync(courseInstructor.CourseInstanceId);

            string courseStatus; 
            // 🔸 Xác định trạng thái lớp học
            if (courseInstance.StartDate > DateTime.UtcNow.AddHours(7))
            {
                courseStatus = "Upcoming"; // Chưa bắt đầu
            }
            else if (courseInstance.EndDate < DateTime.UtcNow.AddHours(7))
            {
                courseStatus = "Completed"; // Đã kết thúc
            }
            else
            {
                courseStatus = "Ongoing"; // Đang diễn ra
            }

            var courseName = courseInstance?.Course?.CourseName ?? string.Empty;
            var semesterName = courseInstance?.Semester?.Name ?? string.Empty;

            return new CourseInstructorResponse
            {
                Id = courseInstructor.Id,
                CourseInstanceId = courseInstructor.CourseInstanceId,
                CourseInstanceName = courseInstance?.SectionCode ?? string.Empty,
                CourseCode = courseInstance?.Course?.CourseCode ?? string.Empty,
                CourseName = courseName,
                SemesterName = semesterName,
                StartDate = courseInstance.StartDate,
                EndDate = courseInstance.EndDate,
                UserId = courseInstructor.UserId,
                InstructorName = user?.FirstName ?? string.Empty,
                InstructorEmail = user?.Email ?? string.Empty,
                IsMainInstructor = false,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                StudentCount = studentCount,
                CourseInstanceStatus = courseStatus
            };
        }
    }
}
