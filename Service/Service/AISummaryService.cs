using BussinessObject.Models;
using Microsoft.Extensions.Logging;
using Repository.IRepository;
using Repository.Repository;
using Service.Interface;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.AISummary;
using Service.RequestAndResponse.Response.AISummary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service
{
    public class AISummaryService : IAISummaryService
    {
        private readonly IAISummaryRepository _aiSummaryRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentTextExtractor _documentTextExtractor;
        private readonly IFileStorageService _fileStorageService;
        private readonly IGenAIService _genAIService;
        private readonly ILogger<AISummaryService> _logger;
        private readonly IRubricRepository _rubricRepository;
        private readonly ICriteriaRepository _criteriaRepository;
        private readonly ISystemConfigService _systemConfigService;
        public AISummaryService(
            IAISummaryRepository aiSummaryRepository,
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository, ILogger<AISummaryService> logger,
            IDocumentTextExtractor documentTextExtractor,
    IFileStorageService fileStorageService,
    IGenAIService genAIService, IRubricRepository rubricRepository,
    ICriteriaRepository criteriaRepository, ISystemConfigService systemConfigService)
        {
            _aiSummaryRepository = aiSummaryRepository;
            _submissionRepository = submissionRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _logger = logger;
            _documentTextExtractor = documentTextExtractor;
            _fileStorageService = fileStorageService;
            _genAIService = genAIService;
            _rubricRepository = rubricRepository;
            _criteriaRepository = criteriaRepository;
            _systemConfigService = systemConfigService;
        }

        public async Task<BaseResponse<AISummaryResponse>> CreateAISummaryAsync(CreateAISummaryRequest request)
        {
            try
            {
                // Validate submission exists
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<AISummaryResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Check if summary already exists for this submission and type
                var existing = await _aiSummaryRepository.ExistsAsync(request.SubmissionId, request.SummaryType);
                if (existing)
                {
                    return new BaseResponse<AISummaryResponse>(
                        $"AI summary already exists for submission {request.SubmissionId} with type {request.SummaryType}",
                        StatusCodeEnum.Conflict_409,
                        null);
                }
                var content = request.Content;
                if (content.Length > 2000)
                {
                    content = content.Substring(0, 2000);
                }

                var aiSummary = new AISummary
                {
                    SubmissionId = request.SubmissionId,
                    Content = content,
                    SummaryType = request.SummaryType,
                    GeneratedAt = DateTime.UtcNow
                };

                await _aiSummaryRepository.AddAsync(aiSummary);

                var response = await MapToResponse(aiSummary);
                return new BaseResponse<AISummaryResponse>(
                    "AI summary created successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AISummaryResponse>(
                    $"Error creating AI summary: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<AISummaryResponse>> UpdateAISummaryAsync(UpdateAISummaryRequest request)
        {
            try
            {
                var aiSummary = await _aiSummaryRepository.GetByIdAsync(request.SummaryId);
                if (aiSummary == null)
                {
                    return new BaseResponse<AISummaryResponse>(
                        "AI summary not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                if (!string.IsNullOrEmpty(request.Content))
                {
                    var content = request.Content;
                    if (content.Length > 2000)
                    {
                        content = content.Substring(0, 2000);
                    }
                    aiSummary.Content = content;
                }


                if (!string.IsNullOrEmpty(request.SummaryType))
                    aiSummary.SummaryType = request.SummaryType;

                await _aiSummaryRepository.UpdateAsync(aiSummary);

                var response = await MapToResponse(aiSummary);
                return new BaseResponse<AISummaryResponse>(
                    "AI summary updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AISummaryResponse>(
                    $"Error updating AI summary: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteAISummaryAsync(int summaryId)
        {
            try
            {
                var aiSummary = await _aiSummaryRepository.GetByIdAsync(summaryId);
                if (aiSummary == null)
                {
                    return new BaseResponse<bool>(
                        "AI summary not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                await _aiSummaryRepository.DeleteAsync(aiSummary);
                return new BaseResponse<bool>(
                    "AI summary deleted successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error deleting AI summary: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<AISummaryResponse>> GetAISummaryByIdAsync(int summaryId)
        {
            try
            {
                var aiSummary = await _aiSummaryRepository.GetByIdAsync(summaryId);
                if (aiSummary == null)
                {
                    return new BaseResponse<AISummaryResponse>(
                        "AI summary not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToResponse(aiSummary);
                return new BaseResponse<AISummaryResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AISummaryResponse>(
                    $"Error retrieving AI summary: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<AISummaryResponse>>> GetAISummariesBySubmissionAsync(int submissionId)
        {
            try
            {
                var aiSummaries = await _aiSummaryRepository.GetBySubmissionIdAsync(submissionId);
                var responses = new List<AISummaryResponse>();

                foreach (var summary in aiSummaries)
                {
                    responses.Add(await MapToResponse(summary));
                }

                return new BaseResponse<List<AISummaryResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AISummaryResponse>>(
                    $"Error retrieving AI summaries: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<AISummaryResponse>>> GetAISummariesByTypeAsync(string summaryType)
        {
            try
            {
                var aiSummaries = await _aiSummaryRepository.GetBySummaryTypeAsync(summaryType);
                var responses = new List<AISummaryResponse>();

                foreach (var summary in aiSummaries)
                {
                    responses.Add(await MapToResponse(summary));
                }

                return new BaseResponse<List<AISummaryResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AISummaryResponse>>(
                    $"Error retrieving AI summaries: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<AISummaryResponse>> GetAISummaryBySubmissionAndTypeAsync(int submissionId, string summaryType)
        {
            try
            {
                var aiSummaries = await _aiSummaryRepository.GetBySubmissionAndTypeAsync(submissionId, summaryType);
                var aiSummary = aiSummaries.FirstOrDefault();

                if (aiSummary == null)
                {
                    return new BaseResponse<AISummaryResponse>(
                        $"AI summary not found for submission {submissionId} with type {summaryType}",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToResponse(aiSummary);
                return new BaseResponse<AISummaryResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<AISummaryResponse>(
                    $"Error retrieving AI summary: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<AISummaryGenerationResponse>> GenerateAISummaryAsync(GenerateAISummaryRequest request)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<AISummaryGenerationResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                _logger.LogInformation($"Processing submission {submission.SubmissionId}, FileName: {submission.FileName}, FileUrl: {submission.FileUrl}");

                // Kiểm tra nếu đã tồn tại
                if (!request.ForceRegenerate)
                {
                    var existingSummaries = await _aiSummaryRepository.GetBySubmissionAndTypeAsync(request.SubmissionId, request.SummaryType);
                    var existingSummary = existingSummaries.FirstOrDefault();
                    if (existingSummary != null)
                    {
                        var response = new AISummaryGenerationResponse
                        {
                            SummaryId = existingSummary.SummaryId,
                            Content = existingSummary.Content,
                            SummaryType = existingSummary.SummaryType,
                            GeneratedAt = existingSummary.GeneratedAt,
                            WasGenerated = false
                        };
                        return new BaseResponse<AISummaryGenerationResponse>("Existing summary returned", StatusCodeEnum.OK_200, response);
                    }
                }

                // SỬA: Sử dụng FileUrl trực tiếp thay vì extract path
                if (string.IsNullOrEmpty(submission.FileUrl))
                {
                    return new BaseResponse<AISummaryGenerationResponse>(
                        "File URL not found in submission",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                _logger.LogInformation($"Using FileUrl directly: {submission.FileUrl}");

                // Tải file trực tiếp từ FileUrl
                using var fileStream = await _fileStorageService.GetFileStreamAsync(submission.FileUrl);
                if (fileStream == null)
                {
                    return new BaseResponse<AISummaryGenerationResponse>(
                        "Could not download file from storage using public URL",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Trích xuất text từ file
                var fileName = submission.FileName ?? submission.OriginalFileName;
                var extractedText = await _documentTextExtractor.ExtractTextAsync(fileStream, fileName);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new BaseResponse<AISummaryGenerationResponse>(
                        "No text could be extracted from the file",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                _logger.LogInformation($"Text extracted successfully: {extractedText.Length} characters");

                var maxTokensConfig = await _systemConfigService.GetSystemConfigAsync("AISummaryMaxTokens");
                var maxWordsConfig = await _systemConfigService.GetSystemConfigAsync("AISummaryMaxWords");

                int maxTokens = int.Parse(maxTokensConfig ?? "1000");
                int maxWords = int.Parse(maxWordsConfig ?? "200");

                // Giới hạn text đầu vào
                if (extractedText.Length > maxTokens)
                {
                    extractedText = extractedText.Substring(0, maxTokens) + "... [document truncated]";
                }
                _logger.LogInformation($"Calling AI service for analysis type: {request.SummaryType}");
                var analysisResult = await _genAIService.AnalyzeDocumentAsync(extractedText, request.SummaryType);
                _logger.LogInformation($"AI analysis completed: {analysisResult.Length} characters");
                // Giới hạn số từ đầu ra
                if (analysisResult.Split(' ').Length > maxWords)
                {
                    var words = analysisResult.Split(' ').Take(maxWords).ToArray();
                    analysisResult = string.Join(" ", words) + "...";
                }
                // TRUNCATE CONTENT TO FIT DATABASE COLUMN
                if (analysisResult.Length > 2000)
                {
                    analysisResult = analysisResult.Substring(0, 2000);
                    _logger.LogInformation($"AI analysis result truncated to 2000 characters");
                }

                // Save result
                var aiSummary = new AISummary
                {
                    SubmissionId = request.SubmissionId,
                    Content = analysisResult,
                    SummaryType = request.SummaryType,
                    GeneratedAt = DateTime.UtcNow
                };

                await _aiSummaryRepository.AddAsync(aiSummary);

                var generationResponse = new AISummaryGenerationResponse
                {
                    SummaryId = aiSummary.SummaryId,
                    Content = aiSummary.Content,
                    SummaryType = aiSummary.SummaryType,
                    GeneratedAt = aiSummary.GeneratedAt,
                    WasGenerated = true,
                    Status = "Successfully generated using Gemini AI"
                };

                return new BaseResponse<AISummaryGenerationResponse>(
                    "AI summary generated successfully",
                    StatusCodeEnum.Created_201,
                    generationResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary for submission {SubmissionId}", request.SubmissionId);
                return new BaseResponse<AISummaryGenerationResponse>(
                    $"Error generating AI summary: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<List<AISummaryResponse>>> GetRecentAISummariesAsync(int maxResults = 10)
        {
            try
            {
                var aiSummaries = await _aiSummaryRepository.GetRecentSummariesAsync(maxResults);
                var responses = new List<AISummaryResponse>();

                foreach (var summary in aiSummaries)
                {
                    responses.Add(await MapToResponse(summary));
                }

                return new BaseResponse<List<AISummaryResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<AISummaryResponse>>(
                    $"Error retrieving recent AI summaries: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> GenerateAllSummaryTypesAsync(int submissionId, bool forceRegenerate = false)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(submissionId);
                if (submission == null)
                {
                    return new BaseResponse<bool>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                var summaryTypes = new[] { "Overall", "Strengths", "Weaknesses", "Recommendations", "KeyPoints" };
                var generatedCount = 0;

                foreach (var summaryType in summaryTypes)
                {
                    // Check if already exists and we're not forcing regenerate
                    if (!forceRegenerate)
                    {
                        var existing = await _aiSummaryRepository.ExistsAsync(submissionId, summaryType);
                        if (existing) continue;
                    }

                    var generateRequest = new GenerateAISummaryRequest
                    {
                        SubmissionId = submissionId,
                        SummaryType = summaryType,
                        ForceRegenerate = forceRegenerate
                    };

                    var result = await GenerateAISummaryAsync(generateRequest);
                    if (result.StatusCode == StatusCodeEnum.Created_201)
                    {
                        generatedCount++;
                    }
                }

                return new BaseResponse<bool>(
                    $"Generated {generatedCount} AI summaries for submission {submissionId}",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error generating all summary types: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }
        public async Task<BaseResponse<AISummaryGenerationResponse>> GenerateReviewAsync(GenerateReviewRequest request)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<AISummaryGenerationResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AISummaryGenerationResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Lấy thông tin rubric và criteria
                Rubric rubric = null;
                List<Criteria> criteria = null;
                if (assignment.RubricId.HasValue)
                {
                    rubric = await _rubricRepository.GetByIdAsync(assignment.RubricId.Value);
                    if (rubric != null)
                    {
                        criteria = (await _criteriaRepository.GetByRubricIdAsync(rubric.RubricId)).ToList();
                    }
                }

                _logger.LogInformation($"Processing submission {submission.SubmissionId} for AI review");

                // Tải file và trích xuất text
                using var fileStream = await _fileStorageService.GetFileStreamAsync(submission.FileUrl);
                if (fileStream == null)
                {
                    return new BaseResponse<AISummaryGenerationResponse>(
                        "Could not download file from storage",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var fileName = submission.FileName ?? submission.OriginalFileName;
                var extractedText = await _documentTextExtractor.ExtractTextAsync(fileStream, fileName);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new BaseResponse<AISummaryGenerationResponse>(
                        "No text could be extracted from the file",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Giới hạn độ dài text
                if (extractedText.Length > 12000)
                {
                    extractedText = extractedText.Substring(0, 12000) + "...";
                }

                // Tạo context với thông tin criteria
                var context = BuildEnhancedContext(assignment, rubric, criteria);

                _logger.LogInformation($"Calling AI service for enhanced review generation");

                // Gọi AI service với prompt cải tiến
                var reviewContent = await _genAIService.GenerateEnhancedReviewAsync(extractedText, context, criteria);

                _logger.LogInformation($"Enhanced AI review generated: {reviewContent.Length} characters");

                // Giới hạn độ dài kết quả
                if (reviewContent.Length > 1800)
                {
                    reviewContent = reviewContent.Substring(0, 1800);
                }

                // Xóa review cũ nếu có
                if (request.ReplaceExisting)
                {
                    var existingReviews = await _aiSummaryRepository.GetBySubmissionAndTypeAsync(request.SubmissionId, "EnhancedReview");
                    foreach (var existing in existingReviews)
                    {
                        await _aiSummaryRepository.DeleteAsync(existing);
                    }

                    // Xóa cả các criteria reviews cũ
                    var existingCriteriaReviews = await _aiSummaryRepository.GetBySubmissionAndTypeAsync(request.SubmissionId, "CriteriaReview_");
                    foreach (var existing in existingCriteriaReviews)
                    {
                        await _aiSummaryRepository.DeleteAsync(existing);
                    }
                }

                // Lưu review tổng quát
                var aiReview = new AISummary
                {
                    SubmissionId = request.SubmissionId,
                    Content = reviewContent,
                    SummaryType = "EnhancedReview",
                    GeneratedAt = DateTime.UtcNow
                };

                await _aiSummaryRepository.AddAsync(aiReview);

                // Tạo reviews cho từng criteria nếu có
                if (criteria != null && criteria.Any())
                {
                    foreach (var criterion in criteria)
                    {
                        var criteriaReviewContent = await _genAIService.GenerateCriteriaReviewAsync(extractedText, criterion, context);

                        if (!string.IsNullOrEmpty(criteriaReviewContent) && criteriaReviewContent.Length > 500)
                        {
                            criteriaReviewContent = criteriaReviewContent.Substring(0, 500);
                        }

                        var criteriaReview = new AISummary
                        {
                            SubmissionId = request.SubmissionId,
                            Content = criteriaReviewContent,
                            SummaryType = $"CriteriaReview_{criterion.CriteriaId}",
                            GeneratedAt = DateTime.UtcNow
                        };

                        await _aiSummaryRepository.AddAsync(criteriaReview);
                    }
                }

                var generationResponse = new AISummaryGenerationResponse
                {
                    SummaryId = aiReview.SummaryId,
                    Content = aiReview.Content,
                    SummaryType = aiReview.SummaryType,
                    GeneratedAt = aiReview.GeneratedAt,
                    WasGenerated = true,
                    Status = $"Enhanced review generated successfully with {criteria?.Count ?? 0} criteria reviews"
                };

                return new BaseResponse<AISummaryGenerationResponse>(
                    "AI review generated successfully",
                    StatusCodeEnum.Created_201,
                    generationResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI review for submission {SubmissionId}", request.SubmissionId);
                return new BaseResponse<AISummaryGenerationResponse>(
                    $"Error generating AI review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<EnhancedReviewResponse>> GenerateEnhancedReviewAsync(GenerateReviewRequest request)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<EnhancedReviewResponse>(
                        "Submission not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<EnhancedReviewResponse>(
                        "Assignment not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Lấy thông tin rubric và criteria
                Rubric rubric = null;
                List<Criteria> criteria = null;
                if (assignment.RubricId.HasValue)
                {
                    rubric = await _rubricRepository.GetByIdAsync(assignment.RubricId.Value);
                    if (rubric != null)
                    {
                        criteria = (await _criteriaRepository.GetByRubricIdAsync(rubric.RubricId)).ToList();
                    }
                }

                _logger.LogInformation($"Processing submission {submission.SubmissionId} for enhanced AI review");

                // Tải file và trích xuất text (giữ nguyên)
                using var fileStream = await _fileStorageService.GetFileStreamAsync(submission.FileUrl);
                if (fileStream == null)
                {
                    return new BaseResponse<EnhancedReviewResponse>(
                        "Could not download file from storage",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                var fileName = submission.FileName ?? submission.OriginalFileName;
                var extractedText = await _documentTextExtractor.ExtractTextAsync(fileStream, fileName);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new BaseResponse<EnhancedReviewResponse>(
                        "No text could be extracted from the file",
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Giới hạn độ dài text
                if (extractedText.Length > 12000)
                {
                    extractedText = extractedText.Substring(0, 12000) + "...";
                }

                // Tạo context với thông tin criteria
                var context = BuildEnhancedContext(assignment, rubric, criteria);

                _logger.LogInformation($"Calling AI service for enhanced review generation");

                // Gọi AI service với prompt cải tiến
                var reviewContent = await _genAIService.GenerateEnhancedReviewAsync(extractedText, context, criteria);
                if (reviewContent.Length > 2000)
                {
                    reviewContent = reviewContent.Substring(0, 2000);
                    _logger.LogInformation($"Truncated enhanced review content to 2000 characters for submission {request.SubmissionId}");
                }
                // Xóa review cũ nếu có
                if (request.ReplaceExisting)
                {
                    var existingReviews = await _aiSummaryRepository.GetBySubmissionAndTypeAsync(request.SubmissionId, "EnhancedReview");
                    foreach (var existing in existingReviews)
                    {
                        await _aiSummaryRepository.DeleteAsync(existing);
                    }

                    // Xóa cả các criteria reviews cũ
                    var existingCriteriaReviews = await _aiSummaryRepository.GetBySubmissionAndTypePrefixAsync(request.SubmissionId, "CriteriaReview_");
                    foreach (var existing in existingCriteriaReviews)
                    {
                        await _aiSummaryRepository.DeleteAsync(existing);
                    }
                }

                // Lưu review tổng quát
                var aiReview = new AISummary
                {
                    SubmissionId = request.SubmissionId,
                    Content = reviewContent,
                    SummaryType = "EnhancedReview",
                    GeneratedAt = DateTime.UtcNow
                };

                await _aiSummaryRepository.AddAsync(aiReview);

                // Tạo và lưu criteria reviews
                var criteriaReviews = new List<CriteriaReviewResponse>();
                if (criteria != null && criteria.Any())
                {
                    foreach (var criterion in criteria)
                    {
                        var criteriaReviewContent = await _genAIService.GenerateCriteriaReviewAsync(extractedText, criterion, context);

                        if (!string.IsNullOrEmpty(criteriaReviewContent) && criteriaReviewContent.Length > 500)
                        {
                            criteriaReviewContent = criteriaReviewContent.Substring(0, 500);
                        }

                        var criteriaReview = new AISummary
                        {
                            SubmissionId = request.SubmissionId,
                            Content = criteriaReviewContent,
                            SummaryType = $"CriteriaReview_{criterion.CriteriaId}",
                            GeneratedAt = DateTime.UtcNow
                        };

                        await _aiSummaryRepository.AddAsync(criteriaReview);

                        // Thêm vào response
                        criteriaReviews.Add(new CriteriaReviewResponse
                        {
                            SummaryId = criteriaReview.SummaryId,
                            Content = criteriaReviewContent,
                            CriteriaTitle = criterion.Title,
                            CriteriaDescription = criterion.Description ?? string.Empty,
                            CriteriaWeight = criterion.Weight,
                            CriteriaMaxScore = criterion.MaxScore,
                            GeneratedAt = criteriaReview.GeneratedAt
                        });
                    }
                }

                var generationResponse = new EnhancedReviewResponse
                {
                    SummaryId = aiReview.SummaryId,
                    Content = aiReview.Content,
                    SummaryType = aiReview.SummaryType,
                    GeneratedAt = aiReview.GeneratedAt,
                    WasGenerated = true,
                    Status = $"Enhanced review generated successfully with {criteriaReviews.Count} criteria reviews",
                    CriteriaReviews = criteriaReviews
                };

                return new BaseResponse<EnhancedReviewResponse>(
                    "Enhanced AI review generated successfully",
                    StatusCodeEnum.Created_201,
                    generationResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating enhanced AI review for submission {SubmissionId}", request.SubmissionId);
                return new BaseResponse<EnhancedReviewResponse>(
                    $"Error generating enhanced AI review: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private string BuildEnhancedContext(Assignment assignment, Rubric rubric, List<Criteria> criteria)
        {
            var contextBuilder = new StringBuilder();

            // Thông tin bài tập cơ bản
            contextBuilder.AppendLine($"Assignment: {assignment.Title}");

            if (!string.IsNullOrEmpty(assignment.Description))
            {
                var shortDesc = assignment.Description.Length > 80 ?
                    assignment.Description.Substring(0, 80) + "..." : assignment.Description;
                contextBuilder.AppendLine($"Description: {shortDesc}");
            }

            // Thông tin grading scale
            contextBuilder.AppendLine($"Grading Scale: {assignment.GradingScale}");
            if (assignment.PassThreshold.HasValue)
            {
                contextBuilder.AppendLine($"Pass Threshold: {assignment.PassThreshold}%");
            }

            // Tiêu chí đánh giá chi tiết
            if (criteria != null && criteria.Any())
            {
                contextBuilder.AppendLine("Evaluation Criteria:");
                foreach (var criterion in criteria)
                {
                    contextBuilder.AppendLine($"- {criterion.Title} (Weight: {criterion.Weight}%, Max Score: {criterion.MaxScore})");
                    if (!string.IsNullOrEmpty(criterion.Description))
                    {
                        contextBuilder.AppendLine($"  Description: {criterion.Description}");
                    }
                }
            }

            return contextBuilder.ToString();
        }
        private async Task<AISummaryResponse> MapToResponse(AISummary aiSummary)
        {
            var submission = await _submissionRepository.GetByIdAsync(aiSummary.SubmissionId);
            var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;
            var student = submission != null ? await _userRepository.GetByIdAsync(submission.UserId) : null;

            var response = new AISummaryResponse
            {
                SummaryId = aiSummary.SummaryId,
                SubmissionId = aiSummary.SubmissionId,
                Content = aiSummary.Content,
                SummaryType = aiSummary.SummaryType,
                GeneratedAt = aiSummary.GeneratedAt,
                AssignmentTitle = assignment?.Title ?? string.Empty,
                CourseName = assignment?.CourseInstance?.Course?.CourseName ?? string.Empty,
                StudentName = student?.FirstName ?? string.Empty,
                StudentCode = student?.StudentCode ?? string.Empty,
                FileName = submission?.FileName ?? string.Empty,
                SubmittedAt = submission?.SubmittedAt ?? DateTime.MinValue,

                CriteriaTitle = string.Empty,
                CriteriaDescription = string.Empty
            };

            if (aiSummary.SummaryType.StartsWith("CriteriaReview_") &&
                int.TryParse(aiSummary.SummaryType.Substring("CriteriaReview_".Length), out int criteriaId))
            {
                var criteria = await _criteriaRepository.GetByIdAsync(criteriaId);
                if (criteria != null)
                {
                    response.CriteriaTitle = criteria.Title;
                    response.CriteriaDescription = criteria.Description ?? string.Empty;
                    response.CriteriaWeight = criteria.Weight;
                    response.CriteriaMaxScore = criteria.MaxScore;
                }
            }

            return response;
        }

        private async Task<string> GenerateMockSummaryAsync(Submission submission, string summaryType, string additionalInstructions)
        {
            // Mock implementation - in real scenario, this would call Gemini API
            var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
            var student = await _userRepository.GetByIdAsync(submission.UserId);

            return summaryType.ToLower() switch
            {
                "overall" => $"This is an overall summary of the submission for '{assignment?.Title}' by {student?.FirstName}. The submission was submitted on {submission.SubmittedAt:yyyy-MM-dd}. {additionalInstructions}",
                "strengths" => $"Key strengths identified in the submission: clear structure, relevant examples, and good analysis. {additionalInstructions}",
                "weaknesses" => $"Areas for improvement: could benefit from more detailed explanations and better formatting. {additionalInstructions}",
                "recommendations" => $"Recommendations: consider adding more references and expanding the conclusion section. {additionalInstructions}",
                "keypoints" => $"Key points: the submission covers all required topics and demonstrates understanding of the subject matter. {additionalInstructions}",
                _ => $"AI-generated summary for {summaryType}: This is a mock summary for demonstration purposes. {additionalInstructions}"
            };
        }

        public async Task<BaseResponse<AIOverallResponse>> GenerateOverallSummaryAsync(GenerateAIOverallRequest request)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<AIOverallResponse>("Submission not found", StatusCodeEnum.NotFound_404, null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AIOverallResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                }

                // Extract text từ file submission
                using var fileStream = await _fileStorageService.GetFileStreamAsync(submission.FileUrl);
                if (fileStream == null)
                {
                    return new BaseResponse<AIOverallResponse>("Could not download file", StatusCodeEnum.BadRequest_400, null);
                }

                var fileName = submission.FileName ?? submission.OriginalFileName;
                var extractedText = await _documentTextExtractor.ExtractTextAsync(fileStream, fileName);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new BaseResponse<AIOverallResponse>("No text extracted", StatusCodeEnum.BadRequest_400, null);
                }

                // KIỂM TRA UNHAPPY CASE - Nếu submission không phù hợp với assignment
                if (!await CheckSubmissionRelevanceAsync(submission, assignment, extractedText))
                {
                    var errorSummary = "**SUBMISSION NOT RELEVANT**\n\n" +
                                      "This submission does not match the assignment requirements. " +
                                      $"Expected: {assignment.Title}\n" +
                                      "Please review the assignment guidelines and resubmit appropriate content.";

                    // Lưu summary thông báo lỗi
                    var aiSummary = new AISummary
                    {
                        SubmissionId = request.SubmissionId,
                        Content = errorSummary,
                        SummaryType = "OverallSummary_NotRelevant",
                        GeneratedAt = DateTime.UtcNow
                    };
                    await _aiSummaryRepository.AddAsync(aiSummary);

                    var response = new AIOverallResponse
                    {
                        Summary = errorSummary,
                        IsRelevant = false
                    };

                    return new BaseResponse<AIOverallResponse>(
                        "Submission is not relevant to assignment",
                        StatusCodeEnum.BadRequest_400, // Đổi sang 400 thay vì 200
                        response
                    );
                }

                // Happy case - tiếp tục xử lý bình thường
                var summaryType = "OverallSummary";

                // Check if exists
                var existing = await _aiSummaryRepository.GetBySubmissionAndTypeAsync(request.SubmissionId, summaryType);
                var existingSummary = existing.FirstOrDefault();
                if (existingSummary != null)
                {
                    return new BaseResponse<AIOverallResponse>(
                        "Existing overall summary loaded",
                        StatusCodeEnum.OK_200,
                        new AIOverallResponse { Summary = existingSummary.Content, IsRelevant = true });
                }

                if (extractedText.Length > 12000) extractedText = extractedText.Substring(0, 12000) + "...";

                // Get max words from config
                var maxWordsConfig = await _systemConfigService.GetSystemConfigAsync("AISummaryMaxWords");
                int maxWords = int.Parse(maxWordsConfig ?? "200");

                // Build context với URL assignment
                var context = BuildEnhancedContextWithUrl(assignment, null, null, submission.FileUrl);

                // Call AI
                var summary = await _genAIService.GenerateOverallSummaryAsync(extractedText, context);

                // Truncate if needed
                if (summary.Split(' ').Length > maxWords)
                {
                    var words = summary.Split(' ').Take(maxWords).ToArray();
                    summary = string.Join(" ", words) + "...";
                }

                if (summary.Length > 2000) summary = summary.Substring(0, 2000);

                // Lưu vào DB
                var newAiSummary = new AISummary
                {
                    SubmissionId = request.SubmissionId,
                    Content = summary,
                    SummaryType = summaryType,
                    GeneratedAt = DateTime.UtcNow
                };
                await _aiSummaryRepository.AddAsync(newAiSummary);

                var successResponse = new AIOverallResponse { Summary = summary, IsRelevant = true };
                return new BaseResponse<AIOverallResponse>("AI overall summary generated and saved", StatusCodeEnum.Created_201, successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating overall summary");
                return new BaseResponse<AIOverallResponse>($"Error: {ex.Message}", StatusCodeEnum.InternalServerError_500, null);
            }
        }

        public async Task<BaseResponse<AICriteriaResponse>> GenerateCriteriaFeedbackAsync(GenerateAICriteriaRequest request)
        {
            try
            {
                var submission = await _submissionRepository.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                {
                    return new BaseResponse<AICriteriaResponse>("Submission not found", StatusCodeEnum.NotFound_404, null);
                }

                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                if (assignment == null)
                {
                    return new BaseResponse<AICriteriaResponse>("Assignment not found", StatusCodeEnum.NotFound_404, null);
                }

                // Get rubric & criteria
                Rubric rubric = null;
                List<Criteria> criteria = null;
                if (assignment.RubricId.HasValue)
                {
                    rubric = await _rubricRepository.GetByIdAsync(assignment.RubricId.Value);
                    if (rubric != null)
                    {
                        criteria = (await _criteriaRepository.GetByRubricIdAsync(rubric.RubricId)).ToList();
                    }
                }

                if (criteria == null || !criteria.Any())
                {
                    return new BaseResponse<AICriteriaResponse>("No rubric criteria found", StatusCodeEnum.NotFound_404, null);
                }

                // Extract text từ file
                using var fileStream = await _fileStorageService.GetFileStreamAsync(submission.FileUrl);
                if (fileStream == null)
                {
                    return new BaseResponse<AICriteriaResponse>("Could not download file", StatusCodeEnum.BadRequest_400, null);
                }

                var fileName = submission.FileName ?? submission.OriginalFileName;
                var extractedText = await _documentTextExtractor.ExtractTextAsync(fileStream, fileName);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new BaseResponse<AICriteriaResponse>("No text extracted", StatusCodeEnum.BadRequest_400, null);
                }

                if (extractedText.Length > 12000)
                    extractedText = extractedText.Substring(0, 12000) + "...";

                var isRelevant = await CheckSubmissionRelevanceAsync(submission, assignment, extractedText);

                if (!isRelevant)
                {
                    _logger.LogWarning($"Submission {request.SubmissionId} is not relevant to assignment. Returning error criteria feedback.");

                    // Tạo error feedback cho TẤT CẢ các criteria
                    var errorFeedbacks = new List<AICriteriaFeedbackItem>();

                    foreach (var criterion in criteria)
                    {
                        var errorMessage = "⚠Unable to evaluate: Submission content does not match assignment requirements.";

                        // Lưu error feedback vào database
                        var errorSummary = new AISummary
                        {
                            SubmissionId = request.SubmissionId,
                            Content = $"Score: 0 | Summary: {errorMessage}",
                            SummaryType = $"CriteriaFeedback_{criterion.CriteriaId}_NotRelevant",
                            GeneratedAt = DateTime.UtcNow
                        };
                        await _aiSummaryRepository.AddAsync(errorSummary);

                        errorFeedbacks.Add(new AICriteriaFeedbackItem
                        {
                            CriteriaId = criterion.CriteriaId,
                            Title = criterion.Title,
                            Description = criterion.Description ?? "",
                            Summary = errorMessage,
                            Score = 0, // Điểm 0 vì không liên quan
                            MaxScore = criterion.MaxScore
                        });
                    }

                    var errorResponse = new AICriteriaResponse
                    {
                        Feedbacks = errorFeedbacks,
                        IsRelevant = false,
                        ErrorMessage = "This submission does not match the assignment requirements. " +
                                      $"Expected: {assignment.Title}. " +
                                      "Please review the assignment guidelines and resubmit appropriate content."
                    };

                    return new BaseResponse<AICriteriaResponse>(
                        "Submission is not relevant to assignment",
                        StatusCodeEnum.BadRequest_400,
                        errorResponse
                    );
                }

                // Kiểm tra xem đã có feedback chưa
                var feedbacks = new List<AICriteriaFeedbackItem>();
                var allExist = true;

                foreach (var criterion in criteria)
                {
                    var summaryType = $"CriteriaFeedback_{criterion.CriteriaId}";
                    var existing = await _aiSummaryRepository.GetBySubmissionAndTypeAsync(request.SubmissionId, summaryType);
                    var existingSummary = existing.FirstOrDefault();

                    if (existingSummary != null)
                    {
                        // Parse score and summary from existing
                        decimal score = 0;
                        string summaryText = existingSummary.Content;

                        if (summaryText.Contains("Score:"))
                        {
                            var parts = summaryText.Split('|');
                            if (parts.Length >= 2)
                            {
                                var scoreStr = parts[0].Replace("Score:", "").Trim();
                                decimal.TryParse(scoreStr, out score);
                                summaryText = parts[1].Replace("Summary:", "").Trim();
                            }
                        }

                        feedbacks.Add(new AICriteriaFeedbackItem
                        {
                            CriteriaId = criterion.CriteriaId,
                            Title = criterion.Title,
                            Description = criterion.Description ?? "",
                            Summary = summaryText,
                            Score = score,
                            MaxScore = criterion.MaxScore
                        });
                    }
                    else
                    {
                        allExist = false;
                        break;
                    }
                }

                // Nếu đã tồn tại hết feedback, trả về luôn
                if (allExist)
                {
                    return new BaseResponse<AICriteriaResponse>(
                        "Existing criteria feedback loaded",
                        StatusCodeEnum.OK_200,
                        new AICriteriaResponse { Feedbacks = feedbacks, IsRelevant = true });
                }

                // Nếu không tồn tại hết, generate mới
                _logger.LogInformation($"Generating new criteria feedback for submission {request.SubmissionId}");

                // Build context
                var context = BuildEnhancedContext(assignment, rubric, criteria);

                // Generate per criteria
                feedbacks.Clear();
                foreach (var criterion in criteria)
                {
                    try
                    {
                        var criteriaSummary = await _genAIService.GenerateCriteriaSummaryAsync(extractedText, criterion, context);

                        // Parse score from AI response (assume "Score: X | Summary: ...")
                        decimal score = 0;
                        string summaryText = criteriaSummary;

                        if (criteriaSummary.Contains("Score:"))
                        {
                            var parts = criteriaSummary.Split('|');
                            if (parts.Length >= 2)
                            {
                                var scoreStr = parts[0].Replace("Score:", "").Trim();
                                if (decimal.TryParse(scoreStr, out var parsedScore))
                                {
                                    score = parsedScore;
                                }
                                summaryText = parts[1].Replace("Summary:", "").Trim();
                            }
                        }
                        else
                        {
                            // Nếu AI không trả về đúng format, log warning
                            _logger.LogWarning($"AI did not return expected format for criteria {criterion.CriteriaId}. Response: {criteriaSummary}");
                            summaryText = criteriaSummary;
                        }

                        // Truncate summary nếu quá dài
                        if (summaryText.Split(' ').Length > 30)
                        {
                            var words = summaryText.Split(' ').Take(30).ToArray();
                            summaryText = string.Join(" ", words) + "...";
                        }

                        // Clamp score trong khoảng hợp lệ
                        score = Math.Clamp(score, 0, criterion.MaxScore);

                        // Truncate toàn bộ content để fit database column
                        var fullContent = $"Score: {score} | Summary: {summaryText}";
                        if (fullContent.Length > 2000)
                        {
                            fullContent = fullContent.Substring(0, 2000);
                        }

                        // Lưu vào DB
                        var aiSummary = new AISummary
                        {
                            SubmissionId = request.SubmissionId,
                            Content = fullContent,
                            SummaryType = $"CriteriaFeedback_{criterion.CriteriaId}",
                            GeneratedAt = DateTime.UtcNow
                        };
                        await _aiSummaryRepository.AddAsync(aiSummary);

                        feedbacks.Add(new AICriteriaFeedbackItem
                        {
                            CriteriaId = criterion.CriteriaId,
                            Title = criterion.Title,
                            Description = criterion.Description ?? "",
                            Summary = summaryText,
                            Score = score,
                            MaxScore = criterion.MaxScore
                        });

                        _logger.LogInformation($"Generated feedback for criteria {criterion.CriteriaId} ({criterion.Title}): Score={score}/{criterion.MaxScore}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error generating feedback for criteria {criterion.CriteriaId}");

                        var errorMessage = $"Error generating feedback: {ex.Message}";
                        if (errorMessage.Length > 1950)
                        {
                            errorMessage = errorMessage.Substring(0, 1950) + "... [truncated]";
                        }

                        feedbacks.Add(new AICriteriaFeedbackItem
                        {
                            CriteriaId = criterion.CriteriaId,
                            Title = criterion.Title,
                            Description = criterion.Description ?? "",
                            Summary = errorMessage,
                            Score = 0,
                            MaxScore = criterion.MaxScore
                        });

                        var fullContent = $"Score: 0 | Summary: {errorMessage}";
                        var errorSummary = new AISummary
                        {
                            SubmissionId = request.SubmissionId,
                            Content = fullContent,
                            SummaryType = $"CriteriaFeedback_{criterion.CriteriaId}_Error",
                            GeneratedAt = DateTime.UtcNow
                        };
                        await _aiSummaryRepository.AddAsync(errorSummary);
                    }
                }

                var response = new AICriteriaResponse
                {
                    Feedbacks = feedbacks,
                    IsRelevant = true,
                    ErrorMessage = null
                };

                return new BaseResponse<AICriteriaResponse>(
                    "AI criteria feedback generated and saved",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating criteria feedback for submission {request.SubmissionId}");
                return new BaseResponse<AICriteriaResponse>(
                    $"Error: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }
        public async Task<BaseResponse<AIOverallResponse>> GenerateInstructorOverallSummaryAsync(GenerateAIOverallRequest request)
        {
            return await GenerateOverallSummaryAsync(request);
        }

        public async Task<BaseResponse<AICriteriaResponse>> GenerateInstructorCriteriaFeedbackAsync(GenerateAICriteriaRequest request)
        {
            return await GenerateCriteriaFeedbackAsync(request);
        }

        private async Task<bool> CheckSubmissionRelevanceAsync(Submission submission, Assignment assignment, string extractedText)
        {
            try
            {
                // Build context với thông tin chi tiết về assignment
                Rubric rubric = null;
                List<Criteria> criteria = null;

                if (assignment.RubricId.HasValue)
                {
                    rubric = await _rubricRepository.GetByIdAsync(assignment.RubricId.Value);
                    if (rubric != null)
                    {
                        criteria = (await _criteriaRepository.GetByRubricIdAsync(rubric.RubricId)).ToList();
                    }
                }

                var context = BuildEnhancedContext(assignment, rubric, criteria);

                // Giới hạn độ dài text để tránh vượt quá token limit
                var textToCheck = extractedText;
                if (textToCheck.Length > 8000)
                {
                    textToCheck = textToCheck.Substring(0, 8000) + "... [truncated]";
                }

                // Gọi AI để kiểm tra relevance
                var relevanceCheck = await _genAIService.CheckSubmissionRelevanceAsync(
                    textToCheck,
                    context,
                    assignment.Title
                );

                _logger.LogInformation($"Relevance check result for submission {submission.SubmissionId}: {relevanceCheck}");

                // Parse kết quả theo format RELEVANT|reason hoặc NOT_RELEVANT|reason
                var upperCheck = relevanceCheck.ToUpper().Trim();

                // Kiểm tra format response
                if (upperCheck.StartsWith("NOT_RELEVANT"))
                {
                    // Extract lý do
                    var parts = relevanceCheck.Split('|');
                    var reason = parts.Length > 1 ? parts[1].Trim() : "No reason provided";

                    _logger.LogWarning($"Submission {submission.SubmissionId} is NOT RELEVANT to assignment '{assignment.Title}'. Reason: {reason}");
                    return false;
                }
                else if (upperCheck.StartsWith("RELEVANT"))
                {
                    // Extract lý do (optional logging)
                    var parts = relevanceCheck.Split('|');
                    var reason = parts.Length > 1 ? parts[1].Trim() : "No reason provided";

                    _logger.LogInformation($"Submission {submission.SubmissionId} is RELEVANT to assignment '{assignment.Title}'. Reason: {reason}");
                    return true;
                }
                else
                {
                    // AI không trả về đúng format - cảnh báo và default là KHÔNG phù hợp để an toàn
                    _logger.LogWarning($"AI returned unexpected format for submission {submission.SubmissionId}: {relevanceCheck}. Defaulting to NOT RELEVANT for safety.");

                    // Kiểm tra fallback bằng keyword matching đơn giản
                    return PerformBasicRelevanceCheck(extractedText, assignment, criteria);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking submission relevance for submission {submission.SubmissionId}");

                // Nếu có lỗi, thực hiện kiểm tra cơ bản thay vì mặc định cho qua
                return PerformBasicRelevanceCheck(extractedText, assignment, null);
            }
        }

        /// <summary>
        /// Kiểm tra relevance cơ bản dựa trên keywords khi AI service fail
        /// </summary>
        private bool PerformBasicRelevanceCheck(string extractedText, Assignment assignment, List<Criteria> criteria)
        {
            try
            {
                var textLower = extractedText.ToLower();
                var titleLower = assignment.Title.ToLower();

                // Kiểm tra 1: Assignment về programming nhưng không có code
                var isProgrammingAssignment = titleLower.Contains("code") ||
                                             titleLower.Contains("programming") ||
                                             titleLower.Contains("implement") ||
                                             titleLower.Contains("system") && titleLower.Contains("oop");

                if (isProgrammingAssignment)
                {
                    // Kiểm tra xem có code keywords không
                    var hasCodeIndicators = textLower.Contains("class ") ||
                                           textLower.Contains("public ") ||
                                           textLower.Contains("private ") ||
                                           textLower.Contains("function") ||
                                           textLower.Contains("method") ||
                                           textLower.Contains("void ") ||
                                           textLower.Contains("return ") ||
                                           textLower.Contains("{") && textLower.Contains("}");

                    if (!hasCodeIndicators)
                    {
                        _logger.LogWarning($"Programming assignment but no code found in submission. Marking as NOT RELEVANT.");
                        return false;
                    }
                }

                // Kiểm tra 2: Extract main topic từ assignment title
                var titleKeywords = ExtractKeywords(titleLower);
                var contentKeywords = ExtractKeywords(textLower);

                // Đếm số keyword match
                var matchCount = titleKeywords.Count(tk => contentKeywords.Contains(tk));
                var matchRatio = titleKeywords.Any() ? (double)matchCount / titleKeywords.Count : 0;

                _logger.LogInformation($"Keyword match ratio: {matchRatio:P} ({matchCount}/{titleKeywords.Count})");

                // Nếu match < 20% keywords chính → không liên quan
                if (matchRatio < 0.2)
                {
                    _logger.LogWarning($"Low keyword match ratio ({matchRatio:P}). Marking as NOT RELEVANT.");
                    return false;
                }

                // Default: cho qua nếu không phát hiện vấn đề rõ ràng
                return true;
            }
            catch
            {
                // Nếu fallback cũng lỗi, mặc định cho qua
                return true;
            }
        }

        /// <summary>
        /// Extract keywords quan trọng từ text (bỏ stopwords)
        /// </summary>
        private List<string> ExtractKeywords(string text)
        {
            var stopWords = new HashSet<string>
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "up", "about", "into", "through", "during",
        "is", "are", "was", "were", "be", "been", "being", "have", "has", "had",
        "do", "does", "did", "will", "would", "should", "could", "may", "might",
        "must", "can", "using", "use", "system", "design", "implement", "create"
    };

            var words = text.Split(new[] { ' ', ',', '.', ';', ':', '\n', '\r', '\t' },
                                  StringSplitOptions.RemoveEmptyEntries)
                            .Select(w => w.Trim().ToLower())
                            .Where(w => w.Length > 3 && !stopWords.Contains(w))
                            .Distinct()
                            .ToList();

            return words;
        }
        // Thêm method mới để build context với URL
        private string BuildEnhancedContextWithUrl(Assignment assignment, Rubric rubric, List<Criteria> criteria, string submissionFileUrl)
        {
            var contextBuilder = new StringBuilder();

            // Thêm thông tin assignment và URL
            contextBuilder.AppendLine($"Assignment: {assignment.Title}");
            contextBuilder.AppendLine($"Assignment URL: [Assignment details available]");
            contextBuilder.AppendLine($"Submission File URL: {submissionFileUrl}");

            if (!string.IsNullOrEmpty(assignment.Description))
            {
                var shortDesc = assignment.Description.Length > 80 ?
                    assignment.Description.Substring(0, 80) + "..." : assignment.Description;
                contextBuilder.AppendLine($"Description: {shortDesc}");
            }

            // Thông tin grading scale
            contextBuilder.AppendLine($"Grading Scale: {assignment.GradingScale}");
            if (assignment.PassThreshold.HasValue)
            {
                contextBuilder.AppendLine($"Pass Threshold: {assignment.PassThreshold}%");
            }

            // Tiêu chí đánh giá chi tiết
            if (criteria != null && criteria.Any())
            {
                contextBuilder.AppendLine("Evaluation Criteria:");
                foreach (var criterion in criteria)
                {
                    contextBuilder.AppendLine($"- {criterion.Title} (Weight: {criterion.Weight}%, Max Score: {criterion.MaxScore})");
                    if (!string.IsNullOrEmpty(criterion.Description))
                    {
                        contextBuilder.AppendLine($"  Description: {criterion.Description}");
                    }
                }
            }

            return contextBuilder.ToString();
        }
    }
}