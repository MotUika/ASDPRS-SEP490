using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Curriculum;
using Service.RequestAndResponse.Response.Curriculum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CurriculumService : ICurriculumService
    {
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public CurriculumService(ICurriculumRepository curriculumRepository, ASDPRSContext context, IMapper mapper)
        {
            _curriculumRepository = curriculumRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<CurriculumResponse>> GetCurriculumByIdAsync(int id)
        {
            try
            {
                var curriculum = await _context.Curriculums
                    .Include(c => c.Campus)
                    .Include(c => c.Major)
                    .Include(c => c.Courses)
                    .FirstOrDefaultAsync(c => c.CurriculumId == id);

                if (curriculum == null)
                {
                    return new BaseResponse<CurriculumResponse>("Curriculum not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<CurriculumResponse>(curriculum);
                response.CourseCount = curriculum.Courses?.Count ?? 0;
                response.CampusName = curriculum.Campus?.CampusName;
                response.MajorName = curriculum.Major?.MajorName;

                return new BaseResponse<CurriculumResponse>("Curriculum retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CurriculumResponse>($"Error retrieving curriculum: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CurriculumResponse>>> GetAllCurriculumsAsync()
        {
            try
            {
                var curriculums = await _context.Curriculums
                    .Include(c => c.Campus)
                    .Include(c => c.Major)
                    .Include(c => c.Courses)
                    .ToListAsync();

                var response = curriculums.Select(c =>
                {
                    var curriculumResponse = _mapper.Map<CurriculumResponse>(c);
                    curriculumResponse.CourseCount = c.Courses?.Count ?? 0;
                    curriculumResponse.CampusName = c.Campus?.CampusName;
                    curriculumResponse.MajorName = c.Major?.MajorName;
                    return curriculumResponse;
                });

                return new BaseResponse<IEnumerable<CurriculumResponse>>("Curriculums retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CurriculumResponse>>($"Error retrieving curriculums: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CurriculumResponse>> CreateCurriculumAsync(CreateCurriculumRequest request)
        {
            try
            {
                // Check if curriculum code already exists
                var existingCurriculum = await _context.Curriculums
                    .FirstOrDefaultAsync(c => c.CurriculumCode == request.CurriculumCode);

                if (existingCurriculum != null)
                {
                    return new BaseResponse<CurriculumResponse>("Curriculum code already exists", StatusCodeEnum.BadRequest_400, null);
                }

                // Check if campus exists
                var campusExists = await _context.Campuses.AnyAsync(c => c.CampusId == request.CampusId);
                if (!campusExists)
                {
                    return new BaseResponse<CurriculumResponse>("Campus not found", StatusCodeEnum.NotFound_404, null);
                }

                // Check if major exists
                var majorExists = await _context.Majors.AnyAsync(m => m.MajorId == request.MajorId);
                if (!majorExists)
                {
                    return new BaseResponse<CurriculumResponse>("Major not found", StatusCodeEnum.NotFound_404, null);
                }

                var curriculum = _mapper.Map<Curriculum>(request);
                var createdCurriculum = await _curriculumRepository.AddAsync(curriculum);

                // Reload with related data for response
                var curriculumWithDetails = await _context.Curriculums
                    .Include(c => c.Campus)
                    .Include(c => c.Major)
                    .Include(c => c.Courses)
                    .FirstOrDefaultAsync(c => c.CurriculumId == createdCurriculum.CurriculumId);

                var response = _mapper.Map<CurriculumResponse>(curriculumWithDetails);
                response.CourseCount = curriculumWithDetails.Courses?.Count ?? 0;
                response.CampusName = curriculumWithDetails.Campus?.CampusName;
                response.MajorName = curriculumWithDetails.Major?.MajorName;

                return new BaseResponse<CurriculumResponse>("Curriculum created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CurriculumResponse>($"Error creating curriculum: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CurriculumResponse>> UpdateCurriculumAsync(UpdateCurriculumRequest request)
        {
            try
            {
                var existingCurriculum = await _context.Curriculums
                    .Include(c => c.Campus)
                    .Include(c => c.Major)
                    .Include(c => c.Courses)
                    .FirstOrDefaultAsync(c => c.CurriculumId == request.CurriculumId);

                if (existingCurriculum == null)
                {
                    return new BaseResponse<CurriculumResponse>("Curriculum not found", StatusCodeEnum.NotFound_404, null);
                }

                // Check if curriculum code already exists (excluding current curriculum)
                if (!string.IsNullOrEmpty(request.CurriculumCode) && request.CurriculumCode != existingCurriculum.CurriculumCode)
                {
                    var codeExists = await _context.Curriculums
                        .AnyAsync(c => c.CurriculumCode == request.CurriculumCode && c.CurriculumId != request.CurriculumId);

                    if (codeExists)
                    {
                        return new BaseResponse<CurriculumResponse>("Curriculum code already exists", StatusCodeEnum.BadRequest_400, null);
                    }
                }

                // Update only provided fields
                if (request.CampusId > 0)
                {
                    var campusExists = await _context.Campuses.AnyAsync(c => c.CampusId == request.CampusId);
                    if (!campusExists)
                    {
                        return new BaseResponse<CurriculumResponse>("Campus not found", StatusCodeEnum.NotFound_404, null);
                    }
                    existingCurriculum.CampusId = request.CampusId;
                }

                if (request.MajorId > 0)
                {
                    var majorExists = await _context.Majors.AnyAsync(m => m.MajorId == request.MajorId);
                    if (!majorExists)
                    {
                        return new BaseResponse<CurriculumResponse>("Major not found", StatusCodeEnum.NotFound_404, null);
                    }
                    existingCurriculum.MajorId = request.MajorId;
                }

                if (!string.IsNullOrEmpty(request.CurriculumName))
                    existingCurriculum.CurriculumName = request.CurriculumName;

                if (!string.IsNullOrEmpty(request.CurriculumCode))
                    existingCurriculum.CurriculumCode = request.CurriculumCode;

                if (request.TotalCredits > 0)
                    existingCurriculum.TotalCredits = request.TotalCredits;

                existingCurriculum.IsActive = request.IsActive;

                var updatedCurriculum = await _curriculumRepository.UpdateAsync(existingCurriculum);
                var response = _mapper.Map<CurriculumResponse>(updatedCurriculum);
                response.CourseCount = existingCurriculum.Courses?.Count ?? 0;
                response.CampusName = existingCurriculum.Campus?.CampusName;
                response.MajorName = existingCurriculum.Major?.MajorName;

                return new BaseResponse<CurriculumResponse>("Curriculum updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CurriculumResponse>($"Error updating curriculum: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCurriculumAsync(int id)
        {
            try
            {
                var curriculum = await _curriculumRepository.GetByIdAsync(id);
                if (curriculum == null)
                {
                    return new BaseResponse<bool>("Curriculum not found", StatusCodeEnum.NotFound_404, false);
                }

                // Check if curriculum has courses
                var hasCourses = await _context.Curriculums
                    .Include(c => c.Courses)
                    .AnyAsync(c => c.CurriculumId == id && c.Courses.Any());

                if (hasCourses)
                {
                    return new BaseResponse<bool>("Cannot delete curriculum that has courses assigned", StatusCodeEnum.BadRequest_400, false);
                }

                await _curriculumRepository.DeleteAsync(curriculum);
                return new BaseResponse<bool>("Curriculum deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting curriculum: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<CurriculumResponse>>> GetCurriculumsByCampusAsync(int campusId)
        {
            try
            {
                var curriculums = await _context.Curriculums
                    .Include(c => c.Campus)
                    .Include(c => c.Major)
                    .Include(c => c.Courses)
                    .Where(c => c.CampusId == campusId)
                    .ToListAsync();

                var response = curriculums.Select(c =>
                {
                    var curriculumResponse = _mapper.Map<CurriculumResponse>(c);
                    curriculumResponse.CourseCount = c.Courses?.Count ?? 0;
                    curriculumResponse.CampusName = c.Campus?.CampusName;
                    curriculumResponse.MajorName = c.Major?.MajorName;
                    return curriculumResponse;
                });

                return new BaseResponse<IEnumerable<CurriculumResponse>>("Curriculums retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CurriculumResponse>>($"Error retrieving curriculums: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CurriculumResponse>>> GetCurriculumsByMajorAsync(int majorId)
        {
            try
            {
                var curriculums = await _context.Curriculums
                    .Include(c => c.Campus)
                    .Include(c => c.Major)
                    .Include(c => c.Courses)
                    .Where(c => c.MajorId == majorId)
                    .ToListAsync();

                var response = curriculums.Select(c =>
                {
                    var curriculumResponse = _mapper.Map<CurriculumResponse>(c);
                    curriculumResponse.CourseCount = c.Courses?.Count ?? 0;
                    curriculumResponse.CampusName = c.Campus?.CampusName;
                    curriculumResponse.MajorName = c.Major?.MajorName;
                    return curriculumResponse;
                });

                return new BaseResponse<IEnumerable<CurriculumResponse>>("Curriculums retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CurriculumResponse>>($"Error retrieving curriculums: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}