using AutoMapper;
using BussinessObject.Models;
using Microsoft.Extensions.Logging;
using Repository.IRepository;
using Repository.Repository;
using Service.Interface;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Notification;
using Service.RequestAndResponse.Request.RegradeRequest;
using Service.RequestAndResponse.Response.RegradeRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly INotificationService _notificationService;
        private readonly ICourseInstructorRepository _courseInstructorRepository;
        private readonly ISystemConfigService _systemConfigService;

        public RegradeRequestService(
            IRegradeRequestRepository regradeRequestRepository,
            ISubmissionRepository submissionRepository,
            IUserRepository userRepository,
            IAssignmentRepository assignmentRepository,
            IMapper mapper,
            ILogger<RegradeRequestService> logger, INotificationService notificationService, ICourseInstructorRepository courseInstructorRepository, ISystemConfigService systemConfigService)
        {
            _regradeRequestRepository = regradeRequestRepository;
            _submissionRepository = submissionRepository;
            _userRepository = userRepository;
            _assignmentRepository = assignmentRepository;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
            _courseInstructorRepository = courseInstructorRepository;
            _systemConfigService = systemConfigService;
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

                if (submission.FileUrl == "Not Submitted" || submission.OriginalFileName == "Not Submitted" || submission.FileName == "Not Submitted")
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Cannot request regrade for non-submitted assignments",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null || assignment.Status != "GradesPublished")
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Cannot request regrade before grades are published",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                if (assignment.Status == "Cancelled")
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Cannot request regrade for cancelled assignments",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                if (submission.GradedAt.HasValue)
                {
                    var requestDeadlineDays = await _systemConfigService.GetConfigValueAsync<int>("RegradeRequestDeadlineDays", 3);
                    var requestDeadline = submission.GradedAt.Value.AddDays(requestDeadlineDays);

                    if (DateTime.UtcNow.AddHours(7) > requestDeadline)
                    {
                        return new BaseResponse<RegradeRequestResponse>(
                            $"Regrade request deadline has passed. Students must submit requests within {requestDeadlineDays} days after grades are published (Deadline: {requestDeadline:yyyy-MM-dd HH:mm})",
                            StatusCodeEnum.BadRequest_400,
                            null);
                    }
                }

                if (submission.UserId != request.RequestedByUserId)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "You can only create regrade requests for your own submissions",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }

                var existingRequests = await _regradeRequestRepository.GetBySubmissionIdAsync(request.SubmissionId);
                if (existingRequests.Count() >= 2)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "You have reached the maximum limit of 2 regrade requests for this submission.",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var hasPendingRequest = await _regradeRequestRepository.HasPendingRequestForSubmissionAsync(request.SubmissionId);
                if (hasPendingRequest)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "There is already a pending regrade request for this submission",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                var regradeRequest = new RegradeRequest
                {
                    SubmissionId = request.SubmissionId,
                    Reason = request.Reason,
                    Status = "Pending",
                    RequestedAt = DateTime.UtcNow.AddHours(7)
                };

                var createdRequest = await _regradeRequestRepository.AddAsync(regradeRequest);
                var response = await MapToRegradeRequestResponse(createdRequest);

                _logger.LogInformation($"Regrade request created successfully. RequestId: {createdRequest.RequestId}");
                await SendRegradeNotificationToInstructors(createdRequest, submission, assignment);


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
        private async Task SendRegradeStatusNotificationToStudent(RegradeRequest regradeRequest)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(regradeRequest.SubmissionId);
                if (submission == null) return;

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null) return;

                string message = "";
                switch (regradeRequest.Status.ToLower())
                {
                    case "approved":
                        message = $"Your regrade request for assignment '{assignment.Title}' has been approved.";
                        if (!string.IsNullOrEmpty(regradeRequest.ResolutionNotes))
                        {
                            message += $" Notes: {regradeRequest.ResolutionNotes}";
                        }
                        break;
                    case "rejected":
                        message = $"Your regrade request for assignment '{assignment.Title}' has been rejected.";
                        if (!string.IsNullOrEmpty(regradeRequest.ResolutionNotes))
                        {
                            message += $" Reason: {regradeRequest.ResolutionNotes}";
                        }
                        break;
                    case "inreview":
                        message = $"Your regrade request for assignment '{assignment.Title}' is now under review.";
                        break;
                    case "completed":
                        message = $"Your regrade request for assignment '{assignment.Title}' has been completed.";
                        if (!string.IsNullOrEmpty(regradeRequest.ResolutionNotes))
                        {
                            message += $" Notes: {regradeRequest.ResolutionNotes}";
                        }
                        break;
                    default:
                        message = $"Your regrade request for assignment '{assignment.Title}' has been updated to: {regradeRequest.Status}";
                        break;
                }

                var notificationRequest = new CreateNotificationRequest
                {
                    UserId = submission.UserId, // Gửi cho student
                    Title = "Regrade Request Status Updated",
                    Message = message,
                    Type = "RegradeStatusUpdate",
                    AssignmentId = assignment.AssignmentId,
                    SubmissionId = submission.SubmissionId
                };

                await _notificationService.CreateNotificationAsync(notificationRequest);

                _logger.LogInformation($"Sent regrade status notification to student {submission.UserId} for request {regradeRequest.RequestId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending regrade status notification to student");
                // Không throw exception để không ảnh hưởng đến flow chính
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
                    requests = await _regradeRequestRepository.GetAllAsync();
                }
                if (request.StudentId.HasValue && request.AssignmentId.HasValue)
                {
                    requests = requests.Where(r => r.Submission.AssignmentId == request.AssignmentId.Value && r.Submission.UserId == request.StudentId.Value);
                }

                var totalCount = requests.Count();

                var pagedRequests = requests
                    .OrderByDescending(r => r.RequestedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var responseList = new List<RegradeRequestResponse>();
                foreach (var req in pagedRequests)
                {
                    responseList.Add(await MapToRegradeRequestResponse(req));
                }

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
                if (request.ReviewedByUserId.HasValue)
                {
                    existingRequest.ReviewedByUserId = request.ReviewedByUserId.Value;
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
                    request.ReviewedByInstructorId,
                    request.ReviewedByUserId);

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

        public async Task<BaseResponse<RegradeRequestListResponse>> GetRegradeRequestsByInstructorIdAsync(int userId)
        {
            var filterRequest = new GetRegradeRequestsByFilterRequest
            {
                InstructorId = userId,

            };

            return await GetRegradeRequestsByFilterAsync(filterRequest);
        }

        private async Task<RegradeRequestResponse> MapToRegradeRequestResponse(RegradeRequest regradeRequest)
        {
            var response = _mapper.Map<RegradeRequestResponse>(regradeRequest);

            if (regradeRequest.Submission != null)
            {
                var submission = regradeRequest.Submission;
                response.Submission = _mapper.Map<SubmissionInfoResponse>(regradeRequest.Submission);
                response.Submission.FileUrl = regradeRequest.Submission.FileUrl;
                response.Submission.FileName = regradeRequest.Submission.FileName;
                response.Submission.PreviewUrl = GeneratePreviewUrl(regradeRequest.Submission.FileUrl);
                response.Submission.InstructorScore = regradeRequest.Submission.InstructorScore;
                response.Submission.PeerAverageScore = regradeRequest.Submission.PeerAverageScore;
                response.Submission.FinalScore = regradeRequest.Submission.FinalScore;
                response.Submission.Feedback = regradeRequest.Submission.Feedback;
                response.Submission.GradedAt = regradeRequest.Submission.GradedAt;

                response.GradeInfo = new GradeInfoResponse
                {
                    FinalScoreAfterRegrade = regradeRequest.Submission.FinalScore,
                    InstructorScore = regradeRequest.Submission.InstructorScore,
                    PeerAverageScore = regradeRequest.Submission.PeerAverageScore,
                    InstructorFeedback = regradeRequest.Submission.Feedback,
                    GradedAt = regradeRequest.Submission.GradedAt,
                    HasBeenRegraded = regradeRequest.Status == "Completed" || regradeRequest.Status == "Approved",
                    RegradeStatus = regradeRequest.Status
                };

                if (regradeRequest.Submission.User != null)
                {
                    response.RequestedByStudent = _mapper.Map<UserInfoRegradeResponse>(regradeRequest.Submission.User);
                }

                // Map Assignment
                if (submission.Assignment != null)
                {
                    var assignment = submission.Assignment;
                    response.Assignment = _mapper.Map<AssignmentInfoRegradeResponse>(assignment);

                    var courseName = assignment.CourseInstance?.Course?.CourseName ?? "Unknown Course";
                    var className = assignment.CourseInstance?.SectionCode ?? "Unknown Section";

                    response.CourseName = assignment.CourseInstance?.Course?.CourseName;
                    response.ClassName = assignment.CourseInstance?.SectionCode;

                    if (response.Assignment != null)
                    {
                        response.CourseName = assignment.CourseInstance?.Course?.CourseName;
                        response.ClassName = assignment.CourseInstance?.SectionCode;
                    }
                }
            }

            if (regradeRequest.ReviewedByInstructor != null)
            {
                response.ReviewedByInstructor = _mapper.Map<UserInfoRegradeResponse>(regradeRequest.ReviewedByInstructor);
            }

            var processingDeadlineDays = await _systemConfigService.GetConfigValueAsync<int>("RegradeProcessingDeadlineDays", 7);
            response.ProcessingDeadline = regradeRequest.RequestedAt.AddDays(processingDeadlineDays);

            if (regradeRequest.Submission?.GradedAt.HasValue == true)
            {
                var requestDeadlineDays = await _systemConfigService.GetConfigValueAsync<int>("RegradeRequestDeadlineDays", 3);
                response.StudentRequestDeadline = regradeRequest.Submission.GradedAt.Value.AddDays(requestDeadlineDays);
            }

            return response;
        }

        private async Task<int> GetTotalCountByFilter(GetRegradeRequestsByFilterRequest request)
        {
            var allRequests = await _regradeRequestRepository.GetAllAsync();
            return allRequests.Count();
        }

        public async Task<BaseResponse<RegradeRequestResponse>> ReviewRegradeRequestAsync(UpdateRegradeRequestStatusByUserRequest request)
        {
            try
            {
                var existingRequest = await _regradeRequestRepository.GetByIdAsync(request.RequestId);
                if (existingRequest == null)
                {
                    _logger.LogWarning($"Regrade request not found. RequestId: {request.RequestId}");
                    return new BaseResponse<RegradeRequestResponse>(
                        "Regrade request not found",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                var deadlineDays = await _systemConfigService.GetConfigValueAsync<int>("RegradeProcessingDeadlineDays", 7);
                var deadline = existingRequest.RequestedAt.AddDays(deadlineDays);
                if (DateTime.UtcNow.AddHours(7) > deadline && existingRequest.Status == "Pending")
                {
                    _logger.LogWarning($"Regrade request processing deadline exceeded. RequestId: {request.RequestId}");
                    return new BaseResponse<RegradeRequestResponse>(
                        "Processing deadline for this regrade request has passed",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // Chỉ cho phép status "Approved" hoặc "Rejected"
                if (request.Status != "Approved" && request.Status != "Rejected")
                {
                    _logger.LogWarning($"Invalid status for review. RequestId: {request.RequestId}, Status: {request.Status}");
                    return new BaseResponse<RegradeRequestResponse>(
                        "Invalid status for review",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                int? courseInstanceId = existingRequest.Submission?.Assignment?.CourseInstanceId;
                if (courseInstanceId == null)
                {
                    _logger.LogError($"Cannot determine CourseInstanceId for RequestId: {request.RequestId}");
                    return new BaseResponse<RegradeRequestResponse>(
                        "Cannot determine instructor for this request",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // Lấy danh sách instructor qua repository
                var instructors = await _courseInstructorRepository.GetByCourseInstanceIdAsync(courseInstanceId.Value);
                if (!instructors.Any())
                {
                    _logger.LogError($"No instructors found for CourseInstanceId: {courseInstanceId.Value}, RequestId: {request.RequestId}");
                    return new BaseResponse<RegradeRequestResponse>(
                        "Cannot determine instructor for this request",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                int instructorId;
                if (request.ReviewedByUserId.HasValue)
                {
                    if (!instructors.Any(i => i.UserId == request.ReviewedByUserId.Value))
                    {
                        _logger.LogError($"ReviewedByUserId {request.ReviewedByUserId.Value} is not an instructor of CourseInstanceId: {courseInstanceId.Value}");
                        return new BaseResponse<RegradeRequestResponse>(
                            "ReviewedByUserId is not an instructor of this course",
                            StatusCodeEnum.Forbidden_403,
                            null
                        );
                    }
                    instructorId = request.ReviewedByUserId.Value;
                }
                else
                {
                    if (instructors.Count() > 1)
                    {
                        var ids = string.Join(",", instructors.Select(c => c.UserId));
                        _logger.LogError($"Multiple instructors found ({ids}) for CourseInstanceId: {courseInstanceId.Value}, RequestId: {request.RequestId}");
                        return new BaseResponse<RegradeRequestResponse>(
                            "Multiple instructors found, cannot determine which instructor reviewed",
                            StatusCodeEnum.BadRequest_400,
                            null
                        );
                    }
                    instructorId = instructors.First().UserId;
                }

                var updatedRequest = await _regradeRequestRepository.UpdateRequestStatusAsync(
                    request.RequestId,
                    request.Status,
                    request.ResolutionNotes,
                    instructorId,
                    request.ReviewedByUserId
                );
                await SendRegradeStatusNotificationToStudent(updatedRequest);

                var fullRequest = await _regradeRequestRepository.GetByIdWithInstructorAsync(request.RequestId);
                var response = await MapToRegradeRequestResponse(fullRequest);

                _logger.LogInformation($"Regrade request reviewed. RequestId: {updatedRequest.RequestId}, Status: {request.Status}, InstructorId: {instructorId}");

                return new BaseResponse<RegradeRequestResponse>(
                    "Regrade request reviewed successfully",
                    StatusCodeEnum.OK_200,
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reviewing regrade request ID: {request.RequestId}");
                return new BaseResponse<RegradeRequestResponse>(
                    "Error reviewing regrade request",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }


        public async Task<BaseResponse<RegradeRequestResponse>> CompleteRegradeRequestAsync(UpdateRegradeRequestStatusByUserRequest request)
        {
            try
            {

                var existingRequest = await _regradeRequestRepository.GetByIdAsync(request.RequestId);
                if (existingRequest == null)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Regrade request not found",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                // Chỉ cho phép hoàn thành khi status là Approved
                if (!string.Equals(existingRequest.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Regrade request must be in 'Approved' status before completing",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                var deadlineDays = await _systemConfigService.GetConfigValueAsync<int>("RegradeProcessingDeadlineDays", 7);
                var deadline = existingRequest.RequestedAt.AddDays(deadlineDays);
                if (DateTime.UtcNow.AddHours(7) > deadline)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Processing deadline for this regrade request has passed",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // Lấy CourseInstanceId
                int? courseInstanceId = existingRequest.Submission?.Assignment?.CourseInstanceId;
                if (courseInstanceId == null)
                {
                    return new BaseResponse<RegradeRequestResponse>(
                        "Cannot determine course instance for this request",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // Lấy danh sách instructor của course
                var instructors = await _courseInstructorRepository.GetByCourseInstanceIdAsync(courseInstanceId.Value);

                int instructorId;

                // Nếu client gửi ReviewedByUserId → check instructor
                if (request.ReviewedByUserId.HasValue)
                {
                    int id = request.ReviewedByUserId.Value;
                    if (!instructors.Any(i => i.UserId == id))
                    {
                        return new BaseResponse<RegradeRequestResponse>(
                            "ReviewedByUserId is not an instructor of this course",
                            StatusCodeEnum.Forbidden_403,
                            null
                        );
                    }
                    instructorId = id;
                }
                else
                {
                    // Tự infer instructor duy nhất
                    if (!instructors.Any())
                    {
                        return new BaseResponse<RegradeRequestResponse>(
                            "No instructor found for this course instance",
                            StatusCodeEnum.BadRequest_400,
                            null
                        );
                    }
                    if (instructors.Count() > 1)
                    {
                        return new BaseResponse<RegradeRequestResponse>(
                            "Multiple instructors found, cannot determine which instructor completed the request",
                            StatusCodeEnum.BadRequest_400,
                            null
                        );
                    }
                    instructorId = instructors.First().UserId;
                }

                // Update status luôn là "Completed"
                var updatedRequest = await _regradeRequestRepository.UpdateRequestStatusAsync(
                    request.RequestId,
                    "Completed",
                    request.ResolutionNotes,
                    instructorId,
                    request.ReviewedByUserId
                );

                // UPDATE GRADED AT
                if (updatedRequest.SubmissionId != null)
                {
                    await _submissionRepository.UpdateGradedAtAsync(updatedRequest.SubmissionId);
                }

                await SendRegradeStatusNotificationToStudent(updatedRequest);

                // Load full request để map response
                var fullRequest = await _regradeRequestRepository.GetByIdWithInstructorAsync(request.RequestId);
                var response = await MapToRegradeRequestResponse(fullRequest);

                return new BaseResponse<RegradeRequestResponse>(
                    "Regrade request completed successfully",
                    StatusCodeEnum.OK_200,
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing regrade request ID: {request.RequestId}");
                return new BaseResponse<RegradeRequestResponse>(
                    "Error completing regrade request",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }


        private async Task SendRegradeNotificationToInstructors(RegradeRequest regradeRequest, Submission submission, Assignment assignment)
        {
            try
            {
                var deadlineDays = await _systemConfigService.GetConfigValueAsync<int>("RegradeProcessingDeadlineDays", 7);
                var deadline = regradeRequest.RequestedAt.AddDays(deadlineDays).ToString("yyyy-MM-dd HH:mm:ss");
                // Lấy danh sách instructors của course
                var instructors = await _courseInstructorRepository.GetByCourseInstanceIdAsync(assignment.CourseInstanceId);
                // Lấy thông tin student
                var student = await _userRepository.GetByIdAsync(submission.UserId);
                var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown Student";
                foreach (var instructor in instructors)
                {
                    var notificationRequest = new CreateNotificationRequest
                    {
                        UserId = instructor.UserId,
                        Title = "New Regrade Request Submitted",
                        Message = $"Student {studentName} has submitted a regrade request for assignment '{assignment.Title}'. Reason: {regradeRequest.Reason}",
                        Type = "RegradeRequest",
                        AssignmentId = assignment.AssignmentId,
                        SubmissionId = submission.SubmissionId,
                        SenderUserId = submission.UserId // Student là người gửi
                    };
                    var result = await _notificationService.CreateNotificationAsync(notificationRequest);
                    if (result.StatusCode != StatusCodeEnum.Created_201)
                    {
                        _logger.LogError($"Failed to create notification for instructor {instructor.UserId}: {result.Message}");
                    }
                }
                _logger.LogInformation($"Sent regrade notifications to {instructors.Count()} instructors for request {regradeRequest.RequestId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending regrade notifications to instructors");
                // Không throw exception để không ảnh hưởng đến flow chính
            }
        }

        private string GeneratePreviewUrl(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl) || fileUrl == "Not Submitted")
                return null;

            try
            {
                string encodedUrl = Uri.EscapeDataString(fileUrl);
                string extension = Path.GetExtension(fileUrl).ToLower();

                // 1. Ảnh -> Link gốc
                if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
                {
                    return fileUrl;
                }

                // 2. Office -> MS Office Viewer
                if (new[] { ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt" }.Contains(extension))
                {
                    return $"https://view.officeapps.live.com/op/view.aspx?src={encodedUrl}";
                }

                // 3. PDF/Khác -> Google Docs Viewer
                return $"https://docs.google.com/viewer?url={encodedUrl}&embedded=true";
            }
            catch
            {
                return null;
            }
        }
    }
}