using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repository.IRepository;
using Repository.Repository;
using Service.Interface;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Submission;
using Service.RequestAndResponse.Response.AISummary;
using Service.RequestAndResponse.Response.Submission;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly ISystemConfigService _systemConfigService;
        private readonly IDocumentTextExtractor _documentTextExtractor;

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
            IReviewAssignmentService reviewAssignmentService, ISystemConfigService systemConfigService,
            IDocumentTextExtractor documentTextExtractor)
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
            _systemConfigService = systemConfigService;
            _documentTextExtractor = documentTextExtractor;
        }


        public async Task<BaseResponse<SubmissionResponse>> CreateSubmissionAsync(CreateSubmissionRequest request)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<SubmissionResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);


                if (assignment.Status != AssignmentStatusEnum.Active.ToString())
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment. Assignment status is: {assignment.Status}. Only Active assignments can be submitted.",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                    return new BaseResponse<SubmissionResponse>("User not found", StatusCodeEnum.NotFound_404, null);

                var now = DateTime.UtcNow;
                if (assignment.StartDate.HasValue && now < assignment.StartDate.Value)
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment before start date: {assignment.StartDate.Value}",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                if (now > (assignment.FinalDeadline ?? assignment.Deadline))
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment after final deadline: {assignment.FinalDeadline ?? assignment.Deadline}",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var existingSubmission = await _submissionRepository.GetAllAsync();
                if (existingSubmission.Any(s => s.AssignmentId == request.AssignmentId && s.UserId == request.UserId))
                {
                    return new BaseResponse<SubmissionResponse>("User has already submitted for this assignment", StatusCodeEnum.Conflict_409, null);
                }

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

        public async Task<BaseResponse<SubmissionResponse>> CreateSubmissionWithCheckAsync(CreateSubmissionRequest request)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<SubmissionResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);


                if (assignment.Status != AssignmentStatusEnum.Active.ToString())
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment. Assignment status is: {assignment.Status}. Only Active assignments can be submitted.",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                    return new BaseResponse<SubmissionResponse>("User not found", StatusCodeEnum.NotFound_404, null);

                var now = DateTime.UtcNow;
                if (assignment.StartDate.HasValue && now < assignment.StartDate.Value)
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment before start date: {assignment.StartDate.Value}",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                if (now > (assignment.FinalDeadline ?? assignment.Deadline))
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment after final deadline: {assignment.FinalDeadline ?? assignment.Deadline}",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var existingSubmission = await _submissionRepository.GetAllAsync();
                if (existingSubmission.Any(s => s.AssignmentId == request.AssignmentId && s.UserId == request.UserId))
                {
                    return new BaseResponse<SubmissionResponse>("User has already submitted for this assignment", StatusCodeEnum.Conflict_409, null);
                }

                // Kiểm tra trùng lặp trước khi upload
                var plagiarismCheck = await CheckPlagiarismAsync(request.AssignmentId, request.File);
                if (plagiarismCheck.StatusCode != StatusCodeEnum.OK_200)
                {
                    return new BaseResponse<SubmissionResponse>(plagiarismCheck.Message, plagiarismCheck.StatusCode, null);
                }

                var uploadResult = await _fileStorageService.UploadFileAsync(request.File, folder: $"submissions/{request.AssignmentId}/{request.UserId}", makePublic: request.IsPublic);
                if (!uploadResult.Success)
                {
                    return new BaseResponse<SubmissionResponse>(uploadResult.ErrorMessage, StatusCodeEnum.BadRequest_400, null);
                }

                var submission = new Submission
                {
                    AssignmentId = request.AssignmentId,
                    UserId = request.UserId,
                    FileUrl = uploadResult.FileUrl,        
                    FileName = uploadResult.FileName,      
                    OriginalFileName = request.File.FileName,
                    Keywords = request.Keywords,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Submitted",
                    IsPublic = request.IsPublic
                };

                var createdSubmission = await _submissionRepository.AddAsync(submission);
                var response = await MapToSubmissionResponse(createdSubmission);

                _logger.LogInformation($"Submission created successfully. SubmissionId: {createdSubmission.SubmissionId}");

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
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<SubmissionResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);

                if (assignment.Status != AssignmentStatusEnum.Active.ToString())
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment. Assignment status is: {assignment.Status}",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting assignment");
                return new BaseResponse<SubmissionResponse>("An error occurred while submitting the assignment", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<SubmissionResponse>> SubmitAssignmentWithCheckAsync(SubmitAssignmentRequest request)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<SubmissionResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);

                if (assignment.Status != AssignmentStatusEnum.Active.ToString())
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment. Assignment status is: {assignment.Status}",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var createRequest = new CreateSubmissionRequest
                {
                    AssignmentId = request.AssignmentId,
                    UserId = request.UserId,
                    File = request.File,
                    Keywords = request.Keywords,
                    IsPublic = request.IsPublic
                };

                return await CreateSubmissionWithCheckAsync(createRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting assignment");
                return new BaseResponse<SubmissionResponse>("An error occurred while submitting the assignment", StatusCodeEnum.InternalServerError_500, null);
            }
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
        public async Task<BaseResponse<SubmissionResponse>> UpdateSubmissionWithCheckAsync(UpdateSubmissionRequest request)
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

                // Kiểm tra trùng lặp nếu có file mới
                if (request.File != null)
                {
                    var plagiarismCheck = await CheckPlagiarismAsync(existingSubmission.AssignmentId, request.File, request.SubmissionId);
                    if (plagiarismCheck.StatusCode != StatusCodeEnum.OK_200)
                    {
                        return new BaseResponse<SubmissionResponse>(plagiarismCheck.Message, plagiarismCheck.StatusCode, null);
                    }
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


        public async Task<BaseResponse<SubmissionListResponse>> GetSubmissionsAllStudentByAssignmentIdAsync(int assignmentId)
        {
            try
            {
                // 1. Lấy Assignment
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<SubmissionListResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var courseInstanceId = assignment.CourseInstanceId;

                // 2. Lấy tất cả sinh viên trong lớp (CourseInstance)
                var enrolledStudents = await _context.CourseStudents
                    .Where(cs => cs.CourseInstanceId == courseInstanceId)
                    .Include(cs => cs.User)
                    .Select(cs => cs.User)
                    .ToListAsync();

                if (!enrolledStudents.Any())
                {
                    return new BaseResponse<SubmissionListResponse>(
                        "No students enrolled in this class",
                        StatusCodeEnum.NoContent_204,
                        new SubmissionListResponse
                        {
                            Submissions = new List<SubmissionResponse>(),
                            TotalCount = 0,
                            TotalStudents = 0
                        });
                }

                // 3. Lấy submissions
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);

                // 4. Tạo danh sách đầy đủ
                var responseList = new List<SubmissionResponse>();

                foreach (var student in enrolledStudents)
                {
                    var submission = submissions.FirstOrDefault(s => s.UserId == student.Id);

                    if (submission != null)
                    {
                        responseList.Add(await MapToSubmissionResponse(submission));
                    }
                    else
                    {
                        var assignmentInfo = _mapper.Map<AssignmentInfoResponse>(assignment);
                        var userInfo = _mapper.Map<UserInfoResponse>(student);

                        responseList.Add(new SubmissionResponse
                        {
                            SubmissionId = 0,
                            AssignmentId = assignmentId,
                            UserId = student.Id,
                            StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                            StudentCode = student.StudentCode,
                            CourseName = assignment.CourseInstance?.Course?.CourseName,
                            ClassName = assignment.CourseInstance?.SectionCode,
                            FileUrl = null,
                            FileName = null,
                            OriginalFileName = null,
                            Keywords = null,
                            SubmittedAt = DateTime.MinValue,
                            Status = "Not Submitted",
                            IsPublic = false,
                            InstructorScore = null,
                            PeerAverageScore = null,
                            FinalScore = null,
                            Feedback = null,
                            GradedAt = null,
                            Assignment = assignmentInfo,
                            User = userInfo,
                            ReviewAssignments = new List<SubmissionReviewAssignmentResponse>(),
                            AISummaries = new List<AISummaryResponse>(),
                            RegradeRequests = new List<RegradeRequestSubmissionResponse>()
                        });
                    }
                }

                // 5. Sắp xếp
                responseList = responseList
                    .OrderBy(x => x.StudentName)
                    .ThenBy(x => x.StudentCode)
                    .ToList();

                // 6. Tạo response
                var response = new SubmissionListResponse
                {
                    Submissions = responseList,
                    TotalCount = responseList.Count,
                    TotalStudents = enrolledStudents.Count
                };

                return new BaseResponse<SubmissionListResponse>(
                    "Full student submission list retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetSubmissionsByAssignmentIdAsync for assignment {assignmentId}");
                return new BaseResponse<SubmissionListResponse>(
                    "An error occurred while retrieving the submission list",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
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
                response.StudentName = $"{submission.User.FirstName} {submission.User.LastName}".Trim();
                response.StudentCode = submission.User.StudentCode;

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

            response.CourseName = submission.Assignment?.CourseInstance?.Course?.CourseName;
            response.ClassName = submission.Assignment?.CourseInstance?.SectionCode;

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

                if (assignment.Status != AssignmentStatusEnum.Active.ToString())
                {
                    return new BaseResponse<bool>(
                        $"Cannot modify submission. Assignment status is: {assignment.Status}",
                        StatusCodeEnum.Forbidden_403,
                        false);
                }

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
        public async Task<BaseResponse<SubmissionResponse>> GetSubmissionByAssignmentAndUserAsync(int assignmentId, int userId)
        {
            try
            {
                var submission = await _submissionRepository.GetByAssignmentAndUserAsync(assignmentId, userId);
                if (submission == null)
                {
                    return new BaseResponse<SubmissionResponse>(
                        "Submission not found for this assignment and user",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToSubmissionResponse(submission);
                return new BaseResponse<SubmissionResponse>(
                    "Submission retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving submission for assignment {assignmentId} and user {userId}");
                return new BaseResponse<SubmissionResponse>(
                    "An error occurred while retrieving the submission",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<SubmissionResponse>>> GetSubmissionsByCourseInstanceAndUserAsync(int courseInstanceId, int userId)
        {
            try
            {
                var submissions = await _submissionRepository.GetByCourseInstanceAndUserAsync(courseInstanceId, userId);
                var responses = new List<SubmissionResponse>();

                foreach (var submission in submissions)
                {
                    responses.Add(await MapToSubmissionResponse(submission));
                }

                return new BaseResponse<List<SubmissionResponse>>(
                    "Submissions retrieved successfully",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving submissions for course instance {courseInstanceId} and user {userId}");
                return new BaseResponse<List<SubmissionResponse>>(
                    "An error occurred while retrieving submissions",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<SubmissionResponse>>> GetSubmissionsByUserAndSemesterAsync(int userId, int semesterId)
        {
            try
            {
                var submissions = await _submissionRepository.GetByUserAndSemesterAsync(userId, semesterId);
                var responses = new List<SubmissionResponse>();

                foreach (var submission in submissions)
                {
                    responses.Add(await MapToSubmissionResponse(submission));
                }

                return new BaseResponse<List<SubmissionResponse>>(
                    "Submissions retrieved successfully",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving submissions for user {userId} and semester {semesterId}");
                return new BaseResponse<List<SubmissionResponse>>(
                    "An error occurred while retrieving submissions",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<GradeSubmissionResponse>> GradeSubmissionAsync(GradeSubmissionRequest request)
        {
            try
            {
                // 1️⃣ Kiểm tra submission
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                    return new BaseResponse<GradeSubmissionResponse>("Submission not found", StatusCodeEnum.NotFound_404, null);

                // 2️⃣ Kiểm tra assignment
                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<GradeSubmissionResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);

                decimal instructorScore = 0;

                // 3️⃣ Nếu giảng viên chấm theo từng tiêu chí (rubric)
                if (request.CriteriaFeedbacks != null && request.CriteriaFeedbacks.Any())
                {
                    // Xóa feedback cũ của instructor (nếu có)
                    var oldInstructorReviews = await _context.ReviewAssignments
                        .Where(ra => ra.SubmissionId == submission.SubmissionId)
                        .SelectMany(ra => ra.Reviews)
                        .Where(r => r.ReviewType == "Instructor")
                        .ToListAsync();

                    foreach (var oldReview in oldInstructorReviews)
                    {
                        var oldFeedbacks = await _context.CriteriaFeedbacks
                            .Where(cf => cf.ReviewId == oldReview.ReviewId)
                            .ToListAsync();

                        _context.CriteriaFeedbacks.RemoveRange(oldFeedbacks);
                        _context.Reviews.Remove(oldReview);
                    }

                    // Tạo ReviewAssignment mới cho instructor nếu chưa có
                    var instructorReviewAssignment = await _context.ReviewAssignments
                        .FirstOrDefaultAsync(ra => ra.SubmissionId == submission.SubmissionId && ra.ReviewerUserId == request.InstructorId && ra.IsAIReview == false);

                    if (instructorReviewAssignment == null)
                    {
                        instructorReviewAssignment = new ReviewAssignment
                        {
                            SubmissionId = submission.SubmissionId,
                            ReviewerUserId = request.InstructorId,
                            AssignedAt = DateTime.UtcNow,
                            Deadline = DateTime.UtcNow.AddDays(7),
                            Status = "Completed",
                            IsAIReview = false
                        };
                        _context.ReviewAssignments.Add(instructorReviewAssignment);
                        await _context.SaveChangesAsync();
                    }

                    // Tạo review mới
                    var review = new Review
                    {
                        ReviewAssignmentId = instructorReviewAssignment.ReviewAssignmentId,
                        OverallScore = 0,
                        GeneralFeedback = request.Feedback,
                        ReviewedAt = DateTime.UtcNow,
                        ReviewType = "Instructor",
                        FeedbackSource = "Instructor"
                    };
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();

                    // Tính tổng điểm có trọng số
                    decimal totalScore = 0;
                    decimal totalWeight = 0;

                    foreach (var cf in request.CriteriaFeedbacks)
                    {
                        var criteria = await _context.Criteria.FirstOrDefaultAsync(c => c.CriteriaId == cf.CriteriaId);
                        if (criteria == null) continue;

                        var weight = criteria.Weight > 0 ? criteria.Weight : 1;
                        totalScore += (cf.Score ?? 0) * weight;
                        totalWeight += weight;

                        var criteriaFeedback = new CriteriaFeedback
                        {
                            ReviewId = review.ReviewId,
                            CriteriaId = cf.CriteriaId,
                            ScoreAwarded = cf.Score,
                            Feedback = cf.Feedback,
                            FeedbackSource = "Instructor"
                        };
                        _context.CriteriaFeedbacks.Add(criteriaFeedback);
                    }

                    instructorScore = totalWeight > 0 ? Math.Round(totalScore / totalWeight, 2) : 0;
                    review.OverallScore = instructorScore;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Nếu không chấm theo tiêu chí → chỉ lưu feedback tổng quát
                    var reviewAssignment = await _context.ReviewAssignments
                        .FirstOrDefaultAsync(ra => ra.SubmissionId == submission.SubmissionId && ra.ReviewerUserId == request.InstructorId);

                    if (reviewAssignment == null)
                    {
                        reviewAssignment = new ReviewAssignment
                        {
                            SubmissionId = submission.SubmissionId,
                            ReviewerUserId = request.InstructorId,
                            AssignedAt = DateTime.UtcNow,
                            Deadline = DateTime.UtcNow.AddDays(7),
                            Status = "Completed",
                            IsAIReview = false
                        };
                        _context.ReviewAssignments.Add(reviewAssignment);
                        await _context.SaveChangesAsync();
                    }

                    var review = new Review
                    {
                        ReviewAssignmentId = reviewAssignment.ReviewAssignmentId,
                        OverallScore = null,
                        GeneralFeedback = request.Feedback,
                        ReviewedAt = DateTime.UtcNow,
                        ReviewType = "Instructor",
                        FeedbackSource = "Instructor"
                    };
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();
                }

                // 4️⃣ Lấy điểm trung bình từ peer review
                var peerAverage = await _reviewAssignmentRepository.GetPeerAverageScoreBySubmissionIdAsync(submission.SubmissionId);
                var peerAvg = peerAverage ?? 0;

                // 5️⃣ Chuẩn hóa trọng số
                var instructorWeight = assignment.InstructorWeight;
                var peerWeight = assignment.PeerWeight;
                if (instructorWeight == 0 && peerWeight == 0)
                {
                    instructorWeight = 50.0m;
                    peerWeight = 50.0m;
                }
                if (instructorWeight + peerWeight != 100)
                {
                    var total = instructorWeight + peerWeight;
                    instructorWeight = (instructorWeight / total) * 100;
                    peerWeight = (peerWeight / total) * 100;
                }

                // 6️⃣ Tính điểm cuối cùng
                var finalScore = Math.Round(
                    (instructorScore * instructorWeight / 100) + (peerAvg * peerWeight / 100), 2);

                // 7️⃣ Cập nhật submission
                submission.InstructorScore = instructorScore;
                submission.PeerAverageScore = peerAvg;
                submission.FinalScore = finalScore;
                submission.Feedback = request.Feedback ?? submission.Feedback;
                submission.GradedAt = DateTime.UtcNow;
                submission.Status = "Graded";
                if (request.PublishImmediately)
                    submission.IsPublic = true;

                await _submissionRepository.UpdateAsync(submission);

                // 8️⃣ Chuẩn bị response
                var response = new GradeSubmissionResponse
                {
                    SubmissionId = submission.SubmissionId,
                    AssignmentId = submission.AssignmentId,
                    UserId = submission.UserId,
                    InstructorScore = submission.InstructorScore,
                    PeerAverageScore = submission.PeerAverageScore,
                    FinalScore = submission.FinalScore,
                    Feedback = submission.Feedback,
                    GradedAt = submission.GradedAt,
                    FileUrl = submission.FileUrl,
                    FileName = submission.FileName,
                    Status = submission.Status,
                    StudentName = submission.User?.UserName,
                    CourseName = submission.Assignment?.CourseInstance?.Course?.CourseName,
                    AssignmentTitle = submission.Assignment?.Title
                };

                _logger.LogInformation($"✅ Submission {submission.SubmissionId} graded successfully. FinalScore: {finalScore}");

                return new BaseResponse<GradeSubmissionResponse>(
                    "Submission graded successfully",
                    StatusCodeEnum.OK_200,
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error grading submission");
                return new BaseResponse<GradeSubmissionResponse>(
                    "An error occurred while grading submission",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }



        private async Task<BaseResponse<bool>> CheckPlagiarismAsync(int assignmentId, IFormFile file, int? excludeSubmissionId = null)
        {
            if (file == null)
            {
                return new BaseResponse<bool>("No file provided for check", StatusCodeEnum.OK_200, true);
            }

            try
            {
                // Get threshold from config, default 80%
                var thresholdStr = await _systemConfigService.GetSystemConfigAsync("PlagiarismThreshold") ?? "80";
                double threshold = double.Parse(thresholdStr) / 100;

                // Extract text from new file
                using var newStream = file.OpenReadStream();
                string newText = await _documentTextExtractor.ExtractTextAsync(newStream, file.FileName);

                if (string.IsNullOrWhiteSpace(newText))
                {
                    return new BaseResponse<bool>("No text extracted from file", StatusCodeEnum.BadRequest_400, false);
                }

                // Get other submissions in same assignment
                var otherSubmissions = (await _submissionRepository.GetByAssignmentIdAsync(assignmentId))
                    .Where(s => s.SubmissionId != excludeSubmissionId)
                    .ToList();

                if (!otherSubmissions.Any())
                {
                    return new BaseResponse<bool>("No other submissions to compare", StatusCodeEnum.OK_200, true);
                }

                double maxSimilarity = 0;

                foreach (var other in otherSubmissions)
                {
                    using var otherStream = await _fileStorageService.GetFileStreamAsync(other.FileUrl);
                    if (otherStream == null) continue;

                    string otherText = await _documentTextExtractor.ExtractTextAsync(otherStream, other.FileName);
                    if (string.IsNullOrWhiteSpace(otherText)) continue;

                    double sim = CosineSimilarity(newText, otherText);
                    if (sim > maxSimilarity) maxSimilarity = sim;
                }

                if (maxSimilarity > threshold)
                {
                    return new BaseResponse<bool>(
                        $"Submission too similar to existing one ({Math.Round(maxSimilarity * 100, 2)}% similarity). Threshold is {threshold * 100}%.",
                        StatusCodeEnum.BadRequest_400,
                        false);
                }

                return new BaseResponse<bool>("Plagiarism check passed", StatusCodeEnum.OK_200, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during plagiarism check");
                return new BaseResponse<bool>(
                    $"Error checking plagiarism: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        // method cho check plagiarism chủ động (trả details)
        public async Task<BaseResponse<PlagiarismCheckResponse>> CheckPlagiarismActiveAsync(int assignmentId, IFormFile file, int? excludeSubmissionId = null)
        {
            if (file == null)
            {
                return new BaseResponse<PlagiarismCheckResponse>("No file provided for check", StatusCodeEnum.BadRequest_400, null);
            }

            try
            {
                // Get threshold from config, default 80%
                var thresholdStr = await _systemConfigService.GetSystemConfigAsync("PlagiarismThreshold") ?? "80";
                double threshold = double.Parse(thresholdStr) / 100;

                // Extract text from new file
                using var newStream = file.OpenReadStream();
                string newText = await _documentTextExtractor.ExtractTextAsync(newStream, file.FileName);

                if (string.IsNullOrWhiteSpace(newText))
                {
                    return new BaseResponse<PlagiarismCheckResponse>("No text extracted from file", StatusCodeEnum.BadRequest_400, new PlagiarismCheckResponse { MaxSimilarity = 0, IsAboveThreshold = false });
                }

                // Get other submissions in same assignment
                var otherSubmissions = (await _submissionRepository.GetByAssignmentIdAsync(assignmentId))
                    .Where(s => s.SubmissionId != excludeSubmissionId)
                    .ToList();

                if (!otherSubmissions.Any())
                {
                    return new BaseResponse<PlagiarismCheckResponse>("No other submissions to compare", StatusCodeEnum.OK_200, new PlagiarismCheckResponse { MaxSimilarity = 0, IsAboveThreshold = false });
                }

                double maxSimilarity = 0;

                foreach (var other in otherSubmissions)
                {
                    using var otherStream = await _fileStorageService.GetFileStreamAsync(other.FileUrl);
                    if (otherStream == null) continue;

                    string otherText = await _documentTextExtractor.ExtractTextAsync(otherStream, other.FileName);
                    if (string.IsNullOrWhiteSpace(otherText)) continue;

                    double sim = CosineSimilarity(newText, otherText);
                    if (sim > maxSimilarity) maxSimilarity = sim;
                }

                var response = new PlagiarismCheckResponse
                {
                    MaxSimilarity = maxSimilarity * 100,
                    IsAboveThreshold = maxSimilarity > threshold,
                    Threshold = threshold * 100
                };

                return new BaseResponse<PlagiarismCheckResponse>(
                    response.IsAboveThreshold ? "High similarity detected" : "Plagiarism check passed",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during plagiarism check");
                return new BaseResponse<PlagiarismCheckResponse>(
                    $"Error checking plagiarism: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private double CosineSimilarity(string text1, string text2)
        {
            var vec1 = TextToVector(text1);
            var vec2 = TextToVector(text2);

            var keys = new HashSet<string>(vec1.Keys);
            keys.UnionWith(vec2.Keys);

            double dot = 0;
            double norm1 = 0;
            double norm2 = 0;

            foreach (var key in keys)
            {
                int freq1 = vec1.GetValueOrDefault(key, 0);
                int freq2 = vec2.GetValueOrDefault(key, 0);

                dot += freq1 * freq2;
                norm1 += freq1 * freq1;
                norm2 += freq2 * freq2;
            }

            norm1 = Math.Sqrt(norm1);
            norm2 = Math.Sqrt(norm2);

            if (norm1 == 0 || norm2 == 0) return 0;
            return dot / (norm1 * norm2);
        }
        private Dictionary<string, int> TextToVector(string text)
        {
            return text.ToLower()
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .GroupBy(word => word)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<BaseResponse<PublishGradesResponse>> PublishGradesAsync(PublishGradesRequest request)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<PublishGradesResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);

                var now = DateTime.UtcNow;
                var finalDeadline = assignment.FinalDeadline ?? assignment.Deadline;
                var isPastDeadline = now > finalDeadline;

                // Lấy sinh viên trong lớp
                var enrolledStudents = await _context.CourseStudents
                    .Where(cs => cs.CourseInstanceId == assignment.CourseInstanceId)
                    .Include(cs => cs.User)
                    .ToListAsync();

                var totalStudents = enrolledStudents.Count;
                if (totalStudents == 0)
                    return new BaseResponse<PublishGradesResponse>("No students in class", StatusCodeEnum.NoContent_204, null);

                // Lấy submissions
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(request.AssignmentId);
                var submittedUserIds = submissions.Select(s => s.UserId).ToHashSet();

                int submittedCount = submittedUserIds.Count;
                int notSubmittedCount = totalStudents - submittedCount;
                int gradedCount = submissions.Count(s => s.Status == "Graded");
                int ungradedCount = submittedCount - gradedCount;

                var response = new PublishGradesResponse
                {
                    AssignmentId = assignment.AssignmentId,
                    AssignmentTitle = assignment.Title ?? "Unknown",
                    TotalStudents = totalStudents,
                    SubmittedCount = submittedCount,
                    NotSubmittedCount = notSubmittedCount,
                    GradedCount = gradedCount,
                    UngradedCount = ungradedCount,
                    IsPublished = false
                };

                var blockingReasons = new List<string>();

                // Kiểm tra điều kiện
                if (ungradedCount > 0 && !request.ForcePublish)
                    blockingReasons.Add($"Còn {ungradedCount} bài chưa chấm.");

                if (notSubmittedCount > 0 && !request.ForcePublish)
                    blockingReasons.Add($"Còn {notSubmittedCount} sinh viên không nộp. Dùng auto-grade-zero.");

                if (!isPastDeadline && !request.ForcePublish)
                    blockingReasons.Add("Chưa đến hạn cuối.");

                // Không public → trả lỗi
                if (blockingReasons.Any() && !request.ForcePublish)
                {
                    response.Note = "Không thể công bố điểm.";
                    response.BlockingReasons = blockingReasons;
                    return new BaseResponse<PublishGradesResponse>(
                        string.Join(" ", blockingReasons),
                        StatusCodeEnum.BadRequest_400,
                        response);
                }

                // === PUBLIC THÀNH CÔNG ===
                bool changed = false;
                foreach (var s in submissions.Where(s => s.Status == "Graded" || s.FinalScore == 0))
                {
                    if (!s.IsPublic)
                    {
                        s.IsPublic = true;
                        changed = true;
                    }
                }
                if (changed) await _context.SaveChangesAsync();

                // Tạo danh sách kết quả
                foreach (var student in enrolledStudents)
                {
                    var submission = submissions.FirstOrDefault(s => s.UserId == student.UserId);
                    var user = student.User;

                    response.Results.Add(new GradeStudentResult
                    {
                        StudentId = user.Id,
                        StudentName = $"{user.FirstName} {user.LastName}".Trim(),
                        StudentCode = user.StudentCode,
                        FinalScore = submission?.FinalScore,
                        Feedback = submission?.Feedback ?? (submission == null ? "Không nộp bài" : null),
                        Status = submission?.Status ?? "Not Submitted"
                    });
                }

                response.IsPublished = true;
                response.PublishedAt = now;
                response.Note = request.ForcePublish ? "Công bố bắt buộc." : "Công bố thành công.";

                _logger.LogInformation($"Grades published for assignment {request.AssignmentId}");

                return new BaseResponse<PublishGradesResponse>(
                    response.Note,
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing grades");
                return new BaseResponse<PublishGradesResponse>(
                    "Lỗi hệ thống", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        // Thêm method để lấy final score đơn giản
        public async Task<BaseResponse<decimal?>> GetMyScoreAsync(int assignmentId, int studentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<decimal?>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                }

                if (assignment.Status != "GradesPublished")
                {
                    return new BaseResponse<decimal?>("Grades not yet published", StatusCodeEnum.Forbidden_403, null);
                }

                var submission = await _submissionRepository.GetByAssignmentAndUserAsync(assignmentId, studentId);
                if (submission == null)
                {
                    return new BaseResponse<decimal?>("No submission found", StatusCodeEnum.NotFound_404, null);
                }

                if (submission.Status != "Graded")
                {
                    return new BaseResponse<decimal?>("Submission not graded", StatusCodeEnum.BadRequest_400, null);
                }

                return new BaseResponse<decimal?>("Score retrieved successfully", StatusCodeEnum.OK_200, submission.FinalScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving score for assignment {assignmentId} and student {studentId}");
                return new BaseResponse<decimal?>("An error occurred", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        // Thêm method để lấy chi tiết điểm
public async Task<BaseResponse<MyScoreDetailsResponse>> GetMyScoreDetailsAsync(int assignmentId, int studentId)
        {
            try
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<MyScoreDetailsResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                }
                if (assignment.Status != "GradesPublished")
                {
                    return new BaseResponse<MyScoreDetailsResponse>("Grades not yet published", StatusCodeEnum.Forbidden_403, null);
                }

                var submission = await _submissionRepository.GetByAssignmentAndUserAsync(assignmentId, studentId);
                if (submission == null)
                {
                    return new BaseResponse<MyScoreDetailsResponse>("No submission found", StatusCodeEnum.NotFound_404, null);
                }

                if (submission.Status != "Graded")
                {
                    return new BaseResponse<MyScoreDetailsResponse>("Submission not graded", StatusCodeEnum.BadRequest_400, null);
                }

                // Lấy regrade requests nếu có
                var regradeRequests = await _regradeRequestRepository.GetBySubmissionIdAsync(submission.SubmissionId);
                var latestRegrade = regradeRequests.OrderByDescending(r => r.RequestedAt).FirstOrDefault();
                var hasPendingRegrade = regradeRequests.Any(r => r.Status == "Pending");
                // Tính điểm trung bình và cao nhất lớp
                var classSubmissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);
                var gradedScores = classSubmissions.Where(s => s.Status == "Graded" && s.FinalScore.HasValue).Select(s => s.FinalScore.Value).ToList();
                decimal classAverage = gradedScores.Any() ? gradedScores.Average() : 0;
                decimal classMax = gradedScores.Any() ? gradedScores.Max() : 0;

                var response = new MyScoreDetailsResponse
                {
                    SubmissionId = submission.SubmissionId,
                    InstructorScore = submission.InstructorScore ?? 0,
                    PeerAverageScore = submission.PeerAverageScore ?? 0,
                    FinalScore = submission.FinalScore ?? 0,
                    Feedback = submission.Feedback,
                    GradedAt = submission.GradedAt,
                    RegradeRequestId = latestRegrade?.RequestId,
                    RegradeStatus = hasPendingRegrade ? "Pending" : (regradeRequests.Any() ? latestRegrade.Status : null),
                    ClassAverageScore = classAverage,
                    ClassMaxScore = classMax,
                    FileUrl = submission.FileUrl,
                    FileName = submission.FileName,
                    KeyWords = submission.Keywords,
                    Note = GenerateScoreNote(submission.FinalScore ?? 0, classAverage, classMax)
                };

                return new BaseResponse<MyScoreDetailsResponse>("Score details retrieved successfully", StatusCodeEnum.OK_200, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving score details for assignment {assignmentId} and student {studentId}");
                return new BaseResponse<MyScoreDetailsResponse>("An error occurred", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        private string GenerateScoreNote(decimal myScore, decimal classAverage, decimal classMax)
        {
            var note = new StringBuilder("Class statistics: ");

            if (myScore > classMax)
            {
                note.Append("Your score is the highest in the class! Great, show off to your friends. ");
            }
            else if (myScore >= classAverage)
            {
                note.Append("Your score is fine, above average. Keep up the good work! ");
            }
            else
            {
                note.Append("Your score is below class average. Please review and improve next time. ");
            }

            note.Append($"Class average: {classAverage:F1}, Highest: {classMax:F1}");

            return note.ToString();
        }

        public async Task<BaseResponse<IEnumerable<SubmissionSummaryResponse>>> GetSubmissionSummaryAsync(
     int? courseId, int? classId, int? assignmentId)
        {
            try
            {
                // 1. XÁC ĐỊNH CourseInstanceId
                int? courseInstanceId = null;

                if (assignmentId.HasValue)
                {
                    var assignment = await _assignmentRepository.GetByIdAsync(assignmentId.Value);
                    if (assignment == null)
                        return new BaseResponse<IEnumerable<SubmissionSummaryResponse>>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                    courseInstanceId = assignment.CourseInstanceId;
                }
                else if (classId.HasValue)
                {
                    courseInstanceId = classId.Value;
                }
                else if (courseId.HasValue)
                {
                    var instance = await _context.CourseInstances
                        .FirstOrDefaultAsync(ci => ci.CourseId == courseId.Value);
                    if (instance == null)
                        return new BaseResponse<IEnumerable<SubmissionSummaryResponse>>("Class not found", StatusCodeEnum.NotFound_404, null);
                    courseInstanceId = instance.CourseInstanceId;
                }
                else
                {
                    return new BaseResponse<IEnumerable<SubmissionSummaryResponse>>("Invalid filter", StatusCodeEnum.BadRequest_400, null);
                }

                // 2. LẤY TOÀN BỘ SINH VIÊN TRONG LỚP
                var enrolledStudents = await _context.CourseStudents
                    .Where(cs => cs.CourseInstanceId == courseInstanceId)
                    .Include(cs => cs.User)
                    .Select(cs => cs.User)
                    .ToListAsync();

                if (!enrolledStudents.Any())
                {
                    return new BaseResponse<IEnumerable<SubmissionSummaryResponse>>(
                        "No students enrolled",
                        StatusCodeEnum.NoContent_204,
                        Enumerable.Empty<SubmissionSummaryResponse>());
                }

                // 3. LẤY SUBMISSIONS (nếu có assignmentId)
                List<Submission> submissions = new();
                if (assignmentId.HasValue)
                {
                    submissions = await _context.Submissions
                        .Where(s => s.AssignmentId == assignmentId.Value)
                        .Include(s => s.Assignment)
                            .ThenInclude(a => a.CourseInstance)
                                .ThenInclude(ci => ci.Course)
                        .ToListAsync();
                }

                // 4. TẠO DANH SÁCH ĐẦY ĐỦ
                var result = new List<SubmissionSummaryResponse>();

                foreach (var student in enrolledStudents)
                {
                    var submission = assignmentId.HasValue
                        ? submissions.FirstOrDefault(s => s.UserId == student.Id)
                        : null;

                    if (submission != null)
                    {
                        result.Add(new SubmissionSummaryResponse
                        {
                            SubmissionId = submission.SubmissionId,
                            AssignmentId = submission.AssignmentId,
                            UserId = student.Id,
                            StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                            StudentCode = student.StudentCode,
                            StudentEmail = student.Email,
                            CourseName = submission.Assignment?.CourseInstance?.Course?.CourseName,
                            ClassName = submission.Assignment?.CourseInstance?.SectionCode,
                            AssignmentTitle = submission.Assignment?.Title,
                            PeerAverageScore = submission.PeerAverageScore ?? 0,
                            InstructorScore = submission.InstructorScore ?? 0,
                            FinalScore = submission.FinalScore ?? 0,
                            Feedback = submission.Feedback,
                            Status = submission.Status,
                            GradedAt = submission.GradedAt
                        });
                    }
                    else
                    {
                        // SINH VIÊN KHÔNG NỘP
                        var assignment = assignmentId.HasValue
                            ? await _assignmentRepository.GetByIdAsync(assignmentId.Value)
                            : null;

                        result.Add(new SubmissionSummaryResponse
                        {
                            SubmissionId = 0,
                            AssignmentId = assignmentId ?? 0,
                            UserId = student.Id,
                            StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                            StudentCode = student.StudentCode,
                            StudentEmail = student.Email,
                            CourseName = assignment?.CourseInstance?.Course?.CourseName,
                            ClassName = assignment?.CourseInstance?.SectionCode,
                            AssignmentTitle = assignment?.Title,
                            PeerAverageScore = 0,
                            InstructorScore = 0,
                            FinalScore = 0,
                            Feedback = "Không nộp bài",
                            Status = "Not Submitted",
                            GradedAt = null
                        });
                    }
                }

                result = result.OrderBy(x => x.StudentName).ThenBy(x => x.StudentCode).ToList();

                return new BaseResponse<IEnumerable<SubmissionSummaryResponse>>(
                    "Submission summary fetched successfully (full class)",
                    StatusCodeEnum.OK_200,
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching submission summary");
                return new BaseResponse<IEnumerable<SubmissionSummaryResponse>>(
                    "An error occurred while fetching submission summary",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }



        public async Task<BaseResponse<AutoGradeZeroResponse>> AutoGradeZeroForNonSubmittersAsync(AutoGradeZeroRequest request)
        {
            try
            {
                // 1. Xác nhận confirm
                if (!request.ConfirmZeroGrade)
                    return new BaseResponse<AutoGradeZeroResponse>(
                        "Bạn phải xác nhận (ConfirmZeroGrade = true) để chấm 0 điểm.",
                        StatusCodeEnum.BadRequest_400, null);

                // 2. Lấy assignment
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<AutoGradeZeroResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);

                // 3. Chỉ cho phép khi status Closed hoặc Cancelled
                if (assignment.Status != AssignmentStatusEnum.Closed.ToString() &&
                    assignment.Status != AssignmentStatusEnum.Cancelled.ToString())
                {
                    return new BaseResponse<AutoGradeZeroResponse>(
                        $"Assignment status là {assignment.Status}, không thể chấm 0 điểm.",
                        StatusCodeEnum.BadRequest_400, null);
                }

                // 4. Chỉ chấm sau ReviewDeadline
                var now = DateTime.UtcNow;
                if (assignment.ReviewDeadline.HasValue && now <= assignment.ReviewDeadline.Value)
                {
                    return new BaseResponse<AutoGradeZeroResponse>(
                        $"Chưa đến ReviewDeadline ({assignment.ReviewDeadline:yyyy-MM-dd HH:mm}). Không thể chấm 0.",
                        StatusCodeEnum.BadRequest_400, null);
                }

                // 5. Lấy danh sách sinh viên trong lớp
                var enrolledStudentIds = await _context.CourseStudents
                    .Where(cs => cs.CourseInstanceId == assignment.CourseInstanceId)
                    .Select(cs => cs.UserId)
                    .ToListAsync();

                // 6. Lấy các submission hiện có
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(request.AssignmentId);
                var submittedUserIds = submissions.Select(s => s.UserId).ToHashSet();

                // 7. Sinh viên chưa nộp
                var nonSubmitters = enrolledStudentIds.Except(submittedUserIds).ToList();
                if (!nonSubmitters.Any())
                    return new BaseResponse<AutoGradeZeroResponse>(
                        "Tất cả sinh viên đã nộp bài hoặc đã được chấm 0.",
                        StatusCodeEnum.OK_200,
                        new AutoGradeZeroResponse { Success = true, NonSubmittedCount = 0 });

                // 8. Lấy thông tin sinh viên (TRƯỚC foreach)
                var users = await _context.Users
                    .Where(u => nonSubmitters.Contains(u.Id))
                    .Select(u => new { u.Id, u.StudentCode })
                    .ToListAsync();

                // 9. Tạo submission chấm 0
                var newSubmissions = new List<Submission>();
                var studentCodes = new List<string>();

                foreach (var userId in nonSubmitters)
                {
                    var user = users.First(u => u.Id == userId);

                    // SỬ DỤNG FinalDeadline nếu có, nếu không thì Deadline (cả hai đều DateTime/DateTime?)
                    var submittedAt = assignment.FinalDeadline.HasValue
                        ? assignment.FinalDeadline.Value
                        : assignment.Deadline;

                    var submission = new Submission
                    {
                        AssignmentId = request.AssignmentId,
                        UserId = userId,
                        FileUrl = "Không nộp",           // KHÔNG NULL
                        FileName = "Không nộp",          // KHÔNG NULL
                        OriginalFileName = "Không nộp bài",
                        Keywords = " ",
                        SubmittedAt = submittedAt,
                        Status = "Graded",
                        FinalScore = 0,
                        Feedback = "Không nộp bài, tự động chấm 0 điểm.",
                        GradedAt = now,
                        IsPublic = true
                    };

                    newSubmissions.Add(submission);
                    studentCodes.Add(user.StudentCode ?? $"User_{userId}");
                }

                // 10. Lưu với transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _submissionRepository.AddRangeAsync(newSubmissions);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex,
                        "AutoGradeZero ROLLBACK | AssignmentId: {AssignmentId} | NonSubmitters: {Count} | Error: {Message}",
                        request.AssignmentId, nonSubmitters.Count, ex.Message);

                    return new BaseResponse<AutoGradeZeroResponse>(
                        $"Lỗi hệ thống khi lưu dữ liệu: {ex.Message}",
                        StatusCodeEnum.InternalServerError_500, null);
                }

                // 11. Tạo response
                var response = new AutoGradeZeroResponse
                {
                    AssignmentId = request.AssignmentId,
                    AssignmentTitle = assignment.Title ?? "Unknown",
                    NonSubmittedCount = nonSubmitters.Count,
                    GradedZeroCount = newSubmissions.Count,
                    StudentCodes = studentCodes,
                    Success = true,
                    Message = $"Đã chấm 0 điểm cho {newSubmissions.Count} sinh viên chưa nộp bài.",
                    ProcessedAt = now
                };

                _logger.LogInformation(
                    "AutoGradeZero SUCCESS | Assignment {AssignmentId} | Chấm 0 cho {Count} sinh viên",
                    request.AssignmentId, newSubmissions.Count);

                return new BaseResponse<AutoGradeZeroResponse>(
                    response.Message,
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "AutoGradeZero FAILED | AssignmentId: {AssignmentId} | StackTrace: {StackTrace}",
                    request.AssignmentId, ex.StackTrace);

                return new BaseResponse<AutoGradeZeroResponse>(
                    $"Lỗi hệ thống: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500, null);
            }
        }

    }
}