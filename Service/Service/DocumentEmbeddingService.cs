```csharp
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.DocumentEmbedding;
using Service.RequestAndResponse.Response.DocumentEmbedding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class DocumentEmbeddingService : IDocumentEmbeddingService
    {
        private readonly IDocumentEmbeddingRepository _documentEmbeddingRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IAISummaryRepository _aiSummaryRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IReviewAssignmentRepository _reviewAssignmentRepository;
        private readonly ASDPRSContext _context;

        public DocumentEmbeddingService(
            IDocumentEmbeddingRepository documentEmbeddingRepository,
            ISubmissionRepository submissionRepository,
            IReviewRepository reviewRepository,
            IAISummaryRepository aiSummaryRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IReviewAssignmentRepository reviewAssignmentRepository,
            ASDPRSContext context)
        {
            _documentEmbeddingRepository = documentEmbeddingRepository;
            _submissionRepository = submissionRepository;
            _reviewRepository = reviewRepository;
            _aiSummaryRepository = aiSummaryRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _reviewAssignmentRepository = reviewAssignmentRepository;
            _context = context;
        }

        public async Task<BaseResponse<DocumentEmbeddingResponse>> CreateDocumentEmbeddingAsync(CreateDocumentEmbeddingRequest request)
        {
            try
            {
                var sourceExists = await ValidateSourceExistsAsync(request.SourceType, request.SourceId);
                if (!sourceExists)
                {
                    return new BaseResponse<DocumentEmbeddingResponse>(
                        $"Source {request.SourceType} with ID {request.SourceId} not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var existing = await _documentEmbeddingRepository.ExistsAsync(request.SourceType, request.SourceId);
                if (existing)
                {
                    return new BaseResponse<DocumentEmbeddingResponse>(
                        $"Document embedding already exists for {request.SourceType} {request.SourceId}",
                        StatusCodeEnum.Conflict_409,
                        null);
                }

                var documentEmbedding = new DocumentEmbedding
                {
                    SourceType = request.SourceType,
                    SourceId = request.SourceId,
                    Content = request.Content,
                    ContentVector = request.ContentVector ?? GeneratePlaceholderVector(request.Content),
                    CreatedAt = DateTime.UtcNow
                };

                await _documentEmbeddingRepository.AddAsync(documentEmbedding);

                var response = await MapToResponseAsync(documentEmbedding);
                return new BaseResponse<DocumentEmbeddingResponse>(
                    "Document embedding created successfully",
                    StatusCodeEnum.Created_201,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<DocumentEmbeddingResponse>(
                    $"Error creating document embedding: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<DocumentEmbeddingResponse>> UpdateDocumentEmbeddingAsync(UpdateDocumentEmbeddingRequest request)
        {
            try
            {
                var documentEmbedding = await _documentEmbeddingRepository.GetByIdAsync(request.EmbeddingId);
                if (documentEmbedding == null)
                {
                    return new BaseResponse<DocumentEmbeddingResponse>(
                        "Document embedding not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                if (!string.IsNullOrEmpty(request.Content))
                    documentEmbedding.Content = request.Content;

                if (request.ContentVector != null)
                    documentEmbedding.ContentVector = request.ContentVector;
                else if (!string.IsNullOrEmpty(request.Content))
                    documentEmbedding.ContentVector = GeneratePlaceholderVector(request.Content);

                documentEmbedding.UpdatedAt = DateTime.UtcNow;

                await _documentEmbeddingRepository.UpdateAsync(documentEmbedding);

                var response = await MapToResponseAsync(documentEmbedding);
                return new BaseResponse<DocumentEmbeddingResponse>(
                    "Document embedding updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<DocumentEmbeddingResponse>(
                    $"Error updating document embedding: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> DeleteDocumentEmbeddingAsync(int embeddingId)
        {
            try
            {
                var documentEmbedding = await _documentEmbeddingRepository.GetByIdAsync(embeddingId);
                if (documentEmbedding == null)
                {
                    return new BaseResponse<bool>(
                        "Document embedding not found",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                await _documentEmbeddingRepository.DeleteAsync(documentEmbedding);
                return new BaseResponse<bool>(
                    "Document embedding deleted successfully",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error deleting document embedding: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<DocumentEmbeddingResponse>> GetDocumentEmbeddingByIdAsync(int embeddingId)
        {
            try
            {
                var documentEmbedding = await _documentEmbeddingRepository.GetByIdAsync(embeddingId);
                if (documentEmbedding == null)
                {
                    return new BaseResponse<DocumentEmbeddingResponse>(
                        "Document embedding not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = await MapToResponseAsync(documentEmbedding);
                return new BaseResponse<DocumentEmbeddingResponse>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<DocumentEmbeddingResponse>(
                    $"Error retrieving document embedding: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<DocumentEmbeddingResponse>>> GetDocumentEmbeddingsBySourceAsync(string sourceType, int sourceId)
        {
            try
            {
                var documentEmbeddings = await _documentEmbeddingRepository.GetBySourceAsync(sourceType, sourceId);
                var responses = new List<DocumentEmbeddingResponse>();

                foreach (var de in documentEmbeddings)
                {
                    responses.Add(await MapToResponseAsync(de));
                }

                return new BaseResponse<List<DocumentEmbeddingResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<DocumentEmbeddingResponse>>(
                    $"Error retrieving document embeddings: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<List<DocumentEmbeddingResponse>>> GetDocumentEmbeddingsBySourceTypeAsync(string sourceType)
        {
            try
            {
                var documentEmbeddings = await _documentEmbeddingRepository.GetBySourceTypeAsync(sourceType);
                var responses = new List<DocumentEmbeddingResponse>();

                foreach (var de in documentEmbeddings)
                {
                    responses.Add(await MapToResponseAsync(de));
                }

                return new BaseResponse<List<DocumentEmbeddingResponse>>(
                    "Success",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<DocumentEmbeddingResponse>>(
                    $"Error retrieving document embeddings: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<SearchResultResponse>> SearchDocumentEmbeddingsAsync(SearchDocumentEmbeddingRequest request)
        {
            try
            {
                IEnumerable<DocumentEmbedding> results;

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    results = await _documentEmbeddingRepository.GetByContentSearchAsync(request.SearchTerm);
                }
                else if (!string.IsNullOrEmpty(request.SourceType) && request.SourceId.HasValue)
                {
                    results = await _documentEmbeddingRepository.GetBySourceAsync(request.SourceType, request.SourceId.Value);
                }
                else if (!string.IsNullOrEmpty(request.SourceType))
                {
                    results = await _documentEmbeddingRepository.GetBySourceTypeAsync(request.SourceType);
                }
                else
                {
                    results = await _documentEmbeddingRepository.GetAllAsync();
                }

                var paginatedResults = results
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var responses = new List<DocumentEmbeddingResponse>();
                foreach (var de in paginatedResults)
                {
                    responses.Add(await MapToResponseAsync(de));
                }

                var searchResult = new SearchResultResponse
                {
                    Results = responses,
                    TotalCount = results.Count(),
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(results.Count() / (double)request.PageSize)
                };

                return new BaseResponse<SearchResultResponse>(
                    "Search completed successfully",
                    StatusCodeEnum.OK_200,
                    searchResult);
            }
            catch (Exception ex)
            {
                return new BaseResponse<SearchResultResponse>(
                    $"Error searching document embeddings: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<bool>> GenerateEmbeddingForSourceAsync(string sourceType, int sourceId)
        {
            try
            {
                var sourceContent = await GetSourceContentAsync(sourceType, sourceId);
                if (string.IsNullOrEmpty(sourceContent))
                {
                    return new BaseResponse<bool>(
                        $"No content found for {sourceType} {sourceId}",
                        StatusCodeEnum.NotFound_404,
                        false);
                }

                // Simulate AI embedding generation (replace with actual AI service integration)
                byte[] embeddingVector = GeneratePlaceholderVector(sourceContent);

                var embeddingRequest = new CreateDocumentEmbeddingRequest
                {
                    SourceType = sourceType,
                    SourceId = sourceId,
                    Content = sourceContent,
                    ContentVector = embeddingVector
                };

                var result = await CreateDocumentEmbeddingAsync(embeddingRequest);
                if (result.StatusCode != StatusCodeEnum.Created_201)
                {
                    return new BaseResponse<bool>(
                        $"Failed to create embedding: {result.Message}",
                        result.StatusCode,
                        false);
                }

                return new BaseResponse<bool>(
                    $"Embedding generated successfully for {sourceType} {sourceId}",
                    StatusCodeEnum.OK_200,
                    true);
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>(
                    $"Error generating embedding: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    false);
            }
        }

        public async Task<BaseResponse<List<DocumentEmbeddingResponse>>> GetSimilarDocumentsAsync(int embeddingId, int maxResults = 5)
        {
            try
            {
                var sourceEmbedding = await _documentEmbeddingRepository.GetByIdAsync(embeddingId);
                if (sourceEmbedding == null)
                {
                    return new BaseResponse<List<DocumentEmbeddingResponse>>(
                        "Source embedding not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var allEmbeddings = await _documentEmbeddingRepository.GetAllAsync();
                var similarEmbeddings = allEmbeddings
                    .Where(de => de.EmbeddingId != embeddingId)
                    .Select(de => new
                    {
                        Embedding = de,
                        Similarity = CalculateCosineSimilarity(sourceEmbedding.ContentVector, de.ContentVector)
                    })
                    .OrderByDescending(x => x.Similarity)
                    .Take(maxResults)
                    .Select(x => x.Embedding)
                    .ToList();

                var responses = new List<DocumentEmbeddingResponse>();
                foreach (var de in similarEmbeddings)
                {
                    responses.Add(await MapToResponseAsync(de));
                }

                return new BaseResponse<List<DocumentEmbeddingResponse>>(
                    "Similar documents retrieved successfully",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<DocumentEmbeddingResponse>>(
                    $"Error retrieving similar documents: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        private async Task<bool> ValidateSourceExistsAsync(string sourceType, int sourceId)
        {
            return sourceType.ToLower() switch
            {
                "submission" => await _submissionRepository.GetByIdAsync(sourceId) != null,
                "review" => await _reviewRepository.GetByIdAsync(sourceId) != null,
                "aisummary" => await _aiSummaryRepository.GetByIdAsync(sourceId) != null,
                _ => false
            };
        }

        private async Task<string> GetSourceContentAsync(string sourceType, int sourceId)
        {
            return sourceType.ToLower() switch
            {
                "submission" => await GetSubmissionContentAsync(sourceId),
                "review" => await GetReviewContentAsync(sourceId),
                "aisummary" => await GetAISummaryContentAsync(sourceId),
                _ => string.Empty
            };
        }

        private async Task<string> GetSubmissionContentAsync(int submissionId)
        {
            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission == null)
                return string.Empty;

            // Check for late submission penalty
            var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
            string penaltyNote = string.Empty;
            if (assignment != null && submission.SubmittedAt > assignment.Deadline && submission.SubmittedAt <= (assignment.FinalDeadline ?? DateTime.MaxValue))
            {
                var latePenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "LateSubmissionPenalty");
                if (decimal.TryParse(latePenaltyStr, out decimal latePenalty))
                {
                    penaltyNote = $"(Late submission, {latePenalty}% penalty applied)";
                }
            }

            return $"FileName: {submission.FileName}, Keywords: {submission.Keywords}, SubmittedAt: {submission.SubmittedAt:yyyy-MM-dd}, Penalty: {penaltyNote}";
        }

        private async Task<string> GetReviewContentAsync(int reviewId)
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
                return string.Empty;

            // Check for missing review penalty (if reviewer failed to complete on time)
            var reviewAssignment = await _reviewAssignmentRepository.GetByIdAsync(review.ReviewAssignmentId);
            string penaltyNote = string.Empty;
            if (reviewAssignment != null && reviewAssignment.Status != "Completed")
            {
                var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
                if (submission != null)
                {
                    var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                    if (assignment != null)
                    {
                        var missPenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "MissingReviewPenalty");
                        if (decimal.TryParse(missPenaltyStr, out decimal missPenalty))
                        {
                            penaltyNote = $"(Missing review, {missPenalty}% penalty applicable)";
                        }
                    }
                }
            }

            return $"Feedback: {review?.GeneralFeedback}, Score: {review?.OverallScore}, Penalty: {penaltyNote}";
        }

        private async Task<string> GetAISummaryContentAsync(int aiSummaryId)
        {
            var aiSummary = await _aiSummaryRepository.GetByIdAsync(aiSummaryId);
            return aiSummary?.Content ?? string.Empty;
        }

        private async Task<DocumentEmbeddingResponse> MapToResponseAsync(DocumentEmbedding documentEmbedding)
        {
            var response = new DocumentEmbeddingResponse
            {
                EmbeddingId = documentEmbedding.EmbeddingId,
                SourceType = documentEmbedding.SourceType,
                SourceId = documentEmbedding.SourceId,
                Content = documentEmbedding.Content,
                ContentVector = documentEmbedding.ContentVector,
                CreatedAt = documentEmbedding.CreatedAt,
                UpdatedAt = documentEmbedding.UpdatedAt
            };

            (response.SourceTitle, response.SourceDescription) = await GetSourceInfoAsync(documentEmbedding.SourceType, documentEmbedding.SourceId);

            return response;
        }

        private async Task<(string Title, string Description)> GetSourceInfoAsync(string sourceType, int sourceId)
        {
            switch (sourceType.ToLower())
            {
                case "submission":
                    var submission = await _submissionRepository.GetByIdAsync(sourceId);
                    if (submission != null)
                    {
                        var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                        var user = await _userRepository.GetByIdAsync(submission.UserId);
                        string penaltyNote = string.Empty;
                        if (submission.SubmittedAt > assignment?.Deadline && submission.SubmittedAt <= (assignment?.FinalDeadline ?? DateTime.MaxValue))
                        {
                            var latePenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "LateSubmissionPenalty");
                            if (decimal.TryParse(latePenaltyStr, out decimal latePenalty))
                            {
                                penaltyNote = $"(Late submission, {latePenalty}% penalty)";
                            }
                        }
                        return (
                            $"Submission: {submission.FileName}",
                            $"Assignment: {assignment?.Title}, Student: {user?.FirstName} {user?.LastName}, Submitted: {submission.SubmittedAt:yyyy-MM-dd}, {penaltyNote}"
                        );
                    }
                    break;

                case "review":
                    var review = await _reviewRepository.GetByIdAsync(sourceId);
                    if (review != null)
                    {
                        var reviewAssignment = await GetReviewAssignmentAsync(review.ReviewAssignmentId);
                        string penaltyNote = string.Empty;
                        if (reviewAssignment?.Status != "Completed")
                        {
                            var submission = await _submissionRepository.GetByIdAsync(reviewAssignment.SubmissionId);
                            if (submission != null)
                            {
                                var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
                                if (assignment != null)
                                {
                                    var missPenaltyStr = await GetAssignmentConfig(assignment.AssignmentId, "MissingReviewPenalty");
                                    if (decimal.TryParse(missPenaltyStr, out decimal missPenalty))
                                    {
                                        penaltyNote = $"(Missing review, {missPenalty}% penalty)";
                                    }
                                }
                            }
                        }
                        return (
                            $"Review #{review.ReviewId}",
                            $"Feedback: {Truncate(review.GeneralFeedback, 100)}, Score: {review.OverallScore}, {penaltyNote}"
                        );
                    }
                    break;

                case "aisummary":
                    var aiSummary = await _aiSummaryRepository.GetByIdAsync(sourceId);
                    if (aiSummary != null)
                    {
                        return (
                            $"AI Summary #{aiSummary.SummaryId}",
                            $"Content: {Truncate(aiSummary.Content, 100)}"
                        );
                    }
                    break;
            }

            return (string.Empty, string.Empty);
        }

        private async Task<ReviewAssignment> GetReviewAssignmentAsync(int reviewAssignmentId)
        {
            return await _reviewAssignmentRepository.GetByIdAsync(reviewAssignmentId);
        }

        private async Task<string> GetAssignmentConfig(int assignmentId, string key)
        {
            var configKey = $"{key}_{assignmentId}";
            var config = await _context.SystemConfigs.FirstOrDefaultAsync(sc => sc.ConfigKey == configKey);
            return config?.ConfigValue;
        }

        private byte[] GeneratePlaceholderVector(string content)
        {
            // Simulate AI embedding (replace with actual AI service integration)
            // For now, use a hash of the content as a placeholder
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        }

        private double CalculateCosineSimilarity(byte[] vectorA, byte[] vectorB)
        {
            if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length)
                return 0;

            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            normA = Math.Sqrt(normA);
            normB = Math.Sqrt(normB);

            return (normA * normB) == 0 ? 0 : dotProduct / (normA * normB);
        }

        private string Truncate(string text, int length)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= length ? text : text.Substring(0, length) + "...";
        }
    }
}