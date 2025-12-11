using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Campus;
using Service.RequestAndResponse.Response.Campus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CampusService : ICampusService
    {
        private readonly ICampusRepository _campusRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public CampusService(ICampusRepository campusRepository, ASDPRSContext context, IMapper mapper)
        {
            _campusRepository = campusRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<CampusResponse>> GetCampusByIdAsync(int id)
        {
            try
            {
                var campus = await _context.Campuses
                    .Include(c => c.Users)
                    .Include(c => c.AcademicYears)
                    .Include(c => c.CourseInstances)
                    .FirstOrDefaultAsync(c => c.CampusId == id);

                if (campus == null)
                {
                    return new BaseResponse<CampusResponse>("Campus not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<CampusResponse>(campus);
                response.UserCount = campus.Users?.Count ?? 0;
                response.AcademicYearCount = campus.AcademicYears?.Count ?? 0;
                response.CourseInstanceCount = campus.CourseInstances?.Count ?? 0;

                return new BaseResponse<CampusResponse>("Campus retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CampusResponse>($"Error retrieving campus: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task<BaseResponse<IEnumerable<CampusResponse>>> GetAllCampusesAsync()
        {
            try
            {
                var campuses = await _context.Campuses
                    .Include(c => c.Users)
                    .Include(c => c.AcademicYears)
                    .Include(c => c.CourseInstances)
                    .ToListAsync();

                var response = campuses.Select(campus =>
                {
                    var campusResponse = _mapper.Map<CampusResponse>(campus);
                    campusResponse.UserCount = campus.Users?.Count ?? 0;
                    campusResponse.AcademicYearCount = campus.AcademicYears?.Count ?? 0;
                    campusResponse.CourseInstanceCount = campus.CourseInstances?.Count ?? 0;
                    return campusResponse;
                });

                return new BaseResponse<IEnumerable<CampusResponse>>("Campuses retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CampusResponse>>($"Error retrieving campuses: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task<BaseResponse<CampusResponse>> CreateCampusAsync(CreateCampusRequest request)
        {
            try
            {
                var campus = _mapper.Map<Campus>(request);
                var createdCampus = await _campusRepository.AddAsync(campus);
                var response = _mapper.Map<CampusResponse>(createdCampus);

                return new BaseResponse<CampusResponse>("Campus created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CampusResponse>($"Error creating campus: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CampusResponse>> UpdateCampusAsync(UpdateCampusRequest request)
        {
            try
            {
                var existingCampus = await _campusRepository.GetByIdAsync(request.CampusId);
                if (existingCampus == null)
                {
                    return new BaseResponse<CampusResponse>("Campus not found", StatusCodeEnum.NotFound_404, null);
                }

                _mapper.Map(request, existingCampus);
                var updatedCampus = await _campusRepository.UpdateAsync(existingCampus);
                var response = _mapper.Map<CampusResponse>(updatedCampus);

                return new BaseResponse<CampusResponse>("Campus updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CampusResponse>($"Error updating campus: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCampusAsync(int id)
        {
            try
            {
                var campus = await _campusRepository.GetByIdAsync(id);
                if (campus == null)
                {
                    return new BaseResponse<bool>("Campus not found", StatusCodeEnum.NotFound_404, false);
                }

                await _campusRepository.DeleteAsync(campus);
                return new BaseResponse<bool>("Campus deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting campus: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }
    }
}