using Microsoft.AspNetCore.Http;
using Service.Interface;

public class FileStorageService : IFileStorageService
{
    public Task<string> SaveFileAsync(Stream file, string fileName)
    {
        // Trả về đường dẫn giả để code không bị null
        return Task.FromResult($"dummy://{fileName}");
    }

    public Task DeleteFileAsync(string filePath)
    {
        // Không làm gì
        return Task.CompletedTask;
    }

    public Task<FileUploadResult> UploadFileAsync(IFormFile file)
    {
        throw new NotImplementedException();
    }

    Task<bool> IFileStorageService.DeleteFileAsync(string fileUrl)
    {
        throw new NotImplementedException();
    }

    public Task<FileDownloadResult> DownloadFileAsync(string fileUrl)
    {
        throw new NotImplementedException();
    }
}
