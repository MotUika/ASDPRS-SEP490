using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadFileAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string fileUrl);
        Task<FileDownloadResult> DownloadFileAsync(string fileUrl);
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class FileDownloadResult
    {
        public bool Success { get; set; }
        public byte[] FileContent { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public string ErrorMessage { get; set; }
    }
}