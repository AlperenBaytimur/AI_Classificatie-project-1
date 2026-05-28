using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using MimeKit;
using SixLabors.ImageSharp;
using Tesseract;
using UglyToad.PdfPig;

public class DocumentIngestionService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp"
    };

    private readonly IConfiguration _configuration;

    public DocumentIngestionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> ExtractTextAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        var extension = Path.GetExtension(file.FileName);

        await using var stream = file.OpenReadStream();

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractTextFromPdf(stream);
        }

        if (string.Equals(extension, ".eml", StringComparison.OrdinalIgnoreCase))
        {
            return await ExtractTextFromEmailAsync(stream, cancellationToken);
        }

        if (ImageExtensions.Contains(extension))
        {
            return await ExtractTextFromImageAsync(stream, cancellationToken);
        }

        if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        throw new NotSupportedException($"File type '{extension}' is not supported. Use .pdf, .eml, .txt or image files.");
    }

    private static string ExtractTextFromPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private async Task<string> ExtractTextFromEmailAsync(Stream stream, CancellationToken cancellationToken)
    {
        var message = await MimeMessage.LoadAsync(stream, cancellationToken);

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            sb.AppendLine(message.TextBody);
        }
        else if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            sb.AppendLine(StripHtml(message.HtmlBody));
        }

        foreach (var attachment in message.Attachments.OfType<MimePart>())
        {
            if (attachment.Content == null)
            {
                continue;
            }

            var fileName = attachment.FileName ?? string.Empty;
            var extension = Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                continue;
            }

            await using var attachmentStream = new MemoryStream();
            await attachment.Content.DecodeToAsync(attachmentStream, cancellationToken);
            attachmentStream.Position = 0;

            if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine();
                sb.AppendLine($"--- Attachment: {fileName} ---");
                sb.AppendLine(ExtractTextFromPdf(attachmentStream));
            }
            else if (ImageExtensions.Contains(extension))
            {
                sb.AppendLine();
                sb.AppendLine($"--- Attachment OCR: {fileName} ---");
                sb.AppendLine(await ExtractTextFromImageAsync(attachmentStream, cancellationToken));
            }
        }

        return sb.ToString();
    }

    private async Task<string> ExtractTextFromImageAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var input = new MemoryStream();
        await stream.CopyToAsync(input, cancellationToken);
        var bytes = input.ToArray();

        // Validate image early to fail fast on unsupported/corrupt files.
        using (Image.Load(bytes))
        {
        }

        var tessDataPath = _configuration["Ocr:TessDataPath"] ?? "./tessdata";
        var language = _configuration["Ocr:Language"] ?? "eng";

        using var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
        using var pix = Pix.LoadFromMemory(bytes);
        using var page = engine.Process(pix);

        return page.GetText() ?? string.Empty;
    }

    private static string StripHtml(string html)
    {
        var noTags = Regex.Replace(html, "<.*?>", " ", RegexOptions.Singleline);
        return Regex.Replace(noTags, "\\s+", " ").Trim();
    }
}
