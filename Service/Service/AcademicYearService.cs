using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.AcademicYear;
using Service.RequestAndResponse.Response.AcademicYear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class AcademicYearService : IAcademicYearService
    {
        private readonly IAcademicYearRepository _academicYearRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public AcademicYearService(IAcademicYearRepository academicYearRepository, ASDPRSContext context, IMapper mapper)
        {
            _academicYearRepository = academicYearRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<AcademicYearResponse>> GetAcademicYearByIdAsync(int id)
        {
            try
            {
                var academicYear = await _context.AcademicYears
                    .Include(ay => ay.Campus)
                    .Include(ay => ay.Semesters)
                    .FirstOrDefaultAsync(ay => ay.AcademicYearId == id);

                if (academicYear == null)
                {
                    return new BaseResponse<AcademicYearResponse>("Academic year not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<AcademicYearResponse>(academicYear);
                response.SemesterCount = academicYear.Semesters?.Count ?? 0;
                response.CampusName = academicYear.Campus?.CampusName;

                return new BaseResponse<AcademicYearResponse>("Academic year retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AcademicYearResponse>($"Error retrieving academic year: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<AcademicYearResponse>>> GetAllAcademicYearsAsync()
        {
            try
            {
                var academicYears = await _context.AcademicYears
                    .Include(ay => ay.Campus)
                    .Include(ay => ay.Semesters)
                    .ToListAsync();

                var response = academicYears.Select(ay =>
                {
                    var academicYearResponse = _mapper.Map<AcademicYearResponse>(ay);
                    academicYearResponse.SemesterCount = ay.Semesters?.Count ?? 0;
                    academicYearResponse.CampusName = ay.Campus?.CampusName;
                    return academicYearResponse;
                });

                return new BaseResponse<IEnumerable<AcademicYearResponse>>("Academic years retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<AcademicYearResponse>>($"Error retrieving academic years: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<AcademicYearResponse>> CreateAcademicYearAsync(CreateAcademicYearRequest request)
        {
            try
            {
                // Kiểm tra ngày bắt đầu phải trước ngày kết thúc
                if (request.StartDate >= request.EndDate)
                {
                    return new BaseResponse<AcademicYearResponse>("Start date must be before end date", StatusCodeEnum.BadRequest_400, null);
                }

                // Kiểm tra không được tạo năm học trong quá khứ
                var currentDate = DateTime.Now.Date;
                if (request.StartDate.Date < currentDate)
                {
                    return new BaseResponse<AcademicYearResponse>("Cannot create academic year with start date in the past", StatusCodeEnum.BadRequest_400, null);
                }

                // Kiểm tra tên năm học đã tồn tại
                var nameExists = await _context.AcademicYears.AnyAsync(ay => ay.Name == request.Name);
                if (nameExists)
                {
                    return new BaseResponse<AcademicYearResponse>("Academic year name already exists", StatusCodeEnum.Conflict_409, null);
                }

                // Kiểm tra năm học có trùng thời gian với năm học khác không
                var overlappingYear = await _context.AcademicYears
                    .Where(ay => ay.CampusId == request.CampusId)
                    .AnyAsync(ay =>
                        (request.StartDate >= ay.StartDate && request.StartDate < ay.EndDate) ||
                        (request.EndDate > ay.StartDate && request.EndDate <= ay.EndDate) ||
                        (request.StartDate <= ay.StartDate && request.EndDate >= ay.EndDate)
                    );

                if (overlappingYear)
                {
                    return new BaseResponse<AcademicYearResponse>("Academic year period overlaps with an existing academic year", StatusCodeEnum.Conflict_409, null);
                }

                var academicYear = _mapper.Map<AcademicYear>(request);
                var createdAcademicYear = await _academicYearRepository.AddAsync(academicYear);
                var response = _mapper.Map<AcademicYearResponse>(createdAcademicYear);

                return new BaseResponse<AcademicYearResponse>("Academic year created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AcademicYearResponse>($"Error creating academic year: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<AcademicYearResponse>> UpdateAcademicYearAsync(UpdateAcademicYearRequest request)
        {
            try
            {
                var existingAcademicYear = await _academicYearRepository.GetByIdAsync(request.AcademicYearId);
                if (existingAcademicYear == null)
                {
                    return new BaseResponse<AcademicYearResponse>("Academic year not found", StatusCodeEnum.NotFound_404, null);
                }

                if (request.StartDate >= request.EndDate)
                {
                    return new BaseResponse<AcademicYearResponse>("Start date must be before end date", StatusCodeEnum.BadRequest_400, null);
                }

                if (existingAcademicYear.Name != request.Name)
                {
                    var nameExists = await _context.AcademicYears.AnyAsync(ay => ay.Name == request.Name && ay.AcademicYearId != request.AcademicYearId);
                    if (nameExists)
                    {
                        return new BaseResponse<AcademicYearResponse>("Academic year name already exists", StatusCodeEnum.Conflict_409, null);
                    }
                }

                // Kiểm tra năm học có trùng thời gian với năm học khác không (trừ chính nó)
                var overlappingYear = await _context.AcademicYears
                    .Where(ay => ay.CampusId == request.CampusId && ay.AcademicYearId != request.AcademicYearId)
                    .AnyAsync(ay =>
                        (request.StartDate >= ay.StartDate && request.StartDate < ay.EndDate) ||
                        (request.EndDate > ay.StartDate && request.EndDate <= ay.EndDate) ||
                        (request.StartDate <= ay.StartDate && request.EndDate >= ay.EndDate)
                    );

                if (overlappingYear)
                {
                    return new BaseResponse<AcademicYearResponse>("Academic year period overlaps with an existing academic year", StatusCodeEnum.Conflict_409, null);
                }

                _mapper.Map(request, existingAcademicYear);
                var updatedAcademicYear = await _academicYearRepository.UpdateAsync(existingAcademicYear);
                var response = _mapper.Map<AcademicYearResponse>(updatedAcademicYear);

                return new BaseResponse<AcademicYearResponse>("Academic year updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AcademicYearResponse>($"Error updating academic year: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteAcademicYearAsync(int id)
        {
            try
            {
                var academicYear = await _academicYearRepository.GetByIdAsync(id);
                if (academicYear == null)
                {
                    return new BaseResponse<bool>("Academic year not found", StatusCodeEnum.NotFound_404, false);
                }

                await _academicYearRepository.DeleteAsync(academicYear);
                return new BaseResponse<bool>("Academic year deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting academic year: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<AcademicYearResponse>>> GetAcademicYearsByCampusAsync(int campusId)
        {
            try
            {
                var academicYears = await _academicYearRepository.GetByCampusIdAsync(campusId);
                var response = _mapper.Map<IEnumerable<AcademicYearResponse>>(academicYears);

                return new BaseResponse<IEnumerable<AcademicYearResponse>>("Academic years retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<AcademicYearResponse>>($"Error retrieving academic years: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}