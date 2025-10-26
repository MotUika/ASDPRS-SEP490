using AutoMapper;
using BussinessObject.Models;
using Repository.IRepository;
using Service.Interface;
using Service.RequestAndResponse.Request.RegradeRequest;
using Service.RequestAndResponse.Response.RegradeRequest;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Service.Service
{
    public class RegradeRequestService : IRegradeRequestService
    {
        private readonly IRegradeRequestRepository _regradeRequestRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<RegradeRequestService> _logger;

        public RegradeRequestService(
            IRegradeRequestRepository regradeRequestRepository,
            ISubmissionRepository submissionRepository,
            IUserRepository userRepository,
            IAssignmentRepository assignmentRepository,
            IMapper mapper,
            ILogger<RegradeRequestService> logger)
        {
            _regradeRequestRepository = regradeRequestRepository;
            _submissionRepository = submissionRepository;
            _userRepository = userRepository;
            _assignmentRepository = assignmentRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BaseResponse<RegradeRequestResponse>> CreateRegradeRequestAsync(CreateRegradeRequestRequest request)
        {
            try
            {
                // Check if submission exists
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null || assignment.Status != "GradesPublished")
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Cannot request regrade before grades are published", StatusCodeEnum.BadRequest_400, null);
                }

                // Check if student exists and is the owner of the submission
                if (submission.UserId != request.RequestedByUserId)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "You can only create regrade requests for your own submissions",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                // Check if there's already a pending request for this submission
                var hasPendingRequest = await _regradeRequestRepository.HasPendingRequestForSubmissionAsync(request.SubmissionId);
                if (hasPendingRequest)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "There is already a pending regrade request for this submission",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                // Create new regrade request
                var regradeRequest = new RegradeRequest
                {
                    SubmissionId = request.SubmissionId,
                    Reason = request.Reason,
                    Status = "Pending",
                    RequestedAt = DateTime.UtcNow
                };

                var createdRequest = await _regradeRequestRepository.AddAsync(regradeRequest);
                var response = await MapToRegradeRequestResponse(createdRequest);

                _logger.LogInformation($"Regrade request created successfully. RequestId: {createdRequest.RequestId}");

                return new BaseResponse<RegradeRequestResponse>(
                    "Regrade request created successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating regrade request");
                return new BaseResponse<RegradeRequestResponse>(
                    "An error occurred while creating the regrade request",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<RegradeRequestResponse>> GetRegradeRequestByIdAsync(GetRegradeRequestByIdRequest request)
        {
            try
            {
                var regradeRequest = await _regradeRequestRepository.GetByIdAsync(request.RequestId);
                if (regradeRequest == null)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Regrade request not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToRegradeRequestResponse(regradeRequest);

                return new BaseResponse<RegradeRequestResponse>(
                    "Regrade request retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving regrade request with ID: {request.RequestId}");
                return new BaseResponse<RegradeRequestResponse>(
                    "An error occurred while retrieving the regrade request",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<RegradeRequestListResponse>> GetRegradeRequestsByFilterAsync(GetRegradeRequestsByFilterRequest request)
        {
            try
            {
                IEnumerable<RegradeRequest> requests;

                if (request.SubmissionId.HasValue)
                {
                    requests = await _regradeRequestRepository.GetBySubmissionIdAsync(request.SubmissionId.Value);
                }
                else if (request.StudentId.HasValue)
                {
                    requests = await _regradeRequestRepository.GetByStudentIdAsync(request.StudentId.Value);
                }
                else if (request.InstructorId.HasValue)
                {
                    requests = await _regradeRequestRepository.GetByInstructorIdAsync(request.InstructorId.Value);
                }
                else if (!string.IsNullOrEmpty(request.Status))
                {
                    requests = await _regradeRequestRepository.GetByStatusAsync(request.Status);
                }
                else if (request.AssignmentId.HasValue)
                {
                    requests = await _regradeRequestRepository.GetRequestsByAssignmentIdAsync(request.AssignmentId.Value);
                }
                else
                {
                    // Get all requests with pagination
                    var allRequests = await _regradeRequestRepository.GetAllAsync();
                    requests = allRequests
                        .OrderByDescending(r => r.RequestedAt)
                        .Skip((request.PageNumber - 1) * request.PageSize)
                        .Take(request.PageSize);
                }

                var requestList = requests.ToList();
                var responseList = new List<RegradeRequestResponse>();

                foreach (var req in requestList)
                {
                    responseList.Add(await MapToRegradeRequestResponse(req));
                }

                var totalCount = await GetTotalCountByFilter(request);

                var response = new RegradeRequestListResponse
                {
                    Requests = responseList,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                };

                return new BaseResponse<RegradeRequestListResponse>(
                    "Regrade requests retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regrade requests by filter");
                return new BaseResponse<RegradeRequestListResponse>(
                    "An error occurred while retrieving regrade requests",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<RegradeRequestResponse>> UpdateRegradeRequestAsync(UpdateRegradeRequestRequest request)
        {
            try
            {
                var existingRequest = await _regradeRequestRepository.GetByIdAsync(request.RequestId);
                if (existingRequest == null)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Regrade request not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Reason))
                {
                    existingRequest.Reason = request.Reason;
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    existingRequest.Status = request.Status;
                }

                if (!string.IsNullOrEmpty(request.ResolutionNotes))
                {
                    existingRequest.ResolutionNotes = request.ResolutionNotes;
                }

                if (request.ReviewedByInstructorId.HasValue)
                {
                    existingRequest.ReviewedByInstructorId = request.ReviewedByInstructorId.Value;
                }

                var updatedRequest = await _regradeRequestRepository.UpdateAsync(existingRequest);
                var response = await MapToRegradeRequestResponse(updatedRequest);

                _logger.LogInformation($"Regrade request updated successfully. RequestId: {updatedRequest.RequestId}");

                return new BaseResponse<RegradeRequestResponse>(
                    "Regrade request updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating regrade request with ID: {request.RequestId}");
                return new BaseResponse<RegradeRequestResponse>(
                    "An error occurred while updating the regrade request",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<RegradeRequestResponse>> UpdateRegradeRequestStatusAsync(UpdateRegradeRequestStatusRequest request)
        {
            try
            {
                var existingRequest = await _regradeRequestRepository.GetByIdAsync(request.RequestId);
                if (existingRequest == null)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Regrade request not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var updatedRequest = await _regradeRequestRepository.UpdateRequestStatusAsync(
                    request.RequestId,
                    request.Status,
                    request.ResolutionNotes,
                    request.ReviewedByInstructorId);

                var response = await MapToRegradeRequestResponse(updatedRequest);

                _logger.LogInformation($"Regrade request status updated successfully. RequestId: {updatedRequest.RequestId}, Status: {request.Status}");

                return new BaseResponse<RegradeRequestResponse>(
                    "Regrade request status updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating regrade request status for ID: {request.RequestId}");
                return new BaseResponse<RegradeRequestResponse>(
                    "An error occurred while updating the regrade request status",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> CheckPendingRequestExistsAsync(int submissionId)
        {
            try
            {
                var exists = await _regradeRequestRepository.HasPendingRequestForSubmissionAsync(submissionId);
                return new BaseResponse<bool>(
                    exists ? "Pending request exists" : "No pending request found",
                    StatusCodeEnum.OK_200,
                    exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking pending request for submission ID: {submissionId}");
                return new BaseResponse<bool>(
                    "An error occurred while checking for pending requests",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<RegradeRequestListResponse>> GetPendingRegradeRequestsAsync(int pageNumber = 1, int pageSize = 20)
        {
            var filterRequest = new GetRegradeRequestsByFilterRequest
            {
                Status = "Pending",
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetRegradeRequestsByFilterAsync(filterRequest);
        }

        public async Task<BaseResponse<RegradeRequestListResponse>> GetRegradeRequestsByStudentIdAsync(int studentId, int pageNumber = 1, int pageSize = 20)
        {
            var filterRequest = new GetRegradeRequestsByFilterRequest
            {
                StudentId = studentId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetRegradeRequestsByFilterAsync(filterRequest);
        }

        public async Task<BaseResponse<RegradeRequestListResponse>> GetRegradeRequestsByInstructorIdAsync(int instructorId, int pageNumber = 1, int pageSize = 20)
        {
            var filterRequest = new GetRegradeRequestsByFilterRequest
            {
                InstructorId = instructorId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetRegradeRequestsByFilterAsync(filterRequest);
        }

        private async Task<RegradeRequestResponse> MapToRegradeRequestResponse(RegradeRequest regradeRequest)
        {
            var response = _mapper.Map<RegradeRequestResponse>(regradeRequest);

            // Load additional data if needed
            if (regradeRequest.Submission != null)
            {
                response.Submission = _mapper.Map<SubmissionInfoResponse>(regradeRequest.Submission);

                if (regradeRequest.Submission.User != null)
                {
                    response.RequestedByStudent = _mapper.Map<UserInfoRegradeResponse>(regradeRequest.Submission.User);
                }

                if (regradeRequest.Submission.Assignment != null)
                {
                    response.Assignment = _mapper.Map<AssignmentInfoRegradeResponse>(regradeRequest.Submission.Assignment);
                }
            }

            if (regradeRequest.ReviewedByInstructor != null)
            {
                response.ReviewedByInstructor = _mapper.Map<UserInfoRegradeResponse>(regradeRequest.ReviewedByInstructor);
            }

            return response;
        }

        private async Task<int> GetTotalCountByFilter(GetRegradeRequestsByFilterRequest request)
        {
            var allRequests = await _regradeRequestRepository.GetAllAsync();
            return allRequests.Count();
        }
    }
}