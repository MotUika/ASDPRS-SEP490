using BussinessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Response.CourseStudent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CourseStudentService : ICourseStudentService
    {
        private readonly ICourseStudentRepository _courseStudentRepository;
        private readonly ICourseInstanceRepository _courseInstanceRepository;
        private readonly IUserRepository _userRepository;

        public CourseStudentService(
            ICourseStudentRepository courseStudentRepository,
            ICourseInstanceRepository courseInstanceRepository,
            IUserRepository userRepository)
        {
            _courseStudentRepository = courseStudentRepository;
            _courseInstanceRepository = courseInstanceRepository;
            _userRepository = userRepository;
        }

        public async Task<BaseResponse<CourseStudentResponse>> CreateCourseStudentAsync(CreateCourseStudentRequest request)
        {
            try
            {
                // Validate course instance exists
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Validate user exists
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "User not found",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Check if student is already enrolled
                var existing = (await _courseStudentRepository.GetByCourseInstanceIdAsync(request.CourseInstanceId))
                    .FirstOrDefault(cs => cs.UserId == request.UserId);

                if (existing != null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Student is already enrolled in this course instance",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                var courseStudent = new CourseStudent
                {
                    CourseInstanceId = request.CourseInstanceId,
                    UserId = request.UserId,
                    EnrolledAt = DateTime.UtcNow,
                    Status = request.Status,
                    FinalGrade = request.FinalGrade,
                    IsPassed = request.IsPassed,
                    StatusChangedAt = DateTime.UtcNow,
                    ChangedByUserId = request.ChangedByUserId
                };

                await _courseStudentRepository.AddAsync(courseStudent);

                User changedByUser = null;
                if (request.ChangedByUserId.HasValue)
                {
                    changedByUser = await _userRepository.GetByIdAsync(request.ChangedByUserId.Value);
                }

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>(
                    "Course student enrolled successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>(
                    $"Error creating course student: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCourseStudentAsync(int courseStudentId)
        {
            try
            {
                var courseStudent = await _courseStudentRepository.GetByIdAsync(courseStudentId);
                if (courseStudent == null)
                {
                    return new BaseResponse<bool>(
                        "Course student not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                await _courseStudentRepository.DeleteAsync(courseStudent);
                return new BaseResponse<bool>(
                    "Course student deleted successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error deleting course student: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<CourseStudentResponse>> GetCourseStudentByIdAsync(int id)
        {
            try
            {
                var courseStudent = await _courseStudentRepository.GetByIdAsync(id);
                if (courseStudent == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Course student not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseStudent.CourseInstanceId);
                var user = await _userRepository.GetByIdAsync(courseStudent.UserId);
                User changedByUser = null;
                if (courseStudent.ChangedByUserId.HasValue)
                {
                    changedByUser = await _userRepository.GetByIdAsync(courseStudent.ChangedByUserId.Value);
                }

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>(
                    $"Error retrieving course student: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> GetCourseStudentsByCourseInstanceAsync(int courseInstanceId)
        {
            try
            {
                var courseStudents = await _courseStudentRepository.GetByCourseInstanceIdAsync(courseInstanceId);
                var responses = new List<CourseStudentResponse>();

                foreach (var cs in courseStudents)
                {
                    var courseInstance = await _courseInstanceRepository.GetByIdAsync(cs.CourseInstanceId);
                    var user = await _userRepository.GetByIdAsync(cs.UserId);
                    User changedByUser = null;
                    if (cs.ChangedByUserId.HasValue)
                    {
                        changedByUser = await _userRepository.GetByIdAsync(cs.ChangedByUserId.Value);
                    }
                    responses.Add(MapToResponse(cs, courseInstance, user, changedByUser));
                }

                return new BaseResponse<List<CourseStudentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>(
                    $"Error retrieving course students: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> GetCourseStudentsByStudentAsync(int studentId)
        {
            try
            {
                var courseStudents = await _courseStudentRepository.GetByUserIdAsync(studentId);
                var responses = new List<CourseStudentResponse>();

                foreach (var cs in courseStudents)
                {
                    var courseInstance = await _courseInstanceRepository.GetByIdAsync(cs.CourseInstanceId);
                    var user = await _userRepository.GetByIdAsync(cs.UserId);
                    User changedByUser = null;
                    if (cs.ChangedByUserId.HasValue)
                    {
                        changedByUser = await _userRepository.GetByIdAsync(cs.ChangedByUserId.Value);
                    }
                    responses.Add(MapToResponse(cs, courseInstance, user, changedByUser));
                }

                return new BaseResponse<List<CourseStudentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>(
                    $"Error retrieving course students: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<CourseStudentResponse>> UpdateCourseStudentStatusAsync(int courseStudentId, string status, int changedByUserId)
        {
            try
            {
                var courseStudent = await _courseStudentRepository.GetByIdAsync(courseStudentId);
                if (courseStudent == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Course student not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                courseStudent.Status = status;
                courseStudent.StatusChangedAt = DateTime.UtcNow;
                courseStudent.ChangedByUserId = changedByUserId;

                await _courseStudentRepository.UpdateAsync(courseStudent);

                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseStudent.CourseInstanceId);
                var user = await _userRepository.GetByIdAsync(courseStudent.UserId);
                User changedByUser = await _userRepository.GetByIdAsync(changedByUserId);

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>(
                    "Course student status updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>(
                    $"Error updating course student status: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<CourseStudentResponse>> UpdateCourseStudentGradeAsync(int courseStudentId, decimal? finalGrade, bool isPassed, int changedByUserId)
        {
            try
            {
                var courseStudent = await _courseStudentRepository.GetByIdAsync(courseStudentId);
                if (courseStudent == null)
                {
                    return new BaseResponse<CourseStudentResponse>(
                        "Course student not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                courseStudent.FinalGrade = finalGrade;
                courseStudent.IsPassed = isPassed;
                courseStudent.StatusChangedAt = DateTime.UtcNow;
                courseStudent.ChangedByUserId = changedByUserId;

                await _courseStudentRepository.UpdateAsync(courseStudent);

                var courseInstance = await _courseInstanceRepository.GetByIdAsync(courseStudent.CourseInstanceId);
                var user = await _userRepository.GetByIdAsync(courseStudent.UserId);
                User changedByUser = await _userRepository.GetByIdAsync(changedByUserId);

                var response = MapToResponse(courseStudent, courseInstance, user, changedByUser);
                return new BaseResponse<CourseStudentResponse>(
                    "Course student grade updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseStudentResponse>(
                    $"Error updating course student grade: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> BulkAssignStudentsAsync(BulkAssignStudentsRequest request)
        {
            try
            {
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (courseInstance == null)
                {
                    return new BaseResponse<List<CourseStudentResponse>>(
                        "Course instance not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var responses = new List<CourseStudentResponse>();
                var existingStudents = (await _courseStudentRepository.GetByCourseInstanceIdAsync(request.CourseInstanceId))
                    .Select(cs => cs.UserId)
                    .ToHashSet();

                foreach (var studentId in request.StudentIds)
                {
                    if (existingStudents.Contains(studentId))
                        continue;

                    var user = await _userRepository.GetByIdAsync(studentId);
                    if (user == null)
                        continue;

                    var courseStudent = new CourseStudent
                    {
                        CourseInstanceId = request.CourseInstanceId,
                        UserId = studentId,
                        EnrolledAt = DateTime.UtcNow,
                        Status = request.Status,
                        StatusChangedAt = DateTime.UtcNow,
                        ChangedByUserId = request.ChangedByUserId
                    };

                    await _courseStudentRepository.AddAsync(courseStudent);

                    User changedByUser = null;
                    if (request.ChangedByUserId.HasValue)
                    {
                        changedByUser = await _userRepository.GetByIdAsync(request.ChangedByUserId.Value);
                    }

                    responses.Add(MapToResponse(courseStudent, courseInstance, user, changedByUser));
                }

                return new BaseResponse<List<CourseStudentResponse>>(
                    "Bulk assign students completed successfully",
                    StatusCodeEnum.Created_201,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>(
                    $"Error in bulk assign students: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<CourseStudentResponse>>> GetStudentsByCourseAndCampusAsync(int courseId, int semesterId, int campusId)
        {
            try
            {
                // Implementation sẽ cần thêm method trong repository để query phức tạp
                // Tạm thời trả về empty list
                return new BaseResponse<List<CourseStudentResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    new List<CourseStudentResponse>());
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseStudentResponse>>(
                    $"Error retrieving students: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private CourseStudentResponse MapToResponse(CourseStudent courseStudent, CourseInstance courseInstance, User user, User changedByUser)
        {
            return new CourseStudentResponse
            {
                CourseStudentId = courseStudent.CourseStudentId,
                CourseInstanceId = courseStudent.CourseInstanceId,
                CourseInstanceName = courseInstance?.SectionCode ?? string.Empty,
                CourseCode = courseInstance?.Course?.CourseCode ?? string.Empty,
                CourseName = courseInstance?.Course?.CourseName ?? string.Empty,
                UserId = courseStudent.UserId,
                StudentName = user?.FirstName ?? string.Empty,
                StudentEmail = user?.Email ?? string.Empty,
                StudentCode = user?.StudentCode ?? string.Empty,
                EnrolledAt = courseStudent.EnrolledAt,
                Status = courseStudent.Status,
                FinalGrade = courseStudent.FinalGrade,
                IsPassed = courseStudent.IsPassed,
                StatusChangedAt = courseStudent.StatusChangedAt,
                ChangedByUserId = courseStudent.ChangedByUserId,
                ChangedByUserName = changedByUser?.UserName ?? string.Empty
            };
        }
    }
}