using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/extract")]
public class ExtractionController : ControllerBase
{
    private readonly ExtractionService _extractor;
    private readonly DocumentIngestionService _ingestion;
    private readonly XmlService _xmlService;

    public ExtractionController(ExtractionService extractor, DocumentIngestionService ingestion, XmlService xmlService)
    {
        _extractor = extractor;
        _ingestion = ingestion;
        _xmlService = xmlService;
    }

    [HttpPost]
    public async Task<IActionResult> Extract([FromBody] ExtractRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text is required");

        ExtractionOutcome? result;

        try
        {
            result = await _extractor.ExtractAsync(request.Text, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            return StatusCode(504, new
            {
                error = "AI model timeout",
                message = ex.Message,
                suggestion = "Try a smaller input file, increase Ollama:TimeoutSeconds, or use a faster model."
            });
        }

        if (result == null)
            return StatusCode(500, "Extraction failed");

        var xml = _xmlService.ToXml(result.Data);

        return Ok(new
        {
            json = result.Data,
            confidenceScore = result.ConfidenceScore,
            manualReviewThreshold = result.ManualReviewThreshold,
            manualReviewRequired = result.ManualReviewRequired,
            missingFields = result.MissingFields,
            xml = xml
        });
    }

    [HttpPost("file")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ExtractFromFile([FromForm] ExtractFileRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null)
            return BadRequest("File is required");

        string sourceText;

        try
        {
            sourceText = await _ingestion.ExtractTextAsync(request.File, cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to read file: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(sourceText))
            return BadRequest("No readable text could be extracted from the provided file.");

        ExtractionOutcome? result;

        try
        {
            result = await _extractor.ExtractAsync(sourceText, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            return StatusCode(504, new
            {
                error = "AI model timeout",
                message = ex.Message,
                suggestion = "Try a smaller input file, increase Ollama:TimeoutSeconds, or use a faster model."
            });
        }

        if (result == null)
            return StatusCode(500, "Extraction failed");

        var xml = _xmlService.ToXml(result.Data);

        return Ok(new
        {
            json = result.Data,
            confidenceScore = result.ConfidenceScore,
            manualReviewThreshold = result.ManualReviewThreshold,
            manualReviewRequired = result.ManualReviewRequired,
            missingFields = result.MissingFields,
            xml = xml
        });
    }
}