using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Service.Interface;
using Supabase;
using Supabase.Storage;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class SupabaseFileStorageService : IFileStorageService
{
    private readonly Supabase.Client _client;
    private readonly ILogger<SupabaseFileStorageService> _logger;
    private readonly string _bucket;
    private readonly HttpClient _httpClient;

    public SupabaseFileStorageService(ILogger<SupabaseFileStorageService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;

        var url = configuration["Authentication:Supabase:Url"] ?? throw new ArgumentNullException("Authentication:Supabase:Url is missing in configuration");
        var key = configuration["Authentication:Supabase:ServiceKey"] ?? throw new ArgumentNullException("Authentication:Supabase:ServiceKey is missing in configuration");
        _bucket = configuration["Authentication:Supabase:DefaultBucket"] ?? "files";

        _client = new Supabase.Client(url, key, new SupabaseOptions
        {
            AutoConnectRealtime = false
        });
        _logger.LogInformation($"Supabase initialized with bucket '{_bucket}'");
    }

    

    private static string SanitizeFilename(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
       
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
      
        return sanitized.Replace(" ", "_");
    }

    public async Task<FileUploadResult> UploadFileAsync(IFormFile file, string? folder = null, bool makePublic = false, string? customFileName = null)
    {
        try
        {
            await _client.InitializeAsync();
            var storage = _client.Storage.From(_bucket);

            var fileExt = Path.GetExtension(file.FileName);
            string fileName;

            if (!string.IsNullOrEmpty(customFileName))
            {
                // LOGIC CHO INSTRUCTOR: Giữ tên gốc
                // Lấy tên file gốc (bỏ extension cũ để tránh trùng lặp vd: file.pdf.pdf)
                var nameWithoutExt = Path.GetFileNameWithoutExtension(customFileName);
                var sanitizedParams = SanitizeFilename(nameWithoutExt);

                // Thêm timestamp ngắn để tránh việc Instructor upload 2 file cùng tên trong cùng 1 folder bị ghi đè mất file cũ
                // Nếu bạn muốn giữ chính xác 100% tên thì bỏ phần DateTime.Now.Ticks đi
                fileName = $"{sanitizedParams}_{DateTime.UtcNow.Ticks}{fileExt}";
            }
            else
            {
                // LOGIC CHO STUDENT (CŨ): Mã hóa tên file bằng GUID
                fileName = $"{Guid.NewGuid()}{fileExt}";
            }

            var path = string.IsNullOrEmpty(folder) ? fileName : $"{folder}/{fileName}";

            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // Upsert = true: Ghi đè nếu trùng tên
            await storage.Upload(bytes, path, new Supabase.Storage.FileOptions { Upsert = true });

            string fileUrl = makePublic
                ? storage.GetPublicUrl(path)
                : await CreateSignedDownloadUrlAsync(path);

            return new FileUploadResult
            {
                Success = true,
                FileUrl = fileUrl,
                FileName = fileName // Trả về tên file đã được xử lý
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Supabase");
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    public async Task<string> CreateSignedDownloadUrlAsync(string path, int expireInSeconds = 3600)
    {
        await _client.InitializeAsync();
        var storage = _client.Storage.From(_bucket);
        return await storage.CreateSignedUrl(path, expireInSeconds);
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        await _client.InitializeAsync();
        var storage = _client.Storage.From(_bucket);
        await storage.Remove(fileUrl);
        return true;
    }

    public async Task<Stream?> GetFileStreamAsync(string filePathOrUrl)
    {
        try
        {
            // Nếu là URL public, tải trực tiếp từ URL
            if (filePathOrUrl.StartsWith("http"))
            {
                _logger.LogInformation($"Downloading from public URL: {filePathOrUrl}");
                return await DownloadFromPublicUrlAsync(filePathOrUrl);
            }

            // Nếu là đường dẫn trong storage, thử các cách khác nhau
            await _client.InitializeAsync();
            var storage = _client.Storage.From(_bucket);

            _logger.LogInformation($"Attempting to download file from storage: {filePathOrUrl}");

            // Thử các path khác nhau
            var possiblePaths = new[]
            {
                filePathOrUrl,
                $"submissions/{filePathOrUrl}",
                filePathOrUrl.Replace("files/", "") // Loại bỏ prefix files/ nếu có
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    _logger.LogInformation($"Trying path: {path}");

                    // SỬA: Chỉ định rõ ràng parameters để tránh ambiguous call
                    TransformOptions? transformOptions = null;
                    var bytes = await storage.Download(path, transformOptions: transformOptions, onProgress: null);

                    if (bytes != null && bytes.Length > 0)
                    {
                        _logger.LogInformation($"File found at: {path}, Size: {bytes.Length} bytes");
                        return new MemoryStream(bytes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to download from {path}: {ex.Message}");
                }
            }

            _logger.LogError($"File not found in any of the attempted paths: {filePathOrUrl}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from Supabase: {FilePath}", filePathOrUrl);
            return null;
        }
    }

    private async Task<Stream?> DownloadFromPublicUrlAsync(string publicUrl)
    {
        try
        {
            // Loại bỏ query parameters nếu có
            var cleanUrl = publicUrl.Split('?')[0];

            _logger.LogInformation($"Downloading from cleaned URL: {cleanUrl}");

            var response = await _httpClient.GetAsync(cleanUrl);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                _logger.LogInformation($"Successfully downloaded from public URL, stream length: {stream.Length}");
                return stream;
            }
            else
            {
                _logger.LogError($"Failed to download from public URL: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading from public URL: {PublicUrl}", publicUrl);
            return null;
        }
    }

    public async Task<byte[]?> GetFileBytesAsync(string filePath)
    {
        try
        {
            await _client.InitializeAsync();
            var storage = _client.Storage.From(_bucket);

            // SỬA: Chỉ định rõ ràng parameters để tránh ambiguous call
            TransformOptions? transformOptions = null;
            return await storage.Download(filePath, transformOptions: transformOptions, onProgress: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file bytes from Supabase: {FilePath}", filePath);
            return null;
        }
    }
}