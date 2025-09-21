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
                    .Include(c => c.Courses)
                    .FirstOrDefaultAsync(c => c.CurriculumId == id);

                if (curriculum == null)
                {
                    return new BaseResponse<CurriculumResponse>("Curriculum not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<CurriculumResponse>(curriculum);
                response.CourseCount = curriculum.Courses?.Count ?? 0;
                response.CampusName = curriculum.Campus?.CampusName;

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
                    .Include(c => c.Courses)
                    .ToListAsync();

                var response = curriculums.Select(c =>
                {
                    var curriculumResponse = _mapper.Map<CurriculumResponse>(c);
                    curriculumResponse.CourseCount = c.Courses?.Count ?? 0;
                    curriculumResponse.CampusName = c.Campus?.CampusName;
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
                var curriculum = _mapper.Map<Curriculum>(request);
                var createdCurriculum = await _curriculumRepository.AddAsync(curriculum);
                var response = _mapper.Map<CurriculumResponse>(createdCurriculum);

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
                var existingCurriculum = await _curriculumRepository.GetByIdAsync(request.CurriculumId);
                if (existingCurriculum == null)
                {
                    return new BaseResponse<CurriculumResponse>("Curriculum not found", StatusCodeEnum.NotFound_404, null);
                }

                _mapper.Map(request, existingCurriculum);
                var updatedCurriculum = await _curriculumRepository.UpdateAsync(existingCurriculum);
                var response = _mapper.Map<CurriculumResponse>(updatedCurriculum);

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
                var curriculums = await _curriculumRepository.GetByCampusIdAsync(campusId);
                var response = _mapper.Map<IEnumerable<CurriculumResponse>>(curriculums);

                return new BaseResponse<IEnumerable<CurriculumResponse>>("Curriculums retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CurriculumResponse>>($"Error retrieving curriculums: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}