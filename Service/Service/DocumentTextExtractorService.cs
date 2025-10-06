// Service/Services/DocumentTextExtractor.cs
using DocumentFormat.OpenXml.Packaging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.IService;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Service.Service
{
    public class DocumentTextExtractor : IDocumentTextExtractor
    {
        public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
        {
            if (!fileStream.CanSeek)
            {
                var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms);
                ms.Position = 0;
                fileStream = ms;
            }
            fileStream.Position = 0;

            var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            try
            {
                return ext switch
                {
                    ".pdf" => ExtractTextFromPdf(fileStream),
                    ".docx" => ExtractTextFromDocx(fileStream),
                    ".xlsx" => ExtractTextFromXlsx(fileStream),
                    ".xls" => ExtractTextFromXls(fileStream),
                    ".zip" or ".rar" => await ExtractFromArchiveAsync(fileStream),
                    _ => await ReadStreamAsText(fileStream)
                };
            }
            finally
            {
                if (fileStream.CanSeek) fileStream.Position = 0;
            }
        }

        private string ExtractTextFromPdf(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        private string ExtractTextFromDocx(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            using var doc = WordprocessingDocument.Open(ms, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            return body?.InnerText ?? string.Empty;
        }

        private string ExtractTextFromXlsx(Stream stream)
        {
            // Use NPOI to read XLSX safely (avoid treating row as object)
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            var sb = new StringBuilder();

            IWorkbook workbook = new XSSFWorkbook(ms);
            for (int s = 0; s < workbook.NumberOfSheets; s++)
            {
                var sheet = workbook.GetSheetAt(s);
                if (sheet == null) continue;

                int firstRow = sheet.FirstRowNum;
                int lastRow = sheet.LastRowNum;
                for (int r = firstRow; r <= lastRow; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    int firstCell = row.FirstCellNum >= 0 ? row.FirstCellNum : 0;
                    int lastCell = row.LastCellNum >= 0 ? row.LastCellNum - 1 : row.LastCellNum;
                    for (int c = firstCell; c <= lastCell; c++)
                    {
                        var cell = row.GetCell(c);
                        if (cell != null)
                        {
                            sb.Append(cell.ToString());
                            sb.Append('\t');
                        }
                    }
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string ExtractTextFromXls(Stream stream)
        {
            // For old .xls (HSSF) use NPOI as well - attempt to open as workbook and reuse logic
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            var sb = new StringBuilder();
            IWorkbook workbook = WorkbookFactory.Create(ms); // supports HSSF and XSSF
            for (int s = 0; s < workbook.NumberOfSheets; s++)
            {
                var sheet = workbook.GetSheetAt(s);
                if (sheet == null) continue;
                int firstRow = sheet.FirstRowNum;
                int lastRow = sheet.LastRowNum;
                for (int r = firstRow; r <= lastRow; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;
                    int firstCell = row.FirstCellNum >= 0 ? row.FirstCellNum : 0;
                    int lastCell = row.LastCellNum >= 0 ? row.LastCellNum - 1 : row.LastCellNum;
                    for (int c = firstCell; c <= lastCell; c++)
                    {
                        var cell = row.GetCell(c);
                        if (cell != null)
                        {
                            sb.Append(cell.ToString());
                            sb.Append('\t');
                        }
                    }
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private async Task<string> ExtractFromArchiveAsync(Stream stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            var sb = new StringBuilder();
            using var archive = ArchiveFactory.Open(ms);
            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
            {
                try
                {
                    using var entryStream = entry.OpenEntryStream();
                    var innerText = await ExtractTextAsync(entryStream, entry.Key);
                    sb.AppendLine($"--- {entry.Key} ---");
                    sb.AppendLine(innerText);
                }
                catch
                {
                    // ignore unreadable entries
                }
            }
            return sb.ToString();
        }

        private async Task<string> ReadStreamAsText(Stream stream)
        {
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            return await sr.ReadToEndAsync();
        }
    }
}
