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

    public SupabaseFileStorageService(IConfiguration config, ILogger<SupabaseFileStorageService> logger)
    {
        _logger = logger;
        _bucket = config["Supabase:DefaultBucket"] ?? "files";
        var url = config["Supabase:Url"];
        var key = config["Supabase:ServiceKey"];
        _client = new Supabase.Client(url, key, new SupabaseOptions { AutoConnectRealtime = false });
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

    public Task<Stream?> GetFileStreamAsync(string fileUrl) => Task.FromResult<Stream?>(null);
}
