using AutoMapper;
using BussinessObject.Models;
using DataAccessLayer;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
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
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<SubmissionService> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly ASDPRSContext _context;
        private readonly IReviewAssignmentService _reviewAssignmentService;
        private readonly ISystemConfigService _systemConfigService;
        private readonly IDocumentTextExtractor _documentTextExtractor;
        private readonly IGenAIService _genAIService;

        public SubmissionService(
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            IAISummaryRepository aiSummaryRepository,
            IRegradeRequestRepository regradeRequestRepository,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<SubmissionService> logger,
            IFileStorageService fileStorageService,
            ASDPRSContext context,
            IReviewAssignmentService reviewAssignmentService, ISystemConfigService systemConfigService,
            IDocumentTextExtractor documentTextExtractor, IGenAIService genAIService)
        {
            _submissionRepository = submissionRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _aiSummaryRepository = aiSummaryRepository;
            _regradeRequestRepository = regradeRequestRepository;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _context = context;
            _reviewAssignmentService = reviewAssignmentService;
            _systemConfigService = systemConfigService;
            _documentTextExtractor = documentTextExtractor;
            _genAIService = genAIService;
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

                var now = DateTime.UtcNow.AddHours(7);
                if (assignment.StartDate.HasValue && now < assignment.StartDate.Value)
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment before start date: {assignment.StartDate.Value}",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                if (now > assignment.Deadline)
                {
                    return new BaseResponse<SubmissionResponse>(
                        $"Cannot submit assignment after deadline: {assignment.Deadline}. Late submissions are not allowed.",
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
                    FileUrl = uploadResult.FileUrl,
                    FileName = uploadResult.FileName,
                    OriginalFileName = request.File.FileName,
                    Keywords = request.Keywords,
                    SubmittedAt = now,
                    Status = "Submitted",
                    IsPublic = request.IsPublic
                };

                var createdSubmission = await _submissionRepository.AddAsync(submission);
                var response = await MapToSubmissionResponse(createdSubmission);

                _logger.LogInformation($"Submission created successfully. SubmissionId: {createdSubmission.SubmissionId}");
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

                var now = DateTime.UtcNow.AddHours(7);
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
                    SubmittedAt = DateTime.UtcNow.AddHours(7),
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
                    UpdatedAt = DateTime.UtcNow.AddHours(7),
                    UpdatedByUserId = 1  // Assume, or pass
                };
                _context.SystemConfigs.Add(config);
            }
            else
            {
                config.ConfigValue = value;
                config.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                // 1️⃣ Lấy assignment + rubric
                var assignment = await _assignmentRepository.GetAssignmentWithRubricAsync(assignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<SubmissionListResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var courseInstanceId = assignment.CourseInstanceId;

                // 2️⃣ Lấy toàn bộ sinh viên trong lớp
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

                // 3️⃣ Lấy submissions của assignment
                var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignmentId);

                // Map sẵn assignment info (tránh map lặp)
                var assignmentInfo = _mapper.Map<AssignmentInfoResponse>(assignment);

                var responseList = new List<SubmissionResponse>();

                // 4️⃣ Build danh sách đầy đủ sinh viên
                foreach (var student in enrolledStudents)
                {
                    var submission = submissions.FirstOrDefault(s => s.UserId == student.Id);

                    if (submission != null)
                    {
                        // ✅ Có nộp bài
                        var submissionResponse = await MapToSubmissionResponse(submission);
                        responseList.Add(submissionResponse);
                    }
                    else
                    {
                        // ❌ Chưa nộp bài
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

                            SubmittedAt = null,
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

                // 5️⃣ Sắp xếp
                responseList = responseList
                    .OrderBy(x => x.StudentName)
                    .ThenBy(x => x.StudentCode)
                    .ToList();

                // 6️⃣ Response
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
                _logger.LogError(ex, $"Error in GetSubmissionsAllStudentByAssignmentIdAsync for assignment {assignmentId}");

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
            try
            {
                // 1️⃣ Gọi lại hàm core lấy Submission
                var request = new GetSubmissionByIdRequest
                {
                    SubmissionId = submissionId,
                    IncludeReviews = true,
                    IncludeAISummaries = true
                };

                var baseResponse = await GetSubmissionByIdAsync(request);

                if (baseResponse?.Data == null)
                    return baseResponse;

                var response = baseResponse.Data;

                // 2️⃣ Chỉ lấy CriteriaFeedback của Instructor
                var instructorFeedbacks = await _context.CriteriaFeedbacks
                    .Where(cf =>
                        cf.Review.ReviewAssignment.SubmissionId == submissionId &&
                        cf.FeedbackSource == "Instructor"
                    )
                    .Select(cf => new SubmissionCriteriaFeedbackResponse
                    {
                        CriteriaId = cf.CriteriaId,
                        ScoreAwarded = cf.ScoreAwarded,
                        Feedback = cf.Feedback ?? string.Empty
                    })
                    .ToListAsync();

                response.CriteriaFeedbacks = instructorFeedbacks;

                // 3️⃣ Đảm bảo các field KHÔNG BAO GIỜ null
                response.InstructorScore ??= 0;
                response.PeerAverageScore ??= 0;
                response.FinalScore ??= 0;
                response.Feedback ??= string.Empty;

                response.CriteriaFeedbacks ??= new List<SubmissionCriteriaFeedbackResponse>();
                response.ReviewAssignments ??= new List<SubmissionReviewAssignmentResponse>();
                response.AISummaries ??= new List<AISummaryResponse>();
                response.RegradeRequests ??= new List<RegradeRequestSubmissionResponse>();

                return new BaseResponse<SubmissionResponse>(
                    "Submission with instructor feedback retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<SubmissionResponse>(
                    $"Error retrieving submission details: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
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
            response.PreviewUrl = GeneratePreviewUrl(submission.FileUrl);

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
                var now = DateTime.UtcNow.AddHours(7);

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
                // 1️⃣ Lấy submission
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                    return new BaseResponse<GradeSubmissionResponse>("Submission not found", StatusCodeEnum.NotFound_404, null);
                //  Không cho chấm thủ công nếu bài này đã AutoGradeZero
                if (submission.FileUrl == "Not Submitted"
                    && submission.FinalScore == 0
                    && submission.Feedback != null
                    && submission.Feedback.Contains("auto grade zero"))
                {
                    return new BaseResponse<GradeSubmissionResponse>(
                        "This submission was automatically graded with zero and cannot be manually graded.",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }
                submission.Feedback = request.Feedback;
                // 2️⃣ Lấy assignment
                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<GradeSubmissionResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                // 🔥 NEW LOGIC: Nếu assignment đã publish điểm → chỉ cho chấm nếu regrade đã được approved
                if (assignment.Status == "GradesPublished")
                {
                    var latestRegradeRequest = await _context.RegradeRequests
                        .Where(r => r.SubmissionId == submission.SubmissionId)
                        .OrderByDescending(r => r.RequestedAt)
                        .FirstOrDefaultAsync();

                    if (latestRegradeRequest == null || latestRegradeRequest.Status != "Approved")
                    {
                        return new BaseResponse<GradeSubmissionResponse>(
                            "Cannot regrade: grades already published and no approved regrade request found.",
                            StatusCodeEnum.Forbidden_403,
                            null
                        );
                    }
                    // LƯU OLD SCORE (CHỈ LƯU 1 LẦN)
                    if (submission.OldScore == null)
                    {
                        submission.OldScore = submission.FinalScore;
                    }
                }
                decimal instructorScore = 0m;

                // 3️⃣ Xử lý chấm theo tiêu chí (rubric)
                if (request.CriteriaFeedbacks != null && request.CriteriaFeedbacks.Any())
                {
                    // Xóa feedback cũ của instructor
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

                    // Tạo ReviewAssignment mới nếu chưa có
                    var instructorReviewAssignment = await _context.ReviewAssignments
                        .FirstOrDefaultAsync(ra => ra.SubmissionId == submission.SubmissionId
                                                   && ra.ReviewerUserId == request.InstructorId
                                                   && !ra.IsAIReview);

                    if (instructorReviewAssignment == null)
                    {
                        instructorReviewAssignment = new ReviewAssignment
                        {
                            SubmissionId = submission.SubmissionId,
                            ReviewerUserId = request.InstructorId,
                            AssignedAt = DateTime.UtcNow.AddHours(7),
                            Deadline = DateTime.UtcNow.AddHours(7).AddDays(7),
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
                        ReviewedAt = DateTime.UtcNow.AddHours(7),
                        ReviewType = "Instructor",
                        FeedbackSource = "Instructor"
                    };
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();

                    // Tính tổng điểm theo trọng số
                    decimal totalScore = 0m;
                    decimal totalWeight = 0m;

                    foreach (var cf in request.CriteriaFeedbacks)
                    {
                        // ✅ Validate score
                        if (cf.Score < 0 || cf.Score > 10)
                        {
                            return new BaseResponse<GradeSubmissionResponse>(
                                $"Score for criteria {cf.CriteriaId} must be between 0 and 10",
                                StatusCodeEnum.BadRequest_400,
                                null
                            );
                        }

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

                    instructorScore = totalWeight > 0 ? Math.Round(totalScore / totalWeight, 2) : 0m;
                    review.OverallScore = instructorScore;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Không chấm theo tiêu chí → chỉ lưu feedback tổng quát
                    var reviewAssignment = await _context.ReviewAssignments
                        .FirstOrDefaultAsync(ra => ra.SubmissionId == submission.SubmissionId
                                                   && ra.ReviewerUserId == request.InstructorId);

                    if (reviewAssignment == null)
                    {
                        reviewAssignment = new ReviewAssignment
                        {
                            SubmissionId = submission.SubmissionId,
                            ReviewerUserId = request.InstructorId,
                            AssignedAt = DateTime.UtcNow.AddHours(7),
                            Deadline = DateTime.UtcNow.AddHours(7).AddDays(7),
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
                        ReviewedAt = DateTime.UtcNow.AddHours(7),
                        ReviewType = "Instructor",
                        FeedbackSource = "Instructor"
                    };
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();
                }

                // 4️⃣ Lấy điểm peer trung bình
                var peerAverage = await _reviewAssignmentRepository.GetPeerAverageScoreBySubmissionIdAsync(submission.SubmissionId);
                var peerAvg = peerAverage ?? 0m;
                // Nếu peerAvg = 0 => xem như không có peer review
                bool noPeer = peerAvg == 0;

                // 5️⃣ Chuẩn hóa trọng số
                var instructorWeight = assignment.InstructorWeight;
                var peerWeight = assignment.PeerWeight;

                if (instructorWeight + peerWeight == 0)
                {
                    instructorWeight = 50m;
                    peerWeight = 50m;
                }
                else if (instructorWeight + peerWeight != 100m)
                {
                    var total = instructorWeight + peerWeight;
                    instructorWeight = (instructorWeight / total) * 100m;
                    peerWeight = (peerWeight / total) * 100m;
                }

                decimal instructorScoreNormalized = instructorScore;
                decimal peerScoreNormalized = peerAvg;

                // 6️⃣ Tính FinalScore trước penalty
                decimal finalScore = Math.Round(
                    (instructorScoreNormalized * instructorWeight / 100m) +
                    (peerScoreNormalized * peerWeight / 100m),
                    2);
                if (peerAvg == 0)
                {
                    finalScore = instructorScoreNormalized;
                }
                decimal finalScoreBeforePenalty = finalScore;

                // === Missing Review Penalty ===
                int requiredReviews = assignment.NumPeerReviewsRequired;
                int missingReviews = 0;
                decimal missingReviewPenaltyPerReview = assignment.MissingReviewPenalty ?? 0m;
                decimal missingReviewPenaltyTotal = 0m;

                if (requiredReviews > 0)
                {
                    int completedReviews = await _context.ReviewAssignments
                    .Where(ra => ra.SubmissionId == submission.SubmissionId)
                    .SelectMany(ra => ra.Reviews)
                    .CountAsync(r => r.ReviewedAt.HasValue);


                    missingReviews = Math.Max(0, requiredReviews - completedReviews);

                    if (missingReviews > 0 && missingReviewPenaltyPerReview > 0)
                    {
                        missingReviewPenaltyTotal = missingReviews * missingReviewPenaltyPerReview;
                        finalScore = Math.Max(0m, finalScore - missingReviewPenaltyTotal);

                        _logger.LogInformation(
                            "Penalty applied: submission {SubId}, missing {Missing}, perReview {Per}, total {Total}, before {Before}, after {After}",
                            submission.SubmissionId,
                            missingReviews,
                            missingReviewPenaltyPerReview,
                            missingReviewPenaltyTotal,
                            finalScoreBeforePenalty,
                            finalScore
                        );
                    }
                }

                // 7️⃣ Cập nhật submission
                submission.InstructorScore = instructorScore;
                submission.PeerAverageScore = peerAvg;
                submission.FinalScore = finalScore;
                //submission.FinalScoreBeforePenalty = finalScoreBeforePenalty;
                //submission.MissingReviews = missingReviews;
                //submission.MissingReviewPenaltyPerReview = missingReviewPenaltyPerReview;
                //submission.MissingReviewPenaltyTotal = missingReviewPenaltyTotal;
                submission.Feedback = request.Feedback ?? submission.Feedback;
                submission.GradedAt = null;
                submission.Status = "Graded";
                if (request.PublishImmediately)
                    submission.IsPublic = true;

                await _context.SaveChangesAsync();
                var newestRegradeRequest = await _context.RegradeRequests
                     .Where(r => r.SubmissionId == submission.SubmissionId)
                     .OrderByDescending(r => r.RequestedAt)
                     .FirstOrDefaultAsync();


                // 8️⃣ Chuẩn bị response
                var response = new GradeSubmissionResponse
                {
                    SubmissionId = submission.SubmissionId,
                    AssignmentId = submission.AssignmentId,
                    UserId = submission.UserId,
                    InstructorScore = submission.InstructorScore,
                    PeerAverageScore = submission.PeerAverageScore,
                    FinalScore = submission.FinalScore,
                    FinalScoreBeforePenalty = finalScoreBeforePenalty,
                    MissingReviews = missingReviews,
                    MissingReviewPenaltyPerReview = missingReviewPenaltyPerReview,
                    MissingReviewPenaltyTotal = missingReviewPenaltyTotal,
                    OldScore = submission.OldScore,
                    Feedback = submission.Feedback,
                    GradedAt = null,
                    FileUrl = submission.FileUrl,
                    FileName = submission.FileName,
                    Status = submission.Status,
                    RegradeRequestStatus = newestRegradeRequest?.Status,
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
                _logger.LogError(ex, "Error grading submission");
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

        public async Task<BaseResponse<PlagiarismCheckResponse>> CheckPlagiarismActiveAsync(int assignmentId, IFormFile file, int? studentId = null)
        {
            if (file == null)
            {
                return new BaseResponse<PlagiarismCheckResponse>("No file provided for check", StatusCodeEnum.BadRequest_400, null);
            }

            try
            {
                // 1. Lấy thông tin Assignment và Student
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                if (assignment == null)
                    return new BaseResponse<PlagiarismCheckResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);

                var studentName = "Unknown Student";
                if (studentId.HasValue)
                {
                    var student = await _userRepository.GetByIdAsync(studentId.Value);
                    if (student != null) studentName = $"{student.FirstName} {student.LastName}";
                }

                // 2. Extract Text từ file upload
                using var newStream = file.OpenReadStream();
                string newText = await _documentTextExtractor.ExtractTextAsync(newStream, file.FileName);

                if (string.IsNullOrWhiteSpace(newText))
                {
                    return new BaseResponse<PlagiarismCheckResponse>(
                        "No text extracted",
                        StatusCodeEnum.BadRequest_400,
                        new PlagiarismCheckResponse { RelevantContent = false, ContentChecking = "Empty file", PlagiarismContent = 0 }
                    );
                }


                var aiCheckTask = _genAIService.CheckIntegrityAsync(
                    newText.Length > 8000 ? newText.Substring(0, 8000) : newText,
                    assignment.Title,
                    studentName
                );

                var plagiarismTask = Task.Run(async () =>
                {
                    var otherSubmissions = (await _submissionRepository.GetByAssignmentIdAsync(assignmentId))
                        .Where(s => s.UserId != studentId)
                        .ToList();

                    if (!otherSubmissions.Any()) return 0.0;

                    double maxSim = 0;
                    foreach (var other in otherSubmissions)
                    {
                        using var otherStream = await _fileStorageService.GetFileStreamAsync(other.FileUrl);
                        if (otherStream == null) continue;

                        string otherText = await _documentTextExtractor.ExtractTextAsync(otherStream, other.FileName);
                        if (string.IsNullOrWhiteSpace(otherText)) continue;

                        double sim = CosineSimilarity(newText, otherText);
                        if (sim > maxSim) maxSim = sim;
                    }
                    return maxSim * 100;
                });

                await Task.WhenAll(aiCheckTask, plagiarismTask);

                var aiResult = await aiCheckTask;
                var plagiarismPercent = await plagiarismTask;

                // Get Threshold config
                var thresholdStr = await _systemConfigService.GetSystemConfigAsync("PlagiarismThreshold") ?? "80";
                double threshold = double.Parse(thresholdStr);

                // 4. Tổng hợp kết quả
                var response = new PlagiarismCheckResponse
                {
                    RelevantContent = aiResult.IsRelevant,
                    ContentChecking = aiResult.CheatDetails,
                    PlagiarismContent = Math.Round(plagiarismPercent, 2),
                    Threshold = threshold
                };

                return new BaseResponse<PlagiarismCheckResponse>(
                    "Check completed successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during plagiarism check");
                return new BaseResponse<PlagiarismCheckResponse>(
                    $"Error checking: {ex.Message}",
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
                // 🔒 Nếu đã publish rồi => không cho publish lại
                if (assignment.Status == AssignmentStatusEnum.GradesPublished.ToString())
                {
                    return new BaseResponse<PublishGradesResponse>(
                        "Grades have already been published for this assignment.",
                        StatusCodeEnum.BadRequest_400,
                        new PublishGradesResponse
                        {
                            AssignmentId = assignment.AssignmentId,
                            AssignmentTitle = assignment.Title ?? "Unknown",
                            IsPublished = true,
                            Note = "Assignment is already in 'GradesPublished' status."
                        });
                }
                var now = DateTime.UtcNow.AddHours(7);
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
                    AssignmentStatus = assignment.Status,
                    TotalStudents = totalStudents,
                    SubmittedCount = submittedCount,
                    NotSubmittedCount = notSubmittedCount,
                    GradedCount = gradedCount,
                    UngradedCount = ungradedCount,
                    IsPublished = false
                };

                var blockingReasons = new List<string>();

                // Check conditions
                if (ungradedCount > 0 && !request.ForcePublish)
                    blockingReasons.Add($"{ungradedCount} submissions are not graded.");

                if (notSubmittedCount > 0 && !request.ForcePublish)
                    blockingReasons.Add($"{notSubmittedCount} students did not submit. Use auto-grade-zero.");

                if (!isPastDeadline && !request.ForcePublish)
                    blockingReasons.Add("The deadline has not passed yet.");

                // Cannot publish → return error
                if (blockingReasons.Any() && !request.ForcePublish)
                {
                    response.Note = "Unable to publish grades.";
                    response.BlockingReasons = blockingReasons;

                    return new BaseResponse<PublishGradesResponse>(
                        string.Join(" ", blockingReasons),
                        StatusCodeEnum.BadRequest_400,
                        response
                    );
                }

                // === PUBLIC THÀNH CÔNG ===
                bool changed = false;
                var publicTime = DateTime.UtcNow.AddHours(7);
                foreach (var s in submissions.Where(s => s.Status == "Graded" || s.FinalScore == 0))
                {
                    s.IsPublic = true;

                    if (s.GradedAt == null)
                    {
                        s.GradedAt = publicTime;
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
                        Feedback = submission?.Feedback ?? (submission == null ? "Not Submitted" : null),
                        Status = submission?.Status ?? "Not Submitted",
                        PublicAt = submission?.GradedAt
                    });
                }

                response.IsPublished = true;
                response.PublishedAt = now;
                response.Note = request.ForcePublish ? "Công bố bắt buộc." : "Công bố thành công.";

                // 🟢 Cập nhật trạng thái assignment sang GradesPublished
                assignment.Status = AssignmentStatusEnum.GradesPublished.ToString();
                await _assignmentRepository.UpdateAsync(assignment);
                response.AssignmentStatus = AssignmentStatusEnum.GradesPublished.ToString();
                // 🔔 Gửi notification cho tất cả sinh viên trong lớp
                try
                {
                    await _notificationService.SendGradesPublishedNotificationToStudents(assignment.AssignmentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending grades published notifications for assignment {assignment.AssignmentId}");
                }
                _logger.LogInformation($"Grades published for assignment {request.AssignmentId}, status set to GradesPublished.");

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
                    AssignmentId = submission.AssignmentId,
                    AssignmentTitle = assignment.Title,
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
                    previewUrl = GeneratePreviewUrl(submission.FileUrl),
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

                    var newestRegradeRequest = submission != null
        ? await _context.RegradeRequests
            .Where(r => r.SubmissionId == submission.SubmissionId)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync()
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
                            AssignmentStatus = submission.Assignment?.Status,
                            RegradeRequestStatus = newestRegradeRequest?.Status,
                            SubmittedAt = submission.SubmittedAt,
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
                            AssignmentStatus = assignment?.Status,
                            RegradeRequestStatus = newestRegradeRequest?.Status,
                            PeerAverageScore = 0,
                            InstructorScore = 0,
                            FinalScore = 0,
                            Feedback = "Not Submitted",
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
                // 1. Confirm
                if (!request.ConfirmZeroGrade)
                    return new BaseResponse<AutoGradeZeroResponse>(
                        "You must confirm (ConfirmZeroGrade = true) to apply zero grades.",
                        StatusCodeEnum.BadRequest_400, null);

                // 2. Get assignment
                var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
                if (assignment == null)
                    return new BaseResponse<AutoGradeZeroResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404, null);

                // 3. Only allowed when status is Closed or Cancelled
                if (assignment.Status != AssignmentStatusEnum.Closed.ToString() &&
                    assignment.Status != AssignmentStatusEnum.Cancelled.ToString())
                {
                    return new BaseResponse<AutoGradeZeroResponse>(
                        $"Assignment status is {assignment.Status}, zero-grading is not allowed.",
                        StatusCodeEnum.BadRequest_400, null);
                }

                // 4. Only grade zero after ReviewDeadline
                var now = DateTime.UtcNow.AddHours(7);
                if (assignment.ReviewDeadline.HasValue && now <= assignment.ReviewDeadline.Value)
                {
                    return new BaseResponse<AutoGradeZeroResponse>(
                        $"ReviewDeadline has not passed yet ({assignment.ReviewDeadline:yyyy-MM-dd HH:mm}). Zero grading is not allowed.",
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
                        "All student have submitted or graded zero.",
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
                        FileUrl = "Not Submitted",           // KHÔNG NULL
                        FileName = "Not Submitted",          // KHÔNG NULL
                        OriginalFileName = "Not Submitted",
                        Keywords = " ",
                        SubmittedAt = submittedAt,
                        Status = "Graded",
                        FinalScore = 0,
                        Feedback = "Not Submitted, auto grade zero",
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
                    Message = $"Assigned zero grade to {newSubmissions.Count} students who did not submit.",
                    ProcessedAt = now
                };

                _logger.LogInformation(
                    "AutoGradeZero SUCCESS | Assignment {AssignmentId} | Zero graded for {Count} students",
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
                    $"Server Error: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<IEnumerable<InstructorSubmissionInfoResponse>> GetInstructorSubmissionInfoAsync(
    int userId, int? classId, int? assignmentId)
        {
            try
            {
                int? courseInstanceId = null;

                // 1️⃣ Nếu truyền assignmentId → lấy ra classId + check instructor
                if (assignmentId.HasValue)
                {
                    var asm = await _context.Assignments
                        .Include(a => a.CourseInstance)
                            .ThenInclude(ci => ci.CourseInstructors)
                        .Include(a => a.CourseInstance.Course)
                        .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId.Value);

                    if (asm == null)
                        throw new Exception("Assignment not found");

                    if (!asm.CourseInstance.CourseInstructors.Any(ci => ci.UserId == userId))
                        throw new Exception("You are not an instructor of this class");

                    courseInstanceId = asm.CourseInstanceId;
                }
                // 2️⃣ Nếu truyền classId → check instructor dạy lớp đó
                else if (classId.HasValue)
                {
                    var instance = await _context.CourseInstances
                        .Include(ci => ci.CourseInstructors)
                        .Include(ci => ci.Course)
                        .FirstOrDefaultAsync(ci => ci.CourseInstanceId == classId.Value);

                    if (instance == null)
                        throw new Exception("Class not found");

                    if (!instance.CourseInstructors.Any(ci => ci.UserId == userId))
                        throw new Exception("You are not an instructor of this class");

                    courseInstanceId = instance.CourseInstanceId;
                }
                else
                {
                    throw new Exception("You must provide classId or assignmentId");
                }

                // 3️⃣ Lấy toàn bộ sinh viên của class
                var students = await _context.CourseStudents
                    .Where(cs => cs.CourseInstanceId == courseInstanceId)
                    .Include(cs => cs.User)
                    .Select(cs => cs.User)
                    .ToListAsync();

                if (!students.Any())
                    return Enumerable.Empty<InstructorSubmissionInfoResponse>();

                // 4️⃣ Lấy toàn bộ assignment trong lớp (nếu chưa cung cấp assignmentId)
                List<Assignment> assignments;

                if (assignmentId.HasValue)
                {
                    assignments = await _context.Assignments
                        .Where(a => a.AssignmentId == assignmentId.Value)
                        .Include(a => a.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                        .ToListAsync();
                }
                else
                {
                    assignments = await _context.Assignments
                        .Where(a => a.CourseInstanceId == courseInstanceId)
                        .Include(a => a.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                        .ToListAsync();
                }

                // 5️⃣ Lấy toàn bộ submission trong những assignment thuộc lớp
                var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();

                var submissions = await _context.Submissions
                    .Where(s => assignmentIds.Contains(s.AssignmentId))
                    .Include(s => s.User)
                    .Include(s => s.Assignment)
                        .ThenInclude(a => a.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                    .ToListAsync();

                // 6️⃣ Tạo danh sách kết quả
                var result = new List<InstructorSubmissionInfoResponse>();

                foreach (var asm in assignments)
                {
                    foreach (var student in students)
                    {
                        var submission = submissions.FirstOrDefault(
                            s => s.AssignmentId == asm.AssignmentId && s.UserId == student.Id);

                        // Lấy regrade mới nhất
                        var latestRegrade = submission != null
                            ? await _context.RegradeRequests
                                .Where(r => r.SubmissionId == submission.SubmissionId)
                                .OrderByDescending(r => r.RequestedAt)
                                .FirstOrDefaultAsync()
                            : null;

                        result.Add(new InstructorSubmissionInfoResponse
                        {
                            // Student
                            UserId = student.Id,
                            Username = student.UserName,
                            StudentCode = student.StudentCode,

                            // Assignment
                            AssignmentId = asm.AssignmentId,
                            AssignmentTitle = asm.Title,

                            // Course & Class
                            CourseInstanceId = asm.CourseInstanceId,
                            CourseId = asm.CourseInstance?.CourseId ?? 0,
                            ClassName = asm.CourseInstance?.SectionCode,
                            CourseName = asm.CourseInstance?.Course?.CourseName,

                            // Submission
                            SubmissionId = submission?.SubmissionId ?? 0,
                            SubmittedAt = submission?.SubmittedAt,
                            FileUrl = submission?.FileUrl,
                            FileName = submission?.FileName,
                            OriginalFileName = submission?.OriginalFileName,
                            StatusSubmission = submission?.Status ?? "Not Submitted",
                            FinalScore = submission?.FinalScore,
                            GradedAt = submission?.GradedAt,

                            // Regrade
                            RegradeReason = latestRegrade?.Reason,
                            RegradeStatus = latestRegrade?.Status,
                            RequestedAt = latestRegrade?.RequestedAt,
                            ReviewedByUserId = latestRegrade?.ReviewedByUserId,
                            ResolutionNotes = latestRegrade?.ResolutionNotes
                        });
                    }
                }

                return result.OrderBy(r => r.StudentCode).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<BaseResponse<List<SubmissionDetailExportResponse>>>
     GetAllSubmissionDetailsForExportAsync(int assignmentId)
        {
            try
            {
                // Lấy assignment + rubric + course + class
                var assignment = await _context.Assignments
                    .Include(a => a.Rubric)
                        .ThenInclude(r => r.Criteria)
                    .Include(a => a.CourseInstance)
                        .ThenInclude(ci => ci.Course)
                    .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

                if (assignment == null)
                {
                    return new BaseResponse<List<SubmissionDetailExportResponse>>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                // 🔥 Lấy đúng sinh viên đã nộp bài
                var submissions = await _context.Submissions
                    .Where(s => s.AssignmentId == assignmentId)   // chỉ trong assignment
                    .Include(s => s.User)                         // lấy user
                    .Include(s => s.ReviewAssignments)
                        .ThenInclude(ra => ra.Reviews)
                            .ThenInclude(r => r.CriteriaFeedbacks)
                    .ToListAsync();

                var resultList = new List<SubmissionDetailExportResponse>();

                foreach (var submission in submissions)
                {
                    // Review của giảng viên (có thể null)
                    var instructorReview = submission.ReviewAssignments
                        .SelectMany(ra => ra.Reviews)
                        .FirstOrDefault(r => r.ReviewType == "Instructor");

                    // Criteria + score mapping
                    var criteriaScores = assignment.Rubric.Criteria
                        .Select(c =>
                        {
                            var fb = instructorReview?
                                .CriteriaFeedbacks?
                                .FirstOrDefault(cf => cf.CriteriaId == c.CriteriaId);

                            return new SubmissionCriteriaScoreExport
                            {
                                CriteriaId = c.CriteriaId,
                                CriteriaName = c.Title,
                                Weight = c.Weight,
                                Score = fb?.ScoreAwarded,
                                Feedback = fb?.Feedback
                            };
                        })
                        .ToList();

                    // Build DTO
                    resultList.Add(new SubmissionDetailExportResponse
                    {
                        // Student
                        UserId = submission.UserId,
                        UserName = submission.User?.UserName,
                        StudentCode = submission.User?.StudentCode,

                        // Assignment
                        AssignmentId = assignment.AssignmentId,
                        AssignmentName = assignment.Title,
                        CourseName = assignment.CourseInstance?.Course?.CourseName,
                        ClassName = assignment.CourseInstance?.SectionCode,

                        // Submission
                        SubmissionId = submission.SubmissionId,
                        SubmittedAt = submission.SubmittedAt,
                        FinalScore = submission.FinalScore,
                        FileName = submission.FileName,
                        FileUrl = submission.FileUrl,
                        Feedback = submission.Feedback,

                        // Criteria details
                        CriteriaScores = criteriaScores
                    });
                }

                return new BaseResponse<List<SubmissionDetailExportResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    resultList
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SubmissionDetailExportResponse>>(
                    $"Error: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }


        //public async Task<BaseResponse<GradeSubmissionResponse>> ImportGradeAsync(ImportGradeRequest request)
        //{
        //    try
        //    {
        //        // 1️⃣ Lấy submission
        //        var submission = await _context.Submissions
        //            .Include(s => s.User)
        //            .Include(s => s.Assignment)
        //                .ThenInclude(a => a.CourseInstance)
        //                    .ThenInclude(ci => ci.Course)
        //            .Include(s => s.Assignment)
        //                .ThenInclude(a => a.Rubric)
        //                    .ThenInclude(r => r.Criteria)
        //            .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId);

        //        if (submission == null)
        //            return new BaseResponse<GradeSubmissionResponse>("Submission not found", StatusCodeEnum.NotFound_404, null);

        //        var assignment = submission.Assignment;

        //        // 🔥 Nếu assignment đã publish → yêu cầu regrade
        //        if (assignment.Status == "GradesPublished")
        //        {
        //            var latestRegrade = await _context.RegradeRequests
        //                .Where(r => r.SubmissionId == submission.SubmissionId)
        //                .OrderByDescending(r => r.RequestedAt)
        //                .FirstOrDefaultAsync();

        //            if (latestRegrade == null || latestRegrade.Status != "Approved")
        //                return new BaseResponse<GradeSubmissionResponse>(
        //                    "Cannot regrade: grades published and no approved regrade request.",
        //                    StatusCodeEnum.Forbidden_403,
        //                    null
        //                );

        //            // Lưu old score (chỉ 1 lần)
        //            if (submission.OldScore == null)
        //                submission.OldScore = submission.FinalScore;
        //        }

        //        // 2️⃣ Xóa feedback cũ của Instructor
        //        var oldReviews = await _context.ReviewAssignments
        //            .Where(ra => ra.SubmissionId == submission.SubmissionId && ra.ReviewerUserId == request.InstructorId)
        //            .SelectMany(ra => ra.Reviews)
        //            .Where(r => r.ReviewType == "Instructor")
        //            .ToListAsync();

        //        foreach (var rv in oldReviews)
        //        {
        //            var oldFeedbacks = await _context.CriteriaFeedbacks.Where(cf => cf.ReviewId == rv.ReviewId).ToListAsync();
        //            _context.CriteriaFeedbacks.RemoveRange(oldFeedbacks);
        //            _context.Reviews.Remove(rv);
        //        }

        //        // 3️⃣ Tạo ReviewAssignment nếu chưa có
        //        var raInstructor = await _context.ReviewAssignments
        //            .FirstOrDefaultAsync(x => x.SubmissionId == submission.SubmissionId && x.ReviewerUserId == request.InstructorId);

        //        if (raInstructor == null)
        //        {
        //            raInstructor = new ReviewAssignment
        //            {
        //                SubmissionId = submission.SubmissionId,
        //                ReviewerUserId = request.InstructorId,
        //                AssignedAt = DateTime.UtcNow.AddHours(7),
        //                Deadline = DateTime.UtcNow.AddHours(7).AddDays(7),
        //                Status = "Completed",
        //                IsAIReview = false
        //            };
        //            _context.ReviewAssignments.Add(raInstructor);
        //            await _context.SaveChangesAsync();
        //        }

        //        // 4️⃣ Tạo review mới
        //        var review = new Review
        //        {
        //            ReviewAssignmentId = raInstructor.ReviewAssignmentId,
        //            ReviewedAt = DateTime.UtcNow.AddHours(7),
        //            ReviewType = "Instructor",
        //            GeneralFeedback = "Imported grading",
        //            FeedbackSource = "Import"
        //        };

        //        _context.Reviews.Add(review);
        //        await _context.SaveChangesAsync();

        //        // 5️⃣ Lưu điểm theo tiêu chí
        //        decimal totalScore = 0;
        //        decimal totalWeight = 0;

        //        foreach (var item in request.CriteriaScores)
        //        {
        //            var criteria = assignment.Rubric.Criteria.FirstOrDefault(c => c.CriteriaId == item.CriteriaId);
        //            if (criteria == null) continue;

        //            var weight = criteria.Weight > 0 ? criteria.Weight : 1;
        //            totalScore += (item.Score ?? 0) * weight;
        //            totalWeight += weight;

        //            var fb = new CriteriaFeedback
        //            {
        //                ReviewId = review.ReviewId,
        //                CriteriaId = criteria.CriteriaId,
        //                ScoreAwarded = item.Score,
        //                Feedback = item.Feedback,
        //                FeedbackSource = "Import"
        //            };
        //            _context.CriteriaFeedbacks.Add(fb);
        //        }

        //        decimal instructorScore = totalWeight > 0
        //            ? Math.Round(totalScore / totalWeight, 2)
        //            : 0m;

        //        review.OverallScore = instructorScore;

        //        // 6️⃣ Lấy điểm_peer_avg
        //        var peerAvg = await _reviewAssignmentRepository.GetPeerAverageScoreBySubmissionIdAsync(submission.SubmissionId)
        //                     ?? 0m;
        //        bool noPeer = peerAvg == 0;

        //        // 7️⃣ Chuẩn hóa trọng số
        //        var instructorWeight = assignment.InstructorWeight;
        //        var peerWeight = assignment.PeerWeight;

        //        if (instructorWeight + peerWeight == 0)
        //        {
        //            instructorWeight = 50;
        //            peerWeight = 50;
        //        }
        //        else if (instructorWeight + peerWeight != 100)
        //        {
        //            var total = instructorWeight + peerWeight;
        //            instructorWeight = (instructorWeight / total) * 100;
        //            peerWeight = (peerWeight / total) * 100;
        //        }

        //        decimal instructorScoreNorm = instructorScore;
        //        decimal peerScoreNorm = peerAvg / 10;

        //        // 8️⃣ Tính final score
        //        decimal finalScore = noPeer
        //            ? instructorScoreNorm
        //            : Math.Round(
        //                (instructorScoreNorm * instructorWeight / 100) +
        //                (peerScoreNorm * peerWeight / 100),
        //                2);

        //        decimal finalBeforePenalty = finalScore;

        //        // 9️⃣ Penalty missing review
        //        int requiredReviews = assignment.NumPeerReviewsRequired;
        //        int completedReviews = await _context.ReviewAssignments
        //            .Where(ra => ra.SubmissionId == submission.SubmissionId)
        //            .SelectMany(ra => ra.Reviews)
        //            .CountAsync(r => r.ReviewedAt != null);

        //        int missingReviews = Math.Max(0, requiredReviews - completedReviews);
        //        decimal penaltyPer = assignment.MissingReviewPenalty ?? 0;
        //        decimal totalPenalty = missingReviews * penaltyPer;

        //        if (totalPenalty > 0)
        //            finalScore = Math.Max(0, finalScore - totalPenalty);

        //        //  🔟 Update submission
        //        submission.InstructorScore = instructorScoreNorm;
        //        submission.PeerAverageScore = peerScoreNorm;
        //        submission.FinalScore = finalScore;
        //        submission.GradedAt = DateTime.UtcNow.AddHours(7);
        //        submission.Status = "Graded";

        //        await _context.SaveChangesAsync();

        //        var latestRegradeAfterUpdate = await _context.RegradeRequests
        //        .Where(r => r.SubmissionId == submission.SubmissionId)
        //        .OrderByDescending(r => r.RequestedAt)
        //        .FirstOrDefaultAsync();

        //        // ️⃣1️⃣ Tạo response y hệt GradeSubmissionResponse
        //        var response = new GradeSubmissionResponse
        //        {
        //            SubmissionId = submission.SubmissionId,
        //            AssignmentId = submission.AssignmentId,
        //            UserId = submission.UserId,
        //            InstructorScore = instructorScore,
        //            PeerAverageScore = peerScoreNorm,
        //            FinalScore = finalScore,
        //            FinalScoreBeforePenalty = finalBeforePenalty,
        //            MissingReviews = missingReviews,
        //            MissingReviewPenaltyPerReview = penaltyPer,
        //            MissingReviewPenaltyTotal = totalPenalty,
        //            OldScore = submission.OldScore,
        //            Feedback = submission.Feedback,
        //            GradedAt = submission.GradedAt,
        //            FileUrl = submission.FileUrl,
        //            FileName = submission.FileName,
        //            Status = submission.Status,
        //            RegradeRequestStatus = latestRegradeAfterUpdate?.Status,
        //            StudentName = submission.User?.UserName,
        //            CourseName = assignment?.CourseInstance?.Course?.CourseName,
        //            AssignmentTitle = assignment?.Title
        //        };


        //        return new BaseResponse<GradeSubmissionResponse>(
        //            "Imported grading successfully",
        //            StatusCodeEnum.OK_200,
        //            response
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<GradeSubmissionResponse>(
        //            $"Error importing grade: {ex.Message}",
        //            StatusCodeEnum.InternalServerError_500,
        //            null
        //        );
        //    }
        //}

        public async Task<BaseResponse<object>> ImportGradesFromExcelAsync(IFormFile file)
        {
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var package = new ExcelPackage(stream);
                var ws = package.Workbook.Worksheets.FirstOrDefault();

                if (ws == null)
                {
                    return new BaseResponse<object>(
                        "Invalid Excel file",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // ==============================
                // CONFIG
                // ==============================
                int headerRow = 1;
                int startRow = 2;

                int submissionIdCol = 3;
                int instructorIdCol = 5;
                int startCriteriaCol = 7;
                int finalFeedbackCol = ws.Dimension.End.Column;

                var excelErrors = new List<ExcelValidationError>();

                // ==============================
                // LOAD RUBRIC (FROM FIRST ROW)
                // ==============================
                if (!int.TryParse(ws.Cells[startRow, submissionIdCol].Text, out int firstSubmissionId))
                {
                    excelErrors.Add(new ExcelValidationError
                    {
                        Row = startRow,
                        Column = "SubmissionId",
                        Message = "SubmissionId is required and must be a number"
                    });

                    return new BaseResponse<object>(
                        "Excel validation failed",
                        StatusCodeEnum.BadRequest_400,
                        excelErrors
                    );
                }

                var firstSubmission = await _context.Submissions
                    .Include(s => s.Assignment)
                        .ThenInclude(a => a.Rubric)
                            .ThenInclude(r => r.Criteria)
                    .FirstOrDefaultAsync(s => s.SubmissionId == firstSubmissionId);

                if (firstSubmission?.Assignment?.Rubric == null)
                {
                    excelErrors.Add(new ExcelValidationError
                    {
                        Row = startRow,
                        Column = "SubmissionId",
                        Message = "Assignment or rubric not found for this submission"
                    });

                    return new BaseResponse<object>(
                        "Excel validation failed",
                        StatusCodeEnum.NotFound_404,
                        excelErrors
                    );
                }

                var criteriaMap = firstSubmission.Assignment.Rubric.Criteria
                    .ToDictionary(c => c.Title.Trim(), c => c.CriteriaId);

                // ==============================
                // PHASE 1️⃣ VALIDATE
                // ==============================
                int row = startRow;

                while (!string.IsNullOrWhiteSpace(ws.Cells[row, submissionIdCol].Text))
                {
                    // ---- SubmissionId
                    if (!int.TryParse(ws.Cells[row, submissionIdCol].Text, out _))
                    {
                        excelErrors.Add(new ExcelValidationError
                        {
                            Row = row,
                            Column = "SubmissionId",
                            Message = "SubmissionId is required and must be a number"
                        });
                    }

                    // ---- InstructorId
                    if (!int.TryParse(ws.Cells[row, instructorIdCol].Text, out _))
                    {
                        excelErrors.Add(new ExcelValidationError
                        {
                            Row = row,
                            Column = "InstructorId",
                            Message = "InstructorId is required and must be a number"
                        });
                    }

                    // ---- Criteria
                    for (int col = startCriteriaCol; col < finalFeedbackCol; col += 2)
                    {
                        string rawHeader = ws.Cells[headerRow, col].Text?.Trim();
                        if (string.IsNullOrEmpty(rawHeader)) continue;

                        string criteriaTitle = rawHeader;
                        int idx = rawHeader.LastIndexOf("(");
                        if (idx > 0)
                            criteriaTitle = rawHeader.Substring(0, idx).Trim();

                        if (!criteriaMap.ContainsKey(criteriaTitle))
                        {
                            excelErrors.Add(new ExcelValidationError
                            {
                                Row = row,
                                Column = rawHeader,
                                Message = "Criteria does not exist in rubric"
                            });
                            continue;
                        }

                        // Score
                        var scoreText = ws.Cells[row, col].Text;
                        if (string.IsNullOrWhiteSpace(scoreText))
                        {
                            excelErrors.Add(new ExcelValidationError
                            {
                                Row = row,
                                Column = rawHeader,
                                Message = "Score is required"
                            });
                        }
                        else if (!decimal.TryParse(scoreText, out _))
                        {
                            excelErrors.Add(new ExcelValidationError
                            {
                                Row = row,
                                Column = rawHeader,
                                Message = "Score must be a number"
                            });
                        }

                        // Feedback
                        var feedbackText = ws.Cells[row, col + 1].Text;
                        if (string.IsNullOrWhiteSpace(feedbackText))
                        {
                            excelErrors.Add(new ExcelValidationError
                            {
                                Row = row,
                                Column = $"{rawHeader} Feedback",
                                Message = "Feedback is required"
                            });
                        }
                    }

                    // ---- Final Feedback
                    if (string.IsNullOrWhiteSpace(ws.Cells[row, finalFeedbackCol].Text))
                    {
                        excelErrors.Add(new ExcelValidationError
                        {
                            Row = row,
                            Column = "Final Feedback",
                            Message = "Final Feedback is required"
                        });
                    }

                    row++;
                }

                // ❌ STOP nếu có lỗi
                if (excelErrors.Any())
                {
                    return new BaseResponse<object>(
                        "Excel validation failed",
                        StatusCodeEnum.BadRequest_400,
                        excelErrors
                    );
                }

                // ==============================
                // PHASE 2️⃣ IMPORT
                // ==============================
                var results = new List<GradeSubmissionResponse>();
                row = startRow;

                while (!string.IsNullOrWhiteSpace(ws.Cells[row, submissionIdCol].Text))
                {
                    var req = new ImportGradeRequest
                    {
                        SubmissionId = int.Parse(ws.Cells[row, submissionIdCol].Text),
                        InstructorId = int.Parse(ws.Cells[row, instructorIdCol].Text),
                        FinalFeedback = ws.Cells[row, finalFeedbackCol].Text,
                        CriteriaScores = new List<ImportCriteriaScore>()
                    };

                    for (int col = startCriteriaCol; col < finalFeedbackCol; col += 2)
                    {
                        string rawHeader = ws.Cells[headerRow, col].Text.Trim();
                        string criteriaTitle = rawHeader.Contains("(")
                            ? rawHeader.Substring(0, rawHeader.LastIndexOf("(")).Trim()
                            : rawHeader;

                        req.CriteriaScores.Add(new ImportCriteriaScore
                        {
                            CriteriaId = criteriaMap[criteriaTitle],
                            Score = decimal.Parse(ws.Cells[row, col].Text),
                            Feedback = ws.Cells[row, col + 1].Text
                        });
                    }

                    var result = await ImportSingleSubmissionAsync(req);
                    if (result.StatusCode != StatusCodeEnum.OK_200)
                    {
                        return new BaseResponse<object>(
                            $"Import failed at submission {req.SubmissionId}: {result.Message}",
                            result.StatusCode,
                            null
                        );
                    }

                    results.Add(result.Data);
                    row++;
                }

                return new BaseResponse<object>(
                    "Imported grades successfully",
                    StatusCodeEnum.OK_200,
                    results
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<object>(
                    $"Error importing Excel: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }







        public async Task<BaseResponse<GradeSubmissionResponse>> ImportSingleSubmissionAsync(
     ImportGradeRequest request)
        {
            try
            {
                // =========================
                // 1️⃣ Validate
                // =========================
                if (request.CriteriaScores == null || !request.CriteriaScores.Any())
                {
                    return new BaseResponse<GradeSubmissionResponse>(
                        "No criteria scores provided",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                // =========================
                // 2️⃣ Load submission + assignment + rubric
                // =========================
                var submission = await _context.Submissions
                    .Include(s => s.User)
                    .Include(s => s.Assignment)
                        .ThenInclude(a => a.CourseInstance)
                            .ThenInclude(ci => ci.Course)
                    .Include(s => s.Assignment)
                        .ThenInclude(a => a.Rubric)
                            .ThenInclude(r => r.Criteria)
                    .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId);

                if (submission == null)
                {
                    return new BaseResponse<GradeSubmissionResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                var assignment = submission.Assignment;

                // =========================
                // 3️⃣ Regrade rule (GIỐNG GradeSubmissionAsync)
                // =========================
                if (assignment.Status == "GradesPublished")
                {
                    var latestRegrade = await _context.RegradeRequests
                        .Where(r => r.SubmissionId == submission.SubmissionId)
                        .OrderByDescending(r => r.RequestedAt)
                        .FirstOrDefaultAsync();

                    if (latestRegrade == null || latestRegrade.Status != "Approved")
                    {
                        return new BaseResponse<GradeSubmissionResponse>(
                            "Cannot regrade: grades already published and no approved regrade request found.",
                            StatusCodeEnum.Forbidden_403,
                            null
                        );
                    }

                    if (submission.OldScore == null)
                        submission.OldScore = submission.FinalScore;
                }

                // =========================
                // 4️⃣ Remove old instructor reviews
                // =========================
                var oldReviews = await _context.ReviewAssignments
                    .Where(ra =>
                        ra.SubmissionId == submission.SubmissionId &&
                        ra.ReviewerUserId == request.InstructorId &&
                        !ra.IsAIReview)
                    .SelectMany(ra => ra.Reviews)
                    .Where(r => r.ReviewType == "Instructor")
                    .ToListAsync();

                foreach (var rv in oldReviews)
                {
                    var oldCriteria = await _context.CriteriaFeedbacks
                        .Where(cf => cf.ReviewId == rv.ReviewId)
                        .ToListAsync();

                    _context.CriteriaFeedbacks.RemoveRange(oldCriteria);
                    _context.Reviews.Remove(rv);
                }

                await _context.SaveChangesAsync();

                // =========================
                // 5️⃣ ReviewAssignment
                // =========================
                var raInstructor = await _context.ReviewAssignments.FirstOrDefaultAsync(ra =>
                    ra.SubmissionId == submission.SubmissionId &&
                    ra.ReviewerUserId == request.InstructorId &&
                    !ra.IsAIReview);

                if (raInstructor == null)
                {
                    raInstructor = new ReviewAssignment
                    {
                        SubmissionId = submission.SubmissionId,
                        ReviewerUserId = request.InstructorId,
                        AssignedAt = DateTime.UtcNow.AddHours(7),
                        Deadline = DateTime.UtcNow.AddHours(7).AddDays(7),
                        Status = "Completed",
                        IsAIReview = false
                    };

                    _context.ReviewAssignments.Add(raInstructor);
                    await _context.SaveChangesAsync();
                }

                // =========================
                // 6️⃣ Review (FINAL FEEDBACK)
                // =========================
                var review = new Review
                {
                    ReviewAssignmentId = raInstructor.ReviewAssignmentId,
                    ReviewedAt = DateTime.UtcNow.AddHours(7),
                    ReviewType = "Instructor",
                    FeedbackSource = "Instructor",
                    GeneralFeedback = request.FinalFeedback
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // =========================
                // 7️⃣ Calculate instructor score (rubric)
                // =========================
                decimal totalScore = 0m;
                decimal totalWeight = 0m;

                foreach (var cs in request.CriteriaScores)
                {
                    var criteria = assignment.Rubric.Criteria
                        .FirstOrDefault(c => c.CriteriaId == cs.CriteriaId);

                    if (criteria == null) continue;

                    if (cs.Score < 0 || cs.Score > 10)
                    {
                        return new BaseResponse<GradeSubmissionResponse>(
                            $"Score for criteria {cs.CriteriaId} must be between 0 and 10",
                            StatusCodeEnum.BadRequest_400,
                            null
                        );
                    }

                    var weight = criteria.Weight > 0 ? criteria.Weight : 1;
                    totalScore += (cs.Score ?? 0) * weight;
                    totalWeight += weight;

                    _context.CriteriaFeedbacks.Add(new CriteriaFeedback
                    {
                        ReviewId = review.ReviewId,
                        CriteriaId = cs.CriteriaId,
                        ScoreAwarded = cs.Score,
                        Feedback = string.IsNullOrWhiteSpace(cs.Feedback)
                            ? "Imported grading"
                            : cs.Feedback,
                        FeedbackSource = "Instructor"
                    });
                }

                if (totalWeight == 0)
                {
                    return new BaseResponse<GradeSubmissionResponse>(
                        "Invalid rubric configuration",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                decimal instructorScore = Math.Round(totalScore / totalWeight, 2);
                review.OverallScore = instructorScore;

                await _context.SaveChangesAsync();

                // =========================
                // 8️⃣ Peer + weight normalize
                // =========================
                var peerAvg = await _reviewAssignmentRepository
                    .GetPeerAverageScoreBySubmissionIdAsync(submission.SubmissionId) ?? 0m;

                bool noPeer = peerAvg == 0m;

                decimal instructorWeight = assignment.InstructorWeight;
                decimal peerWeight = assignment.PeerWeight;

                if (instructorWeight + peerWeight == 0)
                {
                    instructorWeight = 50;
                    peerWeight = 50;
                }
                else if (instructorWeight + peerWeight != 100)
                {
                    var sum = instructorWeight + peerWeight;
                    instructorWeight = instructorWeight / sum * 100;
                    peerWeight = peerWeight / sum * 100;
                }

                decimal instructorNorm = instructorScore;
                decimal peerNorm = peerAvg;

                decimal finalScore = noPeer
                    ? instructorNorm
                    : Math.Round(
                        instructorNorm * instructorWeight / 100 +
                        peerNorm * peerWeight / 100, 2);

                // ⭐ CHỈ DÙNG TRONG RESPONSE
                decimal finalScoreBeforePenalty = finalScore;

                // =========================
                // 9️⃣ Missing review penalty
                // =========================
                int requiredReviews = assignment.NumPeerReviewsRequired;
                int completedReviews = await _context.ReviewAssignments
                    .Where(ra => ra.SubmissionId == submission.SubmissionId)
                    .SelectMany(ra => ra.Reviews)
                    .CountAsync(r => r.ReviewedAt.HasValue);

                int missingReviews = Math.Max(0, requiredReviews - completedReviews);

                decimal penaltyPerReview = assignment.MissingReviewPenalty ?? 0m;
                decimal totalPenalty = missingReviews * penaltyPerReview;

                if (totalPenalty > 0)
                    finalScore = Math.Max(0, finalScore - totalPenalty);

                // =========================
                // 🔟 Update submission
                // =========================
                submission.InstructorScore = instructorNorm;
                submission.PeerAverageScore = peerNorm;
                submission.FinalScore = finalScore;
                submission.GradedAt = DateTime.UtcNow.AddHours(7);
                submission.Status = "Graded";
                submission.IsPublic = true;
                submission.Feedback = request.FinalFeedback;

                await _context.SaveChangesAsync();

                var latestRegradeReq = await _context.RegradeRequests
                    .Where(r => r.SubmissionId == submission.SubmissionId)
                    .OrderByDescending(r => r.RequestedAt)
                    .FirstOrDefaultAsync();

                // =========================
                // 1️⃣1️⃣ Response
                // =========================
                return new BaseResponse<GradeSubmissionResponse>(
                    "Imported grading successfully",
                    StatusCodeEnum.OK_200,
                    new GradeSubmissionResponse
                    {
                        SubmissionId = submission.SubmissionId,
                        AssignmentId = submission.AssignmentId,
                        UserId = submission.UserId,
                        InstructorScore = instructorNorm,
                        PeerAverageScore = peerNorm,
                        FinalScoreBeforePenalty = finalScoreBeforePenalty, // ⭐ OK
                        FinalScore = finalScore,
                        MissingReviews = missingReviews,
                        MissingReviewPenaltyPerReview = penaltyPerReview,
                        MissingReviewPenaltyTotal = totalPenalty,
                        OldScore = submission.OldScore,
                        Feedback = submission.Feedback,
                        GradedAt = submission.GradedAt,
                        Status = submission.Status,
                        IsPublic = submission.IsPublic,
                        FileUrl = submission.FileUrl,
                        FileName = submission.FileName,
                        OriginalFileName = submission.OriginalFileName,
                        RegradeRequestStatus = latestRegradeReq?.Status,
                        StudentName = submission.User?.UserName,
                        StudentEmail = submission.User?.Email,
                        CourseName = submission.Assignment?.CourseInstance?.Course?.CourseName,
                        AssignmentTitle = submission.Assignment?.Title
                    }
                );
            }
            catch (Exception ex)
            {
                return new BaseResponse<GradeSubmissionResponse>(
                    $"Error importing grading: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }


        public async Task<BaseResponse<OverrideFinalScoreResponse>> OverrideFinalScoreAsync(
     OverrideFinalScoreRequest request)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<OverrideFinalScoreResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<OverrideFinalScoreResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null
                    );
                }

                if (assignment.Status != AssignmentStatusEnum.GradesPublished.ToString())
                {
                    return new BaseResponse<OverrideFinalScoreResponse>(
                        "Cannot override final score before grades are published",
                        StatusCodeEnum.Forbidden_403,
                        null
                    );
                }

                var approvedRegrade = await _context.RegradeRequests
                    .AnyAsync(r =>
                        r.SubmissionId == submission.SubmissionId &&
                        r.Status == "Approved");

                if (!approvedRegrade)
                {
                    return new BaseResponse<OverrideFinalScoreResponse>(
                        "No approved regrade request found for this submission",
                        StatusCodeEnum.Forbidden_403,
                        null
                    );
                }

                if (request.NewFinalScore < 0 || request.NewFinalScore > 10)
                {
                    return new BaseResponse<OverrideFinalScoreResponse>(
                        "Final score must be between 0 and 10",
                        StatusCodeEnum.BadRequest_400,
                        null
                    );
                }

                if (submission.OldScore == null)
                {
                    submission.OldScore = submission.FinalScore;
                }

                submission.FinalScore = Math.Round(request.NewFinalScore, 2);
                submission.GradedAt = DateTime.UtcNow.AddHours(7);
                submission.Status = "Graded";

                _logger.LogWarning(
                    "FINAL SCORE OVERRIDDEN | SubmissionId={SubmissionId} | Old={Old} | New={New} | By={InstructorId}",
                    submission.SubmissionId,
                    submission.OldScore,
                    submission.FinalScore,
                    request.InstructorId
                );

                await _context.SaveChangesAsync();

                var response = new OverrideFinalScoreResponse
                {
                    SubmissionId = submission.SubmissionId,
                    OldFinalScore = submission.OldScore,
                    NewFinalScore = submission.FinalScore.Value,
                    OverriddenAt = submission.GradedAt.Value
                };

                _logger.LogInformation(
                    "Final score overridden. SubmissionId={SubmissionId}, Old={Old}, New={New}",
                    submission.SubmissionId,
                    submission.OldScore,
                    submission.FinalScore
                );

                return new BaseResponse<OverrideFinalScoreResponse>(
                    "Final score overridden successfully",
                    StatusCodeEnum.OK_200,
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error overriding final score");
                return new BaseResponse<OverrideFinalScoreResponse>(
                    "An error occurred while overriding final score",
                    StatusCodeEnum.InternalServerError_500,
                    null
                );
            }
        }

        private string GeneratePreviewUrl(string fileUrl)
        {
            string encodedUrl = Uri.EscapeDataString(fileUrl);
            string extension = Path.GetExtension(fileUrl).ToLower();

            if (extension == ".pdf")
            {
                return $"https://docs.google.com/viewer?url={encodedUrl}&embedded=true";
            }

            if (new[] { ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt" }.Contains(extension))
            {
                return $"https://view.officeapps.live.com/op/view.aspx?src={encodedUrl}";
            }

            return $"https://docs.google.com/viewer?url={encodedUrl}&embedded=true";
        }



    }
}