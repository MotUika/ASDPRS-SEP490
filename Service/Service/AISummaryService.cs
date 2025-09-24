using BussinessObject.Models;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.AISummary;
using Service.RequestAndResponse.Response.AISummary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class AISummaryService : IAISummaryService
    {
        private readonly IAISummaryRepository _aiSummaryRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
/*        private readonly IAIService _aiService; // Assuming an AI service interface for Gemini integration
*/
        public AISummaryService(
            IAISummaryRepository aiSummaryRepository,
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository
            /*IAIService aiService*/)
        {
            _aiSummaryRepository = aiSummaryRepository;
            _submissionRepository = submissionRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            /*_aiService = aiService;*/
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

                var aiSummary = new AISummary
                {
                    SubmissionId = request.SubmissionId,
                    Content = request.Content,
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

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Content))
                    aiSummary.Content = request.Content;

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

                // Check if summary already exists and forceRegenerate is false
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
                            WasGenerated = false,
                            Status = "Existing summary returned",
                            ModelUsed = "N/A",
                            GenerationTime = TimeSpan.Zero
                        };

                        return new BaseResponse<AISummaryGenerationResponse>(
                            "Existing AI summary returned",
                            StatusCodeEnum.OK_200,
                            response);
                    }
                }

                // Generate AI summary using Gemini API
                var startTime = DateTime.UtcNow;

                // TODO: Integrate with actual Gemini AI service
                // var aiResult = await _aiService.GenerateSummaryAsync(submission, request.SummaryType, request.AdditionalInstructions);

                // For now, create a mock response
                var generatedContent = await GenerateMockSummaryAsync(submission, request.SummaryType, request.AdditionalInstructions);
                var modelUsed = "gemini-pro"; // Mock model name

                var aiSummary = new AISummary
                {
                    SubmissionId = request.SubmissionId,
                    Content = generatedContent,
                    SummaryType = request.SummaryType,
                    GeneratedAt = DateTime.UtcNow
                };

                await _aiSummaryRepository.AddAsync(aiSummary);

                var generationTime = DateTime.UtcNow - startTime;

                var generationResponse = new AISummaryGenerationResponse
                {
                    SummaryId = aiSummary.SummaryId,
                    Content = aiSummary.Content,
                    SummaryType = aiSummary.SummaryType,
                    GeneratedAt = aiSummary.GeneratedAt,
                    WasGenerated = true,
                    Status = "Successfully generated",
                    ModelUsed = modelUsed,
                    GenerationTime = generationTime
                };

                return new BaseResponse<AISummaryGenerationResponse>(
                    "AI summary generated successfully",
                    StatusCodeEnum.Created_201,
                    generationResponse);
            }
            catch (Exception ex)
            {
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

        private async Task<AISummaryResponse> MapToResponse(AISummary aiSummary)
        {
            var submission = await _submissionRepository.GetByIdAsync(aiSummary.SubmissionId);
            var assignment = submission != null ? await _assignmentRepository.GetByIdAsync(submission.AssignmentId) : null;
            var student = submission != null ? await _userRepository.GetByIdAsync(submission.UserId) : null;

            return new AISummaryResponse
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
                SubmittedAt = submission?.SubmittedAt ?? DateTime.MinValue
            };
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
    }
}