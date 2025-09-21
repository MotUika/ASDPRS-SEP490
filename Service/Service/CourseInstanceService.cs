using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Repository.Repository;
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
                response.CourseCode = courseInstance.Course?.CourseCode;
                response.CourseName = courseInstance.Course?.CourseName;
                response.SemesterName = courseInstance.Semester?.Name;
                response.CampusName = courseInstance.Campus?.CampusName;
                response.InstructorCount = courseInstance.CourseInstructors?.Count ?? 0;
                response.StudentCount = courseInstance.CourseStudents?.Count ?? 0;
                response.AssignmentCount = courseInstance.Assignments?.Count ?? 0;

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

                var response = courseInstances.Select(ci =>
                {
                    var courseInstanceResponse = _mapper.Map<CourseInstanceResponse>(ci);
                    courseInstanceResponse.CourseCode = ci.Course?.CourseCode;
                    courseInstanceResponse.CourseName = ci.Course?.CourseName;
                    courseInstanceResponse.SemesterName = ci.Semester?.Name;
                    courseInstanceResponse.CampusName = ci.Campus?.CampusName;
                    courseInstanceResponse.InstructorCount = ci.CourseInstructors?.Count ?? 0;
                    courseInstanceResponse.StudentCount = ci.CourseStudents?.Count ?? 0;
                    courseInstanceResponse.AssignmentCount = ci.Assignments?.Count ?? 0;
                    return courseInstanceResponse;
                });

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
                var courseInstance = _mapper.Map<CourseInstance>(request);
                var createdCourseInstance = await _courseInstanceRepository.AddAsync(courseInstance);
                var response = _mapper.Map<CourseInstanceResponse>(createdCourseInstance);

                return new BaseResponse<CourseInstanceResponse>("Course instance created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseInstanceResponse>($"Error creating course instance: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CourseInstanceResponse>> UpdateCourseInstanceAsync(UpdateCourseInstanceRequest request)
        {
            try
            {
                var existingCourseInstance = await _courseInstanceRepository.GetByIdAsync(request.CourseInstanceId);
                if (existingCourseInstance == null)
                {
                    return new BaseResponse<CourseInstanceResponse>("Course instance not found", StatusCodeEnum.NotFound_404, null);
                }

                _mapper.Map(request, existingCourseInstance);
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
                var courseInstance = await _courseInstanceRepository.GetByIdAsync(id);
                if (courseInstance == null)
                {
                    return new BaseResponse<bool>("Course instance not found", StatusCodeEnum.NotFound_404, false);
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
                var courseInstances = await _courseInstanceRepository.GetByCourseIdAsync(courseId);
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
                var courseInstances = await _courseInstanceRepository.GetBySemesterIdAsync(semesterId);
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
                var courseInstances = await _courseInstanceRepository.GetByCampusIdAsync(campusId);
                var response = _mapper.Map<IEnumerable<CourseInstanceResponse>>(courseInstances);

                return new BaseResponse<IEnumerable<CourseInstanceResponse>>("Course instances retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CourseInstanceResponse>>($"Error retrieving course instances: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}