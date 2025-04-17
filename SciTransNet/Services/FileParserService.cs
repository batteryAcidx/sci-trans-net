using SciTransNet.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;
using System.Text.RegularExpressions;

namespace SciTransNet.Services
{
    public class FileParserService : IFileParserService
    {
        public async Task<string> ExtractTextAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            using var stream = file.OpenReadStream();

            var rawText = extension switch
            {
                ".pdf" => ExtractFromPdf(stream),
                ".docx" => ExtractFromDocx(stream),
                ".txt" => await ExtractFromTxtAsync(stream),
                _ => throw new NotSupportedException("Unsupported file format.")
            };

            return CleanAndStructureText(rawText);
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

        private async Task<string> ExtractFromTxtAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private string CleanAndStructureText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            // Replace smart quotes, cleanup formatting artifacts
            string cleaned = rawText
                .Replace("“", "\"")
                .Replace("”", "\"")
                .Replace("‘", "'")
                .Replace("’", "'")
                .Replace("\\n", " ")
                .Replace("\r", "")
                .Replace("\t", " ")
                .Replace("  ", " ");

            // Insert line breaks before sections like FOCUS, LOGIC, FEATURES, etc.
            cleaned = Regex.Replace(cleaned, @"\b(FOCUS|LOGIC|FEATURES|IMPLICATIONS)\b", "\n\n$1\n", RegexOptions.IgnoreCase);

            // Add paragraph breaks after sentence endings followed by uppercase (start of new paragraph or heading)
            cleaned = Regex.Replace(cleaned, @"(?<=[.?!])\s+(?=[A-Z])", "\n\n");

            return cleaned.Trim();
        }
    }
}