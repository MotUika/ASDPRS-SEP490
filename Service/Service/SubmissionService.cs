using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repository.IRepository;
using Service.Interface;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Submission;
using Service.RequestAndResponse.Response.Submission;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class SubmissionService : ISubmissionService
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IReviewAssignmentRepository _reviewAssignmentRepository;
        private readonly IAISummaryRepository _aiSummaryRepository;
        private readonly IRegradeRequestRepository _regradeRequestRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SubmissionService> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly ASDPRSContext _context;
        private readonly IReviewAssignmentService _reviewAssignmentService;

        public SubmissionService(
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            IAISummaryRepository aiSummaryRepository,
            IRegradeRequestRepository regradeRequestRepository,
            IMapper mapper,
            ILogger<SubmissionService> logger,
            IFileStorageService fileStorageService,
            ASDPRSContext context,
            IReviewAssignmentService reviewAssignmentService)
        {
            _submissionRepository = submissionRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _aiSummaryRepository = aiSummaryRepository;
            _regradeRequestRepository = regradeRequestRepository;
            _mapper = mapper;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _context = context;
            _reviewAssignmentService = reviewAssignmentService;
        }


        public async Task<BaseResponse<SubmissionResponse>> CreateSubmissionAsync(CreateSubmissionRequest request)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<SubmissionResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                    return new BaseResponse<SubmissionResponse>("User not found", StatusCodeEnum.NotFound_404, null);

                var existingSubmission = await _submissionRepository.GetAllAsync();
                if (existingSubmission.Any(s => s.AssignmentId == request.AssignmentId && s.UserId == request.UserId))
                {
                    return new BaseResponse<SubmissionResponse>("User has already submitted for this assignment", StatusCodeEnum.Conflict_409, null);
                }

                // Upload file to Supabase (via IFileStorageService)
                var uploadResult = await _fileStorageService.UploadFileAsync(request.File, folder: $"submissions/{request.AssignmentId}/{request.UserId}", makePublic: request.IsPublic);
                if (!uploadResult.Success)
                {
                    return new BaseResponse<SubmissionResponse>(uploadResult.ErrorMessage, StatusCodeEnum.BadRequest_400, null);
                }

                var submission = new Submission
                {
                    AssignmentId = request.AssignmentId,
                    UserId = request.UserId,
                    FileUrl = uploadResult.FileUrl,        // URL to serve (public or signed)
                    FileName = uploadResult.FileName,      // object path in bucket (useful to delete)
                    OriginalFileName = request.File.FileName,
                    Keywords = request.Keywords,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Submitted",
                    IsPublic = request.IsPublic
                };

                var createdSubmission = await _submissionRepository.AddAsync(submission);
                var response = await MapToSubmissionResponse(createdSubmission);

                _logger.LogInformation($"Submission created successfully. SubmissionId: {createdSubmission.SubmissionId}");
                await AutoAssignReviewsForNewSubmission(request.AssignmentId);


                // Late check
                if (submission.SubmittedAt > assignment.Deadline && submission.SubmittedAt <= (assignment.FinalDeadline ?? DateTime.MaxValue))
                {
                    submission.Status = "Late";
                    await _submissionRepository.UpdateAsync(submission);
                }

                return new BaseResponse<SubmissionResponse>("Submission created successfully", StatusCodeEnum.Created_201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating submission");
                return new BaseResponse<SubmissionResponse>("An error occurred while creating the submission", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        private async Task AutoAssignReviewsForNewSubmission(int assignmentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null) return;

                // Sử dụng NumPeerReviewsRequired mà instructor đã set
                await _reviewAssignmentService.AssignPeerReviewsAutomaticallyAsync(
                    assignment.AssignmentId,
                    assignment.NumPeerReviewsRequired);

                _logger.LogInformation($"Auto-assigned {assignment.NumPeerReviewsRequired} reviews for new submission in assignment {assignment.AssignmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto review assignment for new submission");
            }
        }
        public async Task<BaseResponse<SubmissionResponse>> SubmitAssignmentAsync(SubmitAssignmentRequest request)
        {
            var createRequest = new CreateSubmissionRequest
            {
                AssignmentId = request.AssignmentId,
                UserId = request.UserId,
                File = request.File,
                Keywords = request.Keywords,
                IsPublic = request.IsPublic
            };

            return await CreateSubmissionAsync(createRequest);
        }
        public async Task<BaseResponse<bool>> ExtendStudentDeadlineAsync(int submissionId, DateTime newDeadline)
        {
            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission == null) return new BaseResponse<bool>("Not found", StatusCodeEnum.NotFound_404, false);

            // Perhaps add ExtensionDeadline field? But no DB change: Use SystemConfig "ExtensionDeadline_SubmissionId"
            await SaveSubmissionConfig(submission.SubmissionId, "ExtensionDeadline", newDeadline.ToString("o"));

            return new BaseResponse<bool>("Extended", StatusCodeEnum.OK_200, true);
        }
        private async Task SaveSubmissionConfig(int submissionId, string key, string value)
        {
            var configKey = $"{key}_{submissionId}";
            var config = await _context.SystemConfigs.FirstOrDefaultAsync(sc => sc.ConfigKey == configKey);

            if (config == null)
            {
                config = new SystemConfig
                {
                    ConfigKey = configKey,
                    ConfigValue = value,
                    Description = "Submission config",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = 1  // Assume, or pass
                };
                _context.SystemConfigs.Add(config);
            }
            else
            {
                config.ConfigValue = value;
                config.UpdatedAt = DateTime.UtcNow;
                // UpdatedBy
            }

            await _context.SaveChangesAsync();
        }

        public async Task<BaseResponse<SubmissionResponse>> GetSubmissionByIdAsync(GetSubmissionByIdRequest request)
        {
            try
            {
                Submission submission;

                if (request.IncludeReviews)
                {
                    submission = await _submissionRepository.GetSubmissionWithReviewsAsync(request.SubmissionId);
                }
                else
                {
                    submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                }

                if (submission == null)
                {
                    return new BaseResponse<SubmissionResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToSubmissionResponse(submission, request.IncludeReviews, request.IncludeAISummaries);

                return new BaseResponse<SubmissionResponse>(
                    "Submission retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving submission with ID: {request.SubmissionId}");
                return new BaseResponse<SubmissionResponse>(
                    "An error occurred while retrieving the submission",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByFilterAsync(GetSubmissionsByFilterRequest request)
        {
            try
            {
                IEnumerable<Submission> submissions;

                if (request.AssignmentId.HasValue)
                {
                    submissions = await _submissionRepository.GetByAssignmentIdAsync(request.AssignmentId.Value);
                }
                else if (request.UserId.HasValue)
                {
                    submissions = await _submissionRepository.GetByUserIdAsync(request.UserId.Value);
                }
                else
                {
                    submissions = await _submissionRepository.GetAllAsync();
                }

                // Apply additional filters
                submissions = ApplyFilters(submissions, request);

                // Apply sorting
                submissions = ApplySorting(submissions, request.SortBy, request.SortDescending);

                // Apply pagination
                var totalCount = submissions.Count();
                var pagedSubmissions = submissions
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var responseList = new List<SubmissionResponse>();
                foreach (var submission in pagedSubmissions)
                {
                    responseList.Add(await MapToSubmissionResponse(submission));
                }

                var response = new SubmissionListResponse
                {
                    Submissions = responseList,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                };

                return new BaseResponse<SubmissionListResponse>(
                    "Submissions retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submissions by filter");
                return new BaseResponse<SubmissionListResponse>(
                    "An error occurred while retrieving submissions",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<SubmissionResponse>> UpdateSubmissionAsync(UpdateSubmissionRequest request)
        {
            try
            {
                var existingSubmission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (existingSubmission == null)
                {
                    return new BaseResponse<SubmissionResponse>("Submission not found", StatusCodeEnum.NotFound_404, null);
                }
                // Check if student can modify submission
                var canModify = await CanStudentModifySubmissionAsync(request.SubmissionId, existingSubmission.UserId);
                if (!canModify.Data)
                {
                    return new BaseResponse<SubmissionResponse>(
                        "Cannot modify submission after deadline",
                        StatusCodeEnum.Forbidden_403,
                        null);
                }
                // Update file if provided
                if (request.File != null)
                {
                    // Upload new file
                    var uploadResult = await _fileStorageService.UploadFileAsync(request.File, folder: $"submissions/{existingSubmission.AssignmentId}/{existingSubmission.UserId}", makePublic: request.IsPublic ?? existingSubmission.IsPublic);
                    if (!uploadResult.Success)
                    {
                        return new BaseResponse<SubmissionResponse>(uploadResult.ErrorMessage, StatusCodeEnum.BadRequest_400, null);
                    }

                    // Delete old file (prefer stored path but allow URL)
                    var toDelete = !string.IsNullOrEmpty(existingSubmission.FileName) ? existingSubmission.FileName : existingSubmission.FileUrl;
                    await _fileStorageService.DeleteFileAsync(toDelete);

                    existingSubmission.FileUrl = uploadResult.FileUrl;
                    existingSubmission.FileName = uploadResult.FileName;
                    existingSubmission.OriginalFileName = request.File.FileName;
                }

                // Update other properties
                if (!string.IsNullOrEmpty(request.Keywords))
                    existingSubmission.Keywords = request.Keywords;

                if (request.IsPublic.HasValue)
                    existingSubmission.IsPublic = request.IsPublic.Value;

                if (!string.IsNullOrEmpty(request.Status))
                    existingSubmission.Status = request.Status;

                var updatedSubmission = await _submissionRepository.UpdateAsync(existingSubmission);
                var response = await MapToSubmissionResponse(updatedSubmission);

                _logger.LogInformation($"Submission updated successfully. SubmissionId: {updatedSubmission.SubmissionId}");

                return new BaseResponse<SubmissionResponse>("Submission updated successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating submission with ID: {request.SubmissionId}");
                return new BaseResponse<SubmissionResponse>("An error occurred while updating the submission", StatusCodeEnum.InternalServerError_500, null);
            }
        }
        public async Task<BaseResponse<SubmissionResponse>> UpdateSubmissionStatusAsync(UpdateSubmissionStatusRequest request)
        {
            try
            {
                var existingSubmission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (existingSubmission == null)
                {
                    return new BaseResponse<SubmissionResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                existingSubmission.Status = request.Status;

                var updatedSubmission = await _submissionRepository.UpdateAsync(existingSubmission);
                var response = await MapToSubmissionResponse(updatedSubmission);

                _logger.LogInformation($"Submission status updated successfully. SubmissionId: {updatedSubmission.SubmissionId}, Status: {request.Status}");

                return new BaseResponse<SubmissionResponse>(
                    "Submission status updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating submission status for ID: {request.SubmissionId}");
                return new BaseResponse<SubmissionResponse>(
                    "An error occurred while updating the submission status",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteSubmissionAsync(int submissionId)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(submissionId);
                if (submission == null)
                {
                    return new BaseResponse<bool>("Submission not found", StatusCodeEnum.NotFound_404, false);
                }
                // Check if student can modify submission
                var canModify = await CanStudentModifySubmissionAsync(submissionId, submission.UserId);
                if (!canModify.Data)
                {
                    return new BaseResponse<bool>(
                        "Cannot delete submission after deadline",
                        StatusCodeEnum.Forbidden_403,
                        false);
                }

                // Delete associated file (prefer stored object path)
                var toDelete = !string.IsNullOrEmpty(submission.FileName) ? submission.FileName : submission.FileUrl;
                await _fileStorageService.DeleteFileAsync(toDelete);

                await _submissionRepository.DeleteAsync(submission);

                _logger.LogInformation($"Submission deleted successfully. SubmissionId: {submissionId}");

                return new BaseResponse<bool>("Submission deleted successfully", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting submission with ID: {submissionId}");
                return new BaseResponse<bool>("An error occurred while deleting the submission", StatusCodeEnum.InternalServerError_500, false);
            }
        }

        public async Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByAssignmentIdAsync(int assignmentId, int pageNumber = 1, int pageSize = 20)
        {
            var filterRequest = new GetSubmissionsByFilterRequest
            {
                AssignmentId = assignmentId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetSubmissionsByFilterAsync(filterRequest);
        }

        public async Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            var filterRequest = new GetSubmissionsByFilterRequest
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetSubmissionsByFilterAsync(filterRequest);
        }

        public async Task<BaseResponse<SubmissionStatisticsResponse>> GetSubmissionStatisticsAsync(int assignmentId)
        {
            try
            {
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);

                if (assignment == null)
                {
                    return new BaseResponse<SubmissionStatisticsResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var statistics = new SubmissionStatisticsResponse
                {
                    AssignmentId = assignmentId,
                    AssignmentTitle = assignment.Title,
                    TotalSubmissions = submissions.Count(),
                    PendingSubmissions = submissions.Count(s => s.Status == "Submitted"),
                    GradedSubmissions = submissions.Count(s => s.Status == "Graded"),
                    LateSubmissions = submissions.Count(s => s.SubmittedAt > assignment.Deadline),
                    StatusDistribution = submissions.GroupBy(s => s.Status)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    TopKeywords = ExtractTopKeywords(submissions)
                };

                return new BaseResponse<SubmissionStatisticsResponse>(
                    "Submission statistics retrieved successfully",
                    StatusCodeEnum.OK_200,
                    statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving submission statistics for assignment ID: {assignmentId}");
                return new BaseResponse<SubmissionStatisticsResponse>(
                    "An error occurred while retrieving submission statistics",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> CheckSubmissionExistsAsync(int assignmentId, int userId)
        {
            try
            {
                var submissions = await _submissionRepository.GetAllAsync();
                var exists = submissions.Any(s => s.AssignmentId == assignmentId && s.UserId == userId);

                return new BaseResponse<bool>(
                    exists ? "Submission exists" : "No submission found",
                    StatusCodeEnum.OK_200,
                    exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking submission existence for assignment {assignmentId} and user {userId}");
                return new BaseResponse<bool>(
                    "An error occurred while checking submission existence",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<SubmissionResponse>> GetSubmissionWithDetailsAsync(int submissionId)
        {
            var request = new GetSubmissionByIdRequest
            {
                SubmissionId = submissionId,
                IncludeReviews = true,
                IncludeAISummaries = true
            };

            return await GetSubmissionByIdAsync(request);
        }

        private IEnumerable<Submission> ApplyFilters(IEnumerable<Submission> submissions, GetSubmissionsByFilterRequest request)
        {
            if (!string.IsNullOrEmpty(request.Status))
            {
                submissions = submissions.Where(s => s.Status == request.Status);
            }

            if (!string.IsNullOrEmpty(request.Keywords))
            {
                submissions = submissions.Where(s => s.Keywords != null &&
                    s.Keywords.Contains(request.Keywords, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsPublic.HasValue)
            {
                submissions = submissions.Where(s => s.IsPublic == request.IsPublic.Value);
            }

            if (request.StartDate.HasValue)
            {
                submissions = submissions.Where(s => s.SubmittedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                submissions = submissions.Where(s => s.SubmittedAt <= request.EndDate.Value);
            }

            return submissions;
        }

        private IEnumerable<Submission> ApplySorting(IEnumerable<Submission> submissions, string sortBy, bool sortDescending)
        {
            return sortBy?.ToLower() switch
            {
                "submittedat" => sortDescending ?
                    submissions.OrderByDescending(s => s.SubmittedAt) :
                    submissions.OrderBy(s => s.SubmittedAt),
                "filename" => sortDescending ?
                    submissions.OrderByDescending(s => s.FileName) :
                    submissions.OrderBy(s => s.FileName),
                "status" => sortDescending ?
                    submissions.OrderByDescending(s => s.Status) :
                    submissions.OrderBy(s => s.Status),
                _ => sortDescending ?
                    submissions.OrderByDescending(s => s.SubmittedAt) :
                    submissions.OrderBy(s => s.SubmittedAt)
            };
        }

        private List<KeywordFrequencyResponse> ExtractTopKeywords(IEnumerable<Submission> submissions, int topCount = 10)
        {
            var keywordFrequencies = new Dictionary<string, int>();

            foreach (var submission in submissions.Where(s => !string.IsNullOrEmpty(s.Keywords)))
            {
                var keywords = submission.Keywords.Split(',', ';', ' ')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k));

                foreach (var keyword in keywords)
                {
                    if (keywordFrequencies.ContainsKey(keyword))
                    {
                        keywordFrequencies[keyword]++;
                    }
                    else
                    {
                        keywordFrequencies[keyword] = 1;
                    }
                }
            }

            return keywordFrequencies
                .OrderByDescending(kv => kv.Value)
                .Take(topCount)
                .Select(kv => new KeywordFrequencyResponse { Keyword = kv.Key, Frequency = kv.Value })
                .ToList();
        }

        private async Task<SubmissionResponse> MapToSubmissionResponse(Submission submission, bool includeReviews = false, bool includeAISummaries = false)
        {
            var response = _mapper.Map<SubmissionResponse>(submission);

            // Load assignment info
            if (submission.Assignment != null)
            {
                response.Assignment = _mapper.Map<AssignmentInfoResponse>(submission.Assignment);
            }
            else
            {
                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                response.Assignment = _mapper.Map<AssignmentInfoResponse>(assignment);
            }

            // Load user info
            if (submission.User != null)
            {
                response.User = _mapper.Map<UserInfoResponse>(submission.User);
            }
            else
            {
                var user = await _userRepository.GetByIdAsync(submission.UserId);
                response.User = _mapper.Map<UserInfoResponse>(user);
            }

            // Load reviews if requested
            if (includeReviews && submission.ReviewAssignments != null)
            {
                response.ReviewAssignments = _mapper.Map<List<SubmissionReviewAssignmentResponse>>(submission.ReviewAssignments);
            }

            // Load AI summaries if requested
            if (includeAISummaries)
            {
                var aiSummaries = await _aiSummaryRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                response.AISummaries = _mapper.Map<List<AISummaryResponse>>(aiSummaries);
            }

            return response;
        }

        public async Task<BaseResponse<bool>> CanStudentModifySubmissionAsync(int submissionId, int studentId)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(submissionId);
                if (submission == null || submission.UserId != studentId)
                    return new BaseResponse<bool>("Access denied", StatusCodeEnum.Forbidden_403, false);

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                var now = DateTime.UtcNow;

                // Cho phép sửa/xóa trước deadline
                bool canModify = now <= assignment.Deadline;

                return new BaseResponse<bool>(canModify ? "Can modify" : "Cannot modify after deadline",
                                            StatusCodeEnum.OK_200, canModify);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error checking submission modification: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }
    }
}