using SciTransNet.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace SciTransNet.Services
{
    public class FileParserService : IFileParserService
    {
        public async Task<string> ExtractTextAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            using var stream = file.OpenReadStream();

            return extension switch
            {
                ".pdf" => ExtractFromPdf(stream),
                ".docx" => ExtractFromDocx(stream),
                _ => throw new NotSupportedException("Unsupported file format.")
            };
        }

        private string ExtractFromPdf(Stream stream)
        {
            var sb = new StringBuilder();
            using var pdf = PdfDocument.Open(stream);
            foreach (Page page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        private string ExtractFromDocx(Stream stream)
        {
            using var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            using var wordDoc = WordprocessingDocument.Open(memStream, false);

            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            var text = body.Descendants<Text>()
                .Select(t => t.Text)
                .Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine(s))
                .ToString();

            return text;
        }
    }
}