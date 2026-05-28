using System.Text;
using Newtonsoft.Json;

public class OllamaService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public OllamaService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var model = _configuration["Ollama:Model"] ?? "gpt-oss:20b";
        var endpoint = _configuration["Ollama:Endpoint"] ?? "http://localhost:11434/api/generate";

        var body = new
        {
            model,
            prompt = prompt,
            temperature = 0,
            stream = false
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(body),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await _http.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Ollama request timed out after {_http.Timeout.TotalSeconds} seconds.", ex);
        }
    }
}