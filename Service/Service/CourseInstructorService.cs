using BussinessObject.Models;
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
        private readonly IUserRepository _userRepository;

        public CourseInstructorService(
            ICourseInstructorRepository courseInstructorRepository,
            ICourseInstanceRepository courseInstanceRepository,
            IUserRepository userRepository)
        {
            _courseInstructorRepository = courseInstructorRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _userRepository = userRepository;
        }

        public async Task<BaseResponse<CourseInstructorResponse>> CreateCourseInstructorAsync(CreateCourseInstructorRequest request)
        {
            try
            {
                // Validate course instance exists
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<CourseInstructorResponse>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Validate user exists
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new BaseResponse<CourseInstructorResponse>(
                        "User not found",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Check if assignment already exists
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

                var response = MapToResponse(courseInstructor, courseInstance, user);
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

        public async Task<BaseResponse<bool>> DeleteCourseInstructorAsync(int courseInstructorId)
        {
            try
            {
                var courseInstructor = await _courseInstructorRepository.GetByIdAsync(courseInstructorId);
                if (courseInstructor == null)
                {
                    return new BaseResponse<bool>(
                        "Course instructor not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

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

                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseInstructor.CourseInstanceId);
                var user = await _userRepository.GetByIdAsync(courseInstructor.UserId);
                var response = MapToResponse(courseInstructor, courseInstance, user);
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

        public async Task<BaseResponse<List<CourseInstructorResponse>>> GetCourseInstructorsByCourseInstanceAsync(int courseInstanceId)
        {
            try
            {
                var courseInstructors = await _courseInstructorRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                var responses = new List<CourseInstructorResponse>();

                foreach (var ci in courseInstructors)
                {
                    var courseInstance = await _courseInstanceRepository.GetByIdAsync(ci.CourseInstanceId);
                    var user = await _userRepository.GetByIdAsync(ci.UserId);
                    responses.Add(MapToResponse(ci, courseInstance, user));
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

        public async Task<BaseResponse<List<CourseInstructorResponse>>> GetCourseInstructorsByInstructorAsync(int instructorId)
        {
            try
            {
                var courseInstructors = await _courseInstructorRepository.GetByUserIdAsync(instructorId);
                var responses = new List<CourseInstructorResponse>();

                foreach (var ci in courseInstructors)
                {
                    var courseInstance = await _courseInstanceRepository.GetByIdAsync(ci.CourseInstanceId);
                    var user = await _userRepository.GetByIdAsync(ci.UserId);
                    responses.Add(MapToResponse(ci, courseInstance, user));
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

        public async Task<BaseResponse<bool>> UpdateMainInstructorAsync(int courseInstanceId, int mainInstructorId)
        {
            // Tạm thời chưa implement vì model chưa có field IsMainInstructor
            return new BaseResponse<bool>(
                "Function not implemented yet",
                StatusCodeEnum.NotImplemented_501,
                false);
        }

        public async Task<BaseResponse<List<CourseInstructorResponse>>> BulkAssignInstructorsAsync(BulkAssignInstructorsRequest request)
        {
            try
            {
                // Validate course instance exists
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
                    // Skip if already assigned
                    if (existingInstructors.Contains(instructorId))
                        continue;

                    // Validate user exists and is instructor
                    var user = await _userRepository.GetByIdAsync(instructorId);
                    if (user == null)
                        continue;

                    var courseInstructor = new CourseInstructor
                    {
                        CourseInstanceId = request.CourseInstanceId,
                        UserId = instructorId
                    };

                    await _courseInstructorRepository.AddAsync(courseInstructor);
                    responses.Add(MapToResponse(courseInstructor, courseInstance, user));
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

        private CourseInstructorResponse MapToResponse(CourseInstructor courseInstructor, CourseInstance courseInstance, User user)
        {
            return new CourseInstructorResponse
            {
                Id = courseInstructor.Id,
                CourseInstanceId = courseInstructor.CourseInstanceId,
                CourseInstanceName = courseInstance?.SectionCode ?? string.Empty,
                CourseCode = courseInstance?.Course?.CourseCode ?? string.Empty,
                UserId = courseInstructor.UserId,
                InstructorName = user?.FirstName ?? string.Empty,
                InstructorEmail = user?.Email ?? string.Empty,
                IsMainInstructor = false, // Mặc định false vì model chưa có field này
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}