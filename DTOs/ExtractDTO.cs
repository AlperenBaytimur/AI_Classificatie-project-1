using Microsoft.AspNetCore.Http;

public class ExtractRequest
{
    public string? Text { get; set; }
}

public class ExtractFileRequest
{
    public IFormFile? File { get; set; }
}