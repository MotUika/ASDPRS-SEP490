using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Course;
using Service.RequestAndResponse.Response.Course;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public CourseService(ICourseRepository courseRepository, ASDPRSContext context, IMapper mapper)
        {
            _courseRepository = courseRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<CourseResponse>> GetCourseByIdAsync(int id)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Curriculum)
                        .ThenInclude(cur => cur.Major)
                    .Include(c => c.CourseInstances)
                    .FirstOrDefaultAsync(c => c.CourseId == id);

                if (course == null)
                {
                    return new BaseResponse<CourseResponse>("Course not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<CourseResponse>(course);
                return new BaseResponse<CourseResponse>("Course retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseResponse>($"Error retrieving course: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseResponse>>> GetAllCoursesAsync()
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Curriculum)
                        .ThenInclude(cur => cur.Major)
                    .Include(c => c.CourseInstances)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseResponse>>(courses);
                return new BaseResponse<IEnumerable<CourseResponse>>("Courses retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseResponse>>($"Error retrieving courses: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CourseResponse>> CreateCourseAsync(CreateCourseRequest request)
        {
            try
            {
                // Validate if curriculum exists
                var curriculumExists = await _context.Curriculums.AnyAsync(c => c.CurriculumId == request.CurriculumId);
                if (!curriculumExists)
                {
                    return new BaseResponse<CourseResponse>("Curriculum not found", StatusCodeEnum.NotFound_404, null);
                }

                // Check for duplicate course code in the same curriculum
                var duplicateCourse = await _context.Courses
                    .AnyAsync(c => c.CurriculumId == request.CurriculumId && c.CourseCode == request.CourseCode);

                if (duplicateCourse)
                {
                    return new BaseResponse<CourseResponse>("Course code already exists in this curriculum", StatusCodeEnum.BadRequest_400, null);
                }

                var course = _mapper.Map<Course>(request);
                var createdCourse = await _courseRepository.AddAsync(course);

                // Reload with related data for response
                var courseWithDetails = await _context.Courses
                    .Include(c => c.Curriculum)
                        .ThenInclude(cur => cur.Major)
                    .Include(c => c.CourseInstances)
                    .FirstOrDefaultAsync(c => c.CourseId == createdCourse.CourseId);

                var response = _mapper.Map<CourseResponse>(courseWithDetails);
                return new BaseResponse<CourseResponse>("Course created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseResponse>($"Error creating course: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CourseResponse>> UpdateCourseAsync(UpdateCourseRequest request)
        {
            try
            {
                var existingCourse = await _context.Courses
                    .Include(c => c.Curriculum)
                        .ThenInclude(cur => cur.Major)
                    .Include(c => c.CourseInstances)
                    .FirstOrDefaultAsync(c => c.CourseId == request.CourseId);

                if (existingCourse == null)
                {
                    return new BaseResponse<CourseResponse>("Course not found", StatusCodeEnum.NotFound_404, null);
                }

                // Validate curriculum if provided
                if (request.CurriculumId > 0 && request.CurriculumId != existingCourse.CurriculumId)
                {
                    var curriculumExists = await _context.Curriculums.AnyAsync(c => c.CurriculumId == request.CurriculumId);
                    if (!curriculumExists)
                    {
                        return new BaseResponse<CourseResponse>("Curriculum not found", StatusCodeEnum.NotFound_404, null);
                    }
                }

                // Check for duplicate course code if provided
                if (!string.IsNullOrEmpty(request.CourseCode) && request.CourseCode != existingCourse.CourseCode)
                {
                    var curriculumIdToCheck = request.CurriculumId > 0 ? request.CurriculumId : existingCourse.CurriculumId;
                    var duplicateCourse = await _context.Courses
                        .AnyAsync(c => c.CurriculumId == curriculumIdToCheck &&
                                     c.CourseCode == request.CourseCode &&
                                     c.CourseId != request.CourseId);

                    if (duplicateCourse)
                    {
                        return new BaseResponse<CourseResponse>("Course code already exists in this curriculum", StatusCodeEnum.BadRequest_400, null);
                    }
                }

                // Update only provided fields
                if (request.CurriculumId > 0) existingCourse.CurriculumId = request.CurriculumId;
                if (!string.IsNullOrEmpty(request.CourseCode)) existingCourse.CourseCode = request.CourseCode;
                if (!string.IsNullOrEmpty(request.CourseName)) existingCourse.CourseName = request.CourseName;
                if (request.Credits > 0) existingCourse.Credits = request.Credits;

                existingCourse.IsActive = request.IsActive;

                var updatedCourse = await _courseRepository.UpdateAsync(existingCourse);
                var response = _mapper.Map<CourseResponse>(updatedCourse);

                return new BaseResponse<CourseResponse>("Course updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseResponse>($"Error updating course: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCourseAsync(int id)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseInstances)
                    .FirstOrDefaultAsync(c => c.CourseId == id);

                if (course == null)
                {
                    return new BaseResponse<bool>("Course not found", StatusCodeEnum.NotFound_404, false);
                }

                // Check if course has course instances
                if (course.CourseInstances.Any())
                {
                    return new BaseResponse<bool>("Cannot delete course that has course instances", StatusCodeEnum.BadRequest_400, false);
                }

                await _courseRepository.DeleteAsync(course);
                return new BaseResponse<bool>("Course deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting course: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseResponse>>> GetCoursesByCurriculumAsync(int curriculumId)
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Curriculum)
                        .ThenInclude(cur => cur.Major)
                    .Include(c => c.CourseInstances)
                    .Where(c => c.CurriculumId == curriculumId)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseResponse>>(courses);
                return new BaseResponse<IEnumerable<CourseResponse>>("Courses retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseResponse>>($"Error retrieving courses: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseResponse>>> GetCoursesByCodeAsync(string courseCode)
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Curriculum)
                        .ThenInclude(cur => cur.Major)
                    .Include(c => c.CourseInstances)
                    .Where(c => c.CourseCode.Contains(courseCode))
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseResponse>>(courses);
                return new BaseResponse<IEnumerable<CourseResponse>>("Courses retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseResponse>>($"Error retrieving courses: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseResponse>>> GetActiveCoursesAsync()
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Curriculum)
                        .ThenInclude(cur => cur.Major)
                    .Include(c => c.CourseInstances)
                    .Where(c => c.IsActive)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseResponse>>(courses);
                return new BaseResponse<IEnumerable<CourseResponse>>("Active courses retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseResponse>>($"Error retrieving active courses: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseResponse>>> GetCoursesByMajorAsync(int majorId)
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Curriculum)
                        .ThenInclude(cur => cur.Major)
                    .Include(c => c.CourseInstances)
                    .Where(c => c.Curriculum.MajorId == majorId)
                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseResponse>>(courses);
                return new BaseResponse<IEnumerable<CourseResponse>>("Courses retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseResponse>>($"Error retrieving courses: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CourseResponse>>> GetCoursesByUserIdAsync(int userId)
        {
            try
            {
                var courses = await (from ci in _context.CourseInstances
                                     join ciu in _context.CourseInstructors on ci.CourseInstanceId equals ciu.CourseInstanceId
                                     join c in _context.Courses on ci.CourseId equals c.CourseId
                                     where ciu.UserId == userId
                                     select c)
                                    .Distinct()
                                    .Include(c => c.Curriculum)
                                        .ThenInclude(cur => cur.Major)
                                    .Include(c => c.CourseInstances)
                                    .ToListAsync();

                var response = _mapper.Map<IEnumerable<CourseResponse>>(courses);
                return new BaseResponse<IEnumerable<CourseResponse>>("Courses retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseResponse>>($"Error retrieving courses: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }


    }
}