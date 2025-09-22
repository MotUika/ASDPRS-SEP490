using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CriteriaFeedback;
using Service.RequestAndResponse.Response.CriteriaFeedback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class CriteriaFeedbackService : ICriteriaFeedbackService
    {
        private readonly ICriteriaFeedbackRepository _criteriaFeedbackRepository;
        private readonly ASDPRSContext _context;
        private readonly IMapper _mapper;

        public CriteriaFeedbackService(ICriteriaFeedbackRepository criteriaFeedbackRepository, ASDPRSContext context, IMapper mapper)
        {
            _criteriaFeedbackRepository = criteriaFeedbackRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<BaseResponse<CriteriaFeedbackResponse>> GetCriteriaFeedbackByIdAsync(int id)
        {
            try
            {
                var criteriaFeedback = await _context.CriteriaFeedbacks
                    .Include(cf => cf.Criteria)
                    .FirstOrDefaultAsync(cf => cf.CriteriaFeedbackId == id);

                if (criteriaFeedback == null)
                {
                    return new BaseResponse<CriteriaFeedbackResponse>("Criteria feedback not found", StatusCodeEnum.NotFound_404, null);
                }

                var response = _mapper.Map<CriteriaFeedbackResponse>(criteriaFeedback);
                response.CriteriaTitle = criteriaFeedback.Criteria?.Title;

                return new BaseResponse<CriteriaFeedbackResponse>("Criteria feedback retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaFeedbackResponse>($"Error retrieving criteria feedback: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CriteriaFeedbackResponse>>> GetAllCriteriaFeedbacksAsync()
        {
            try
            {
                var criteriaFeedbacks = await _context.CriteriaFeedbacks
                    .Include(cf => cf.Criteria)
                    .ToListAsync();

                var response = criteriaFeedbacks.Select(cf =>
                {
                    var criteriaFeedbackResponse = _mapper.Map<CriteriaFeedbackResponse>(cf);
                    criteriaFeedbackResponse.CriteriaTitle = cf.Criteria?.Title;
                    return criteriaFeedbackResponse;
                });

                return new BaseResponse<IEnumerable<CriteriaFeedbackResponse>>("Criteria feedbacks retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaFeedbackResponse>>($"Error retrieving criteria feedbacks: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CriteriaFeedbackResponse>> CreateCriteriaFeedbackAsync(CreateCriteriaFeedbackRequest request)
        {
            try
            {
                var criteriaFeedback = _mapper.Map<CriteriaFeedback>(request);
                var createdCriteriaFeedback = await _criteriaFeedbackRepository.AddAsync(criteriaFeedback);
                var response = _mapper.Map<CriteriaFeedbackResponse>(createdCriteriaFeedback);

                return new BaseResponse<CriteriaFeedbackResponse>("Criteria feedback created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaFeedbackResponse>($"Error creating criteria feedback: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<CriteriaFeedbackResponse>> UpdateCriteriaFeedbackAsync(UpdateCriteriaFeedbackRequest request)
        {
            try
            {
                var existingCriteriaFeedback = await _criteriaFeedbackRepository.GetByIdAsync(request.CriteriaFeedbackId);
                if (existingCriteriaFeedback == null)
                {
                    return new BaseResponse<CriteriaFeedbackResponse>("Criteria feedback not found", StatusCodeEnum.NotFound_404, null);
                }

                _mapper.Map(request, existingCriteriaFeedback);
                var updatedCriteriaFeedback = await _criteriaFeedbackRepository.UpdateAsync(existingCriteriaFeedback);
                var response = _mapper.Map<CriteriaFeedbackResponse>(updatedCriteriaFeedback);

                return new BaseResponse<CriteriaFeedbackResponse>("Criteria feedback updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<CriteriaFeedbackResponse>($"Error updating criteria feedback: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteCriteriaFeedbackAsync(int id)
        {
            try
            {
                var criteriaFeedback = await _criteriaFeedbackRepository.GetByIdAsync(id);
                if (criteriaFeedback == null)
                {
                    return new BaseResponse<bool>("Criteria feedback not found", StatusCodeEnum.NotFound_404, false);
                }

                await _criteriaFeedbackRepository.DeleteAsync(criteriaFeedback);
                return new BaseResponse<bool>("Criteria feedback deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>($"Error deleting criteria feedback: {ex.Message}", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<IEnumerable<CriteriaFeedbackResponse>>> GetCriteriaFeedbacksByReviewIdAsync(int reviewId)
        {
            try
            {
                var criteriaFeedbacks = await _criteriaFeedbackRepository.GetByReviewIdAsync(reviewId);
                var response = _mapper.Map<IEnumerable<CriteriaFeedbackResponse>>(criteriaFeedbacks);

                return new BaseResponse<IEnumerable<CriteriaFeedbackResponse>>("Criteria feedbacks retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaFeedbackResponse>>($"Error retrieving criteria feedbacks: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<IEnumerable<CriteriaFeedbackResponse>>> GetCriteriaFeedbacksByCriteriaIdAsync(int criteriaId)
        {
            try
            {
                var criteriaFeedbacks = await _criteriaFeedbackRepository.GetByCriteriaIdAsync(criteriaId);
                var response = _mapper.Map<IEnumerable<CriteriaFeedbackResponse>>(criteriaFeedbacks);

                return new BaseResponse<IEnumerable<CriteriaFeedbackResponse>>("Criteria feedbacks retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<CriteriaFeedbackResponse>>($"Error retrieving criteria feedbacks: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        }
    }