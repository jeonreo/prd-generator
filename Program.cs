using System.Text.Json;
using PrdGenerator.Models;
using PrdGenerator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddHttpClient<OpenAiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/generate-prd", async (
    PrdRequest req,
    OpenAiService openAi,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("GeneratePrd");
    var requestId = Guid.NewGuid().ToString("N");

    var validationError = Validate(req);
    if (validationError is not null)
    {
        logger.LogWarning("requestId={RequestId} validationFailed reason={Reason}", requestId, validationError);
        return Results.BadRequest(new { requestId, error = validationError });
    }

    try
    {
        var started = DateTimeOffset.UtcNow;
        var result = await openAi.GeneratePrdAsync(req, requestId);
        var elapsedMs = (DateTimeOffset.UtcNow - started).TotalMilliseconds;

        logger.LogInformation(
            "requestId={RequestId} ok latencyMs={LatencyMs} ideaLen={IdeaLen} timelineWeeks={TimelineWeeks}",
            requestId,
            (int)elapsedMs,
            req.ProductIdea?.Length ?? 0,
            req.TimelineWeeks
        );

        return Results.Ok(result);
    }
    catch (OpenAiService.OpenAiException ex)
    {
        return Results.Problem(
            title: "Upstream AI error",
            detail: ex.Message.Length > 800 ? ex.Message.Substring(0, 800) : ex.Message,
            statusCode: StatusCodes.Status502BadGateway,
            extensions: new Dictionary<string, object?> { ["requestId"] = requestId }
        );
    }
    catch (JsonException ex)
    {
        logger.LogError(ex, "requestId={RequestId} jsonParseFailed", requestId);
        return Results.Problem(
            title: "Invalid AI response",
            detail: "Model returned invalid JSON",
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: new Dictionary<string, object?> { ["requestId"] = requestId }
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "requestId={RequestId} unexpectedError", requestId);
        return Results.Problem(
            title: "Server error",
            detail: "Unexpected error",
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: new Dictionary<string, object?> { ["requestId"] = requestId }
        );
    }
})
.WithName("GeneratePrd");

app.Run();

static string? Validate(PrdRequest req)
{
    if (string.IsNullOrWhiteSpace(req.ProductIdea)) return "productIdea is required";
    if (string.IsNullOrWhiteSpace(req.TargetUser)) return "targetUser is required";
    if (string.IsNullOrWhiteSpace(req.Problem)) return "problem is required";
    if (req.TimelineWeeks <= 0) return "timelineWeeks must be greater than 0";

    if (req.ProductIdea.Trim().Length < 30) return "productIdea is too short";
    return null;
}
