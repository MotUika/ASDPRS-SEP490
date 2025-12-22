using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Major;
using Service.RequestAndResponse.Response.Major;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service
{
    public class MajorService : IMajorService
    {
        private readonly IMajorRepository _majorRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public MajorService(IMajorRepository majorRepository, ASDPRSContext context, IMapper mapper)
        {
            _majorRepository = majorRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<MajorResponse>> GetMajorByIdAsync(int id)
        {
            try
            {
                var major = await _context.Majors
                    .FirstOrDefaultAsync(m => m.MajorId == id);

                if (major == null)
                    return new BaseResponse<MajorResponse>("Major not found", StatusCodeEnum.NotFound_404, null);

                var response = _mapper.Map<MajorResponse>(major);

                return new BaseResponse<MajorResponse>("Major retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<MajorResponse>($"Error retrieving major: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<MajorResponse>>> GetAllMajorsAsync()
        {
            try
            {
                var majors = await _context.Majors
                    .ToListAsync();

                var response = majors.Select(m =>
                {
                    var res = _mapper.Map<MajorResponse>(m);
                    return res;
                });

                return new BaseResponse<IEnumerable<MajorResponse>>("Majors retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<MajorResponse>>($"Error retrieving majors: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task<BaseResponse<MajorResponse>> CreateMajorAsync(CreateMajorRequest request)
        {
            try
            {
                // Kiểm tra trùng mã ngành
                bool exists = await _context.Majors.AnyAsync(m => m.MajorCode == request.MajorCode);
                if (exists)
                    return new BaseResponse<MajorResponse>("Major code already exists", StatusCodeEnum.BadRequest_400, null);

                var major = _mapper.Map<Major>(request);
                var createdMajor = await _majorRepository.AddAsync(major);
                var response = _mapper.Map<MajorResponse>(createdMajor);

                return new BaseResponse<MajorResponse>("Major created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<MajorResponse>($"Error creating major: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<MajorResponse>> UpdateMajorAsync(UpdateMajorRequest request)
        {
            try
            {
                var existingMajor = await _majorRepository.GetByIdAsync(request.MajorId);
                if (existingMajor == null)
                    return new BaseResponse<MajorResponse>("Major not found", StatusCodeEnum.NotFound_404, null);

                if (existingMajor.IsActive && !request.IsActive)
                {
                    bool hasUsers = await _context.Users.AnyAsync(u => u.MajorId == request.MajorId);
                    if (hasUsers)
                    {
                        return new BaseResponse<MajorResponse>(
                            "Cannot deactivate Major because there are users currently assigned to this major.",
                            StatusCodeEnum.BadRequest_400,
                            null);
                    }
                }

                _mapper.Map(request, existingMajor);
                var updated = await _majorRepository.UpdateAsync(existingMajor);
                var response = _mapper.Map<MajorResponse>(updated);

                return new BaseResponse<MajorResponse>("Major updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<MajorResponse>($"Error updating major: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteMajorAsync(int id)
        {
            try
            {
                var major = await _majorRepository.GetByIdAsync(id);
                if (major == null)
                    return new BaseResponse<bool>("Major not found", StatusCodeEnum.NotFound_404, false);

                bool hasUsers = await _context.Users.AnyAsync(u => u.MajorId == id);
                if (hasUsers)
                {
                    return new BaseResponse<bool>(
                        "Cannot delete Major because there are users currently assigned to this major.",
                        StatusCodeEnum.BadRequest_400,
                        false);
                }

                await _majorRepository.DeleteAsync(major);
                return new BaseResponse<bool>("Major deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting major: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }
    }
}
