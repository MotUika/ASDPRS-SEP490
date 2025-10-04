using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IDocumentTextExtractor
    {
        /// <summary>
        /// Trả về toàn bộ text rút ra từ file stream (có thể là nhiều trang)
        /// </summary>
        Task<string> ExtractTextAsync(Stream fileStream, string fileName);
    }
}
