using BussinessObject.Models;
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

        public DocumentEmbeddingService(
            IDocumentEmbeddingRepository documentEmbeddingRepository,
            ISubmissionRepository submissionRepository,
            IReviewRepository reviewRepository,
            IAISummaryRepository aiSummaryRepository,
            IAssignmentRepository assignmentRepository,
            IUserRepository userRepository)
        {
            _documentEmbeddingRepository = documentEmbeddingRepository;
            _submissionRepository = submissionRepository;
            _reviewRepository = reviewRepository;
            _aiSummaryRepository = aiSummaryRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
        }

        public async Task<BaseResponse<DocumentEmbeddingResponse>> CreateDocumentEmbeddingAsync(CreateDocumentEmbeddingRequest request)
        {
            try
            {
                // Validate source exists based on source type
                var sourceExists = await ValidateSourceExistsAsync(request.SourceType, request.SourceId);
                if (!sourceExists)
                {
                    return new BaseResponse<DocumentEmbeddingResponse>(
                        $"Source {request.SourceType} with ID {request.SourceId} not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Check if embedding already exists for this source
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
                    ContentVector = request.ContentVector,
                    CreatedAt = DateTime.UtcNow
                };

                await _documentEmbeddingRepository.AddAsync(documentEmbedding);

                var response = await MapToResponse(documentEmbedding);
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

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Content))
                    documentEmbedding.Content = request.Content;

                if (request.ContentVector != null)
                    documentEmbedding.ContentVector = request.ContentVector;

                await _documentEmbeddingRepository.UpdateAsync(documentEmbedding);

                var response = await MapToResponse(documentEmbedding);
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

                var response = await MapToResponse(documentEmbedding);
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
                    responses.Add(await MapToResponse(de));
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
                    responses.Add(await MapToResponse(de));
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

                // Apply pagination
                var paginatedResults = results
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var responses = new List<DocumentEmbeddingResponse>();
                foreach (var de in paginatedResults)
                {
                    responses.Add(await MapToResponse(de));
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

                // TODO: Integrate with AI service to generate vector embedding
                // var embeddingVector = await _aiService.GenerateEmbeddingAsync(sourceContent);

                // For now, create a simple embedding
                var embeddingRequest = new CreateDocumentEmbeddingRequest
                {
                    SourceType = sourceType,
                    SourceId = sourceId,
                    Content = sourceContent,
                    ContentVector = System.Text.Encoding.UTF8.GetBytes(sourceContent) // Placeholder
                };

                await CreateDocumentEmbeddingAsync(embeddingRequest);

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

                // TODO: Implement vector similarity search
                // For now, return embeddings with similar content length as a placeholder
                var allEmbeddings = await _documentEmbeddingRepository.GetAllAsync();
                var similarEmbeddings = allEmbeddings
                    .Where(de => de.EmbeddingId != embeddingId)
                    .OrderBy(de => Math.Abs(de.Content.Length - sourceEmbedding.Content.Length))
                    .Take(maxResults)
                    .ToList();

                var responses = new List<DocumentEmbeddingResponse>();
                foreach (var de in similarEmbeddings)
                {
                    responses.Add(await MapToResponse(de));
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
            // For Submission, we might need to read the file content from FileUrl
            // For now, return a combination of available text data
            return $"FileName: {submission?.FileName}, Keywords: {submission?.Keywords}";
        }

        private async Task<string> GetReviewContentAsync(int reviewId)
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            return review?.GeneralFeedback ?? string.Empty;
        }

        private async Task<string> GetAISummaryContentAsync(int aiSummaryId)
        {
            var aiSummary = await _aiSummaryRepository.GetByIdAsync(aiSummaryId);
            return aiSummary?.Content ?? string.Empty;
        }

        private async Task<DocumentEmbeddingResponse> MapToResponse(DocumentEmbedding documentEmbedding)
        {
            var response = new DocumentEmbeddingResponse
            {
                EmbeddingId = documentEmbedding.EmbeddingId,
                SourceType = documentEmbedding.SourceType,
                SourceId = documentEmbedding.SourceId,
                Content = documentEmbedding.Content,
                ContentVector = documentEmbedding.ContentVector,
                CreatedAt = documentEmbedding.CreatedAt
            };

            // Add additional info based on source type - SỬA LẠI ĐÂY
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
                        return (
                            $"Submission: {submission.FileName}",
                            $"Assignment: {assignment?.Title}, Student: {user?.FirstName}, Submitted: {submission.SubmittedAt:yyyy-MM-dd}"
                        );
                    }
                    break;

                case "review":
                    var review = await _reviewRepository.GetByIdAsync(sourceId);
                    if (review != null)
                    {
                        var reviewAssignment = await GetReviewAssignmentAsync(review.ReviewAssignmentId);
                        return (
                            $"Review #{review.ReviewId}",
                            $"Feedback: {Truncate(review.GeneralFeedback, 100)}, Score: {review.OverallScore}"
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
            // This would require a method in ReviewAssignmentRepository to get by ID with includes
            // For now, return null or implement a basic get
            return null;
        }

        private string Truncate(string text, int length)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= length ? text : text.Substring(0, length) + "...";
        }
    }
}