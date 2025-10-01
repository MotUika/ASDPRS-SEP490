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
                    .Include(c => c.CourseInstances)
                    .FirstOrDefaultAsync(c => c.CourseId == id);

                if (course == null)
                {
                    return new BaseResponse<CourseResponse>("Course not found", StatusCodeEnum.NotFound_404, null);
                }
                var response = _mapper.Map<CourseResponse>(course);
                response.CourseInstanceCount = course.CourseInstances?.Count ?? 0;
                response.MajorName = course.Curriculum?.Major?.MajorName;

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
                    .Include(c => c.CourseInstances)
                    .ToListAsync();

                var response = courses.Select(c =>
                {
                    var courseResponse = _mapper.Map<CourseResponse>(c);
                    courseResponse.CourseInstanceCount = c.CourseInstances?.Count ?? 0;
                    courseResponse.MajorName = c.Curriculum?.Major?.MajorName;
                    return courseResponse;
                });

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
                var course = _mapper.Map<Course>(request);
                var createdCourse = await _courseRepository.AddAsync(course);
                var response = _mapper.Map<CourseResponse>(createdCourse);

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
                var existingCourse = await _courseRepository.GetByIdAsync(request.CourseId);
                if (existingCourse == null)
                {
                    return new BaseResponse<CourseResponse>("Course not found", StatusCodeEnum.NotFound_404, null);
                }

                _mapper.Map(request, existingCourse);
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
                var course = await _courseRepository.GetByIdAsync(id);
                if (course == null)
                {
                    return new BaseResponse<bool>("Course not found", StatusCodeEnum.NotFound_404, false);
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
                var courses = await _courseRepository.GetByCurriculumIdAsync(curriculumId);
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
                var courses = await _courseRepository.GetByCourseCodeAsync(courseCode);
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