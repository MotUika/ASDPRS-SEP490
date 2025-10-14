using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Service.Interface;
using Supabase;
using Supabase.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

public class SupabaseFileStorageService : IFileStorageService
{
    private readonly Supabase.Client _client;
    private readonly ILogger<SupabaseFileStorageService> _logger;
    private readonly string _bucket;

    public SupabaseFileStorageService(ILogger<SupabaseFileStorageService> logger)
    {
        _logger = logger;

        var url = "https://yznanpovvpvcqtblwggk.supabase.co";
        var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Inl6bmFucG92dnB2Y3F0Ymx3Z2drIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1OTU2NjU1NywiZXhwIjoyMDc1MTQyNTU3fQ.LivBDEuCI7VOVbjArzbI3aDdZkSEpwnOXISEE87nTxE"; // ⚠️ Service Role Key, không phải anon key
        _bucket = "files"; // Tên bucket bạn tạo trong Supabase Storage

        _client = new Supabase.Client(url, key, new SupabaseOptions
        {
            AutoConnectRealtime = false
        });

        _logger.LogInformation($"✅ Supabase initialized with bucket '{_bucket}'");
    }

    private static string SanitizeFilename(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');
        return fileName;
    }

    public async Task<FileUploadResult> UploadFileAsync(IFormFile file, string? folder = null, bool makePublic = false)
    {
        try
        {
            await _client.InitializeAsync();
            var storage = _client.Storage.From(_bucket);

            var fileName = $"{Guid.NewGuid()}_{SanitizeFilename(file.FileName)}";
            var path = string.IsNullOrEmpty(folder) ? fileName : $"{folder}/{fileName}";

            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            await storage.Upload(bytes, path, new Supabase.Storage.FileOptions { Upsert = true });


            string fileUrl = makePublic
                ? storage.GetPublicUrl(path)
                : await CreateSignedDownloadUrlAsync(path);

            return new FileUploadResult
            {
                Success = true,
                FileUrl = fileUrl,
                FileName = fileName
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

    public async Task<Stream?> GetFileStreamAsync(string filePath)
    {
        try
        {
            await _client.InitializeAsync();
            var storage = _client.Storage.From(_bucket);

            // Cách 2: Sử dụng transform options rõ ràng
            TransformOptions? transformOptions = null;
            var bytes = await storage.Download(filePath, transformOptions, null);

            if (bytes != null && bytes.Length > 0)
            {
                return new MemoryStream(bytes);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from Supabase: {FilePath}", filePath);
            return null;
        }
    }
}
