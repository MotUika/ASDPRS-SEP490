using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Semester;
using Service.RequestAndResponse.Response.Semester;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public SemesterService(ISemesterRepository semesterRepository, ASDPRSContext context, IMapper mapper)
        {
            _semesterRepository = semesterRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<SemesterResponse>> GetSemesterByIdAsync(int id)
        {
            try
            {
                var semester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .Include(s => s.CourseInstances)
                    .FirstOrDefaultAsync(s => s.SemesterId == id);

                if (semester == null)
                {
                    return new BaseResponse<SemesterResponse>("Semester not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<SemesterResponse>(semester);
                response.CourseInstanceCount = semester.CourseInstances?.Count ?? 0;
                response.AcademicYearName = semester.AcademicYear?.Name;

                return new BaseResponse<SemesterResponse>("Semester retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<SemesterResponse>($"Error retrieving semester: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<SemesterResponse>>> GetAllSemestersAsync()
        {
            try
            {
                var semesters = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .Include(s => s.CourseInstances)
                    .ToListAsync();

                var response = semesters.Select(s =>
                {
                    var semesterResponse = _mapper.Map<SemesterResponse>(s);
                    semesterResponse.CourseInstanceCount = s.CourseInstances?.Count ?? 0;
                    semesterResponse.AcademicYearName = s.AcademicYear?.Name;
                    return semesterResponse;
                });

                return new BaseResponse<IEnumerable<SemesterResponse>>("Semesters retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<SemesterResponse>>($"Error retrieving semesters: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<SemesterResponse>> CreateSemesterAsync(CreateSemesterRequest request)
        {
            try
            {
                // Kiểm tra ngày bắt đầu phải trước ngày kết thúc
                if (request.StartDate >= request.EndDate)
                {
                    return new BaseResponse<SemesterResponse>("Start date must be before end date", StatusCodeEnum.BadRequest_400, null);
                }

                // Kiểm tra năm học có tồn tại không
                var academicYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.AcademicYearId == request.AcademicYearId);

                if (academicYear == null)
                {
                    return new BaseResponse<SemesterResponse>("Academic year not found", StatusCodeEnum.NotFound_404, null);
                }

                // Kiểm tra học kỳ phải nằm HOÀN TOÀN trong năm học
                if (request.StartDate < academicYear.StartDate || request.EndDate > academicYear.EndDate)
                {
                    return new BaseResponse<SemesterResponse>(
                        $"Semester must be entirely within the academic year period ({academicYear.StartDate:dd/MM/yyyy} - {academicYear.EndDate:dd/MM/yyyy})",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Kiểm tra không có học kỳ nào khác có thời gian giao nhau trong cùng năm học
                var overlappingSemester = await _context.Semesters
                    .Where(s => s.AcademicYearId == request.AcademicYearId)
                    .AnyAsync(s =>
                        // Kiểm tra bất kỳ phần nào của học kỳ mới có nằm trong học kỳ hiện có
                        (request.StartDate >= s.StartDate && request.StartDate < s.EndDate) ||
                        (request.EndDate > s.StartDate && request.EndDate <= s.EndDate) ||
                        // Hoặc học kỳ mới bao phủ hoàn toàn học kỳ hiện có
                        (request.StartDate <= s.StartDate && request.EndDate >= s.EndDate)
                    );

                if (overlappingSemester)
                {
                    return new BaseResponse<SemesterResponse>(
                        "Semester period overlaps with an existing semester in the same academic year",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                var semester = _mapper.Map<Semester>(request);
                var createdSemester = await _semesterRepository.AddAsync(semester);
                var response = _mapper.Map<SemesterResponse>(createdSemester);

                return new BaseResponse<SemesterResponse>("Semester created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<SemesterResponse>($"Error creating semester: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<SemesterResponse>> UpdateSemesterAsync(UpdateSemesterRequest request)
        {
            try
            {
                var existingSemester = await _semesterRepository.GetByIdAsync(request.SemesterId);
                if (existingSemester == null)
                {
                    return new BaseResponse<SemesterResponse>("Semester not found", StatusCodeEnum.NotFound_404, null);
                }

                // Kiểm tra ngày bắt đầu phải trước ngày kết thúc
                if (request.StartDate >= request.EndDate)
                {
                    return new BaseResponse<SemesterResponse>("Start date must be before end date", StatusCodeEnum.BadRequest_400, null);
                }

                // Kiểm tra năm học có tồn tại không
                var academicYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.AcademicYearId == request.AcademicYearId);

                if (academicYear == null)
                {
                    return new BaseResponse<SemesterResponse>("Academic year not found", StatusCodeEnum.NotFound_404, null);
                }

                // Kiểm tra học kỳ phải nằm HOÀN TOÀN trong năm học
                if (request.StartDate < academicYear.StartDate || request.EndDate > academicYear.EndDate)
                {
                    return new BaseResponse<SemesterResponse>(
                        $"Semester must be entirely within the academic year period ({academicYear.StartDate:dd/MM/yyyy} - {academicYear.EndDate:dd/MM/yyyy})",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Kiểm tra không có học kỳ nào khác có thời gian giao nhau trong cùng năm học (trừ chính nó)
                var overlappingSemester = await _context.Semesters
                    .Where(s => s.AcademicYearId == request.AcademicYearId && s.SemesterId != request.SemesterId)
                    .AnyAsync(s =>
                        (request.StartDate >= s.StartDate && request.StartDate < s.EndDate) ||
                        (request.EndDate > s.StartDate && request.EndDate <= s.EndDate) ||
                        (request.StartDate <= s.StartDate && request.EndDate >= s.EndDate)
                    );

                if (overlappingSemester)
                {
                    return new BaseResponse<SemesterResponse>(
                        "Semester period overlaps with an existing semester in the same academic year",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                _mapper.Map(request, existingSemester);
                var updatedSemester = await _semesterRepository.UpdateAsync(existingSemester);
                var response = _mapper.Map<SemesterResponse>(updatedSemester);

                return new BaseResponse<SemesterResponse>("Semester updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<SemesterResponse>($"Error updating semester: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteSemesterAsync(int id)
        {
            try
            {
                var semester = await _semesterRepository.GetByIdAsync(id);
                if (semester == null)
                {
                    return new BaseResponse<bool>("Semester not found", StatusCodeEnum.NotFound_404, false);
                }

                await _semesterRepository.DeleteAsync(semester);
                return new BaseResponse<bool>("Semester deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting semester: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<SemesterResponse>>> GetSemestersByAcademicYearAsync(int academicYearId)
        {
            try
            {
                var semesters = await _semesterRepository.GetByAcademicYearIdAsync(academicYearId);
                var response = _mapper.Map<IEnumerable<SemesterResponse>>(semesters);

                return new BaseResponse<IEnumerable<SemesterResponse>>("Semesters retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<SemesterResponse>>($"Error retrieving semesters: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
    }
}