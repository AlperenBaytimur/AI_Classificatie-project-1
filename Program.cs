var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<OllamaService>((serviceProvider, httpClient) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var timeoutSeconds = configuration.GetValue<int?>("Ollama:TimeoutSeconds") ?? 300;
    httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
builder.Services.AddScoped<ExtractionService>();
builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<XmlService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "E-lips AI Classificatie API v1");
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => Results.Ok(new
{
    status = "running",
    service = "E-lips AI Classificatie API",
    endpoints = new[]
    {
        "POST /api/extract",
        "POST /api/extract/file",
        "GET /openapi/v1.json",
        "GET /swagger"
    }
}));

app.MapControllers();

app.Run();