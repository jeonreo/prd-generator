using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PrdGenerator.Models;
using PrdGenerator.Prompts;

namespace PrdGenerator.Services;

public sealed class OpenAiService
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAiService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OpenAiService(HttpClient http, IConfiguration config, ILogger<OpenAiService> logger)
    {
        _http = http;
        _logger = logger;

        _apiKey = config["OPENAI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        _model = config["OPENAI_MODEL"] ?? Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-5.2";

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is missing");
    }

    public async Task<PrdResponse> GeneratePrdAsync(PrdRequest req, string requestId)
    {
        var inputObject = new
        {
            productIdea = req.ProductIdea,
            targetUser = req.TargetUser,
            problem = req.Problem,
            constraints = req.Constraints ?? new List<string>(),
            timelineWeeks = req.TimelineWeeks
        };

        var userContent = JsonSerializer.Serialize(inputObject, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        // Responses API payload
        // - max_output_tokens 사용 (Chat Completions의 max_tokens / max_completion_tokens와 다름) :contentReference[oaicite:1]{index=1}
        // - text.format.type = json_object 로 JSON 출력 강제 :contentReference[oaicite:2]{index=2}
        // - GPT-5 계열은 temperature 변경이 제한될 수 있어 아예 보내지 않음 :contentReference[oaicite:3]{index=3}
        var payload = new
        {
            model = _model,
            max_output_tokens = 1000,
            input = new object[]
            {
                new { role = "system", content = PrdSystemPrompt.Text.Trim() },
                new { role = "user", content = userContent }
            },
            text = new
            {
                format = new
                {
                    type = "json_object"
                }
            }
        };

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        using var response = await _http.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "requestId={RequestId} openAiNon200 status={Status} body={Body}",
                requestId,
                (int)response.StatusCode,
                body
            );
            throw new OpenAiException(body, response.StatusCode);
        }

        // Responses API는 output_text 필드를 제공 (있으면 그걸 우선 사용) :contentReference[oaicite:4]{index=4}
        using var doc = JsonDocument.Parse(body);

        string? content = null;
        if (doc.RootElement.TryGetProperty("output_text", out var outputTextEl))
        {
            content = outputTextEl.GetString();
        }

        // 일부 응답은 output 배열의 message.content[]에 담길 수 있으니 fallback
        if (string.IsNullOrWhiteSpace(content))
        {
            if (doc.RootElement.TryGetProperty("output", out var outputEl) && outputEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in outputEl.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "message" &&
                        item.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var c in contentArr.EnumerateArray())
                        {
                            if (c.TryGetProperty("type", out var ct) && ct.GetString() == "output_text" &&
                                c.TryGetProperty("text", out var txt))
                            {
                                content = txt.GetString();
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(content)) break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(content))
            throw new JsonException("Empty output_text from model");

        var prd = JsonSerializer.Deserialize<PrdResponse>(content, JsonOptions);
        if (prd is null) throw new JsonException("Failed to deserialize PRD JSON");

        prd.UseCases ??= new();
        prd.FunctionalRequirements ??= new();
        prd.NonFunctionalRequirements ??= new();
        prd.OutOfScope ??= new();
        prd.SuccessMetrics ??= new();
        prd.Risks ??= new();

        return prd;
    }

    public sealed class OpenAiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public OpenAiException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
