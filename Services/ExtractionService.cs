using Newtonsoft.Json;

public class ExtractionService
{
  private readonly OllamaService _ollama;
  private readonly IConfiguration _configuration;

  public ExtractionService(OllamaService ollama, IConfiguration configuration)
  {
    _ollama = ollama;
    _configuration = configuration;
  }

  public async Task<ExtractionOutcome?> ExtractAsync(string text, CancellationToken cancellationToken = default)
  {
    var maxInputChars = _configuration.GetValue<int?>("ExtractionQuality:MaxInputChars") ?? 20000;
    var boundedText = text.Length > maxInputChars ? text[..maxInputChars] : text;
    var prompt = BuildPrompt(boundedText);

    var rawResponse = await _ollama.GenerateAsync(prompt, cancellationToken);

    var envelope = JsonConvert.DeserializeObject<OllamaEnvelope>(rawResponse);
    var aiOutput = envelope?.Response;

    if (string.IsNullOrWhiteSpace(aiOutput))
    {
      Console.WriteLine("❌ Ollama response did not contain a 'response' field.");
      Console.WriteLine(rawResponse);
      return null;
    }

    try
    {
      var opdracht = JsonConvert.DeserializeObject<Opdracht>(aiOutput);

      if (opdracht == null)
      {
        return null;
      }

      var (score, missingFields) = CalculateConfidence(opdracht);
      var threshold = _configuration.GetValue<int?>("ExtractionQuality:ManualReviewThreshold") ?? 90;

      return new ExtractionOutcome
      {
        Data = opdracht,
        ConfidenceScore = score,
        ManualReviewThreshold = threshold,
        ManualReviewRequired = score < threshold,
        MissingFields = missingFields
      };
    }
    catch (Exception ex)
    {
      Console.WriteLine("JSON parsing failed:");
      Console.WriteLine($"Exception: {ex.GetType().Name}");
      Console.WriteLine($"Message: {ex.Message}");
      Console.WriteLine($"Output: {aiOutput}");
      return null;
    }
  }

  private static (decimal Score, List<string> MissingFields) CalculateConfidence(Opdracht opdracht)
  {
    var checks = new List<(string Field, bool Present)>
    {
      ("RelatieCode", !string.IsNullOrWhiteSpace(opdracht.RelatieCode)),
      ("Aantal", opdracht.Aantal.HasValue),
      ("ContainerType", !string.IsNullOrWhiteSpace(opdracht.ContainerType)),
      ("BrutoGewicht", opdracht.BrutoGewicht.HasValue),
      ("ZegelNummers", opdracht.ZegelNummers is { Count: > 0 }),
      ("Activiteiten", opdracht.Activiteiten is { Count: > 0 }),
      ("Financieel.Object", !string.IsNullOrWhiteSpace(opdracht.Financieel?.Object)),
      ("Financieel.EenheidsPrijs", opdracht.Financieel?.EenheidsPrijs.HasValue == true),
      ("Financieel.ValutaCode", !string.IsNullOrWhiteSpace(opdracht.Financieel?.ValutaCode)),
      ("Financieel.BTWCode", !string.IsNullOrWhiteSpace(opdracht.Financieel?.BTWCode)),
      ("Financieel.Fin_Code", !string.IsNullOrWhiteSpace(opdracht.Financieel?.Fin_Code))
    };

    var firstActivity = opdracht.Activiteiten?.FirstOrDefault();

    checks.AddRange(new[]
    {
      ("Activiteiten[0].Type", !string.IsNullOrWhiteSpace(firstActivity?.Type)),
      ("Activiteiten[0].Terminal", !string.IsNullOrWhiteSpace(firstActivity?.Terminal)),
      ("Activiteiten[0].Bedrijf", !string.IsNullOrWhiteSpace(firstActivity?.Bedrijf)),
      ("Activiteiten[0].Straat", !string.IsNullOrWhiteSpace(firstActivity?.Straat)),
      ("Activiteiten[0].Postcode", !string.IsNullOrWhiteSpace(firstActivity?.Postcode)),
      ("Activiteiten[0].Plaats", !string.IsNullOrWhiteSpace(firstActivity?.Plaats))
    });

    var presentCount = checks.Count(x => x.Present);
    var score = Math.Round((decimal)presentCount / checks.Count * 100m, 2, MidpointRounding.AwayFromZero);
    var missing = checks.Where(x => !x.Present).Select(x => x.Field).ToList();

    return (score, missing);
  }


  private string BuildPrompt(string text)
  {
    return $@"
You are an AI system for logistics order extraction.

Return ONLY valid JSON in this structure:

{{
  ""RelatieCode"": string,
  ""Aantal"": number,
  ""ContainerType"": string,
  ""BrutoGewicht"": number,
  ""ZegelNummers"": string[],
  ""Activiteiten"": [
    {{
      ""Type"": string,
      ""Terminal"": string,
      ""Bedrijf"": string,
      ""Straat"": string,
      ""Postcode"": string,
      ""Plaats"": string
    }}
  ],
  ""Financieel"": {{
    ""Object"": string,
    ""EenheidsPrijs"": number,
    ""ValutaCode"": string,
    ""BTWCode"": string,
    ""Fin_Code"": string
  }}
}}

Rules:
- No explanations
- No extra text
- Missing values = null

Document:
""""""
{text}
""""""
";
  }
}

public class ExtractionOutcome
{
  public required Opdracht Data { get; set; }
  public decimal ConfidenceScore { get; set; }
  public int ManualReviewThreshold { get; set; }
  public bool ManualReviewRequired { get; set; }
  public List<string> MissingFields { get; set; } = new();
}

public class OllamaEnvelope
{
  [JsonProperty("response")]
  public string? Response { get; set; }
}