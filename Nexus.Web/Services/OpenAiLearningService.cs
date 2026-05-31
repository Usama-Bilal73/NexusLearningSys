using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nexus.Web.Services;

public interface IOpenAiLearningService
{
    Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default);
    Task<string> AnswerCourseQuestionAsync(string question, IReadOnlyList<string> contextChunks, CancellationToken cancellationToken = default);
}

public class OpenAiLearningService : IOpenAiLearningService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiLearningService> _logger;

    public OpenAiLearningService(IConfiguration configuration, HttpClient httpClient, ILogger<OpenAiLearningService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
    {
        var prompt = "Summarize this course PDF for students. Include key topics, action items, and important dates when present.";
        return SendChatAsync(prompt, Truncate(text), cancellationToken);
    }

    public Task<string> AnswerCourseQuestionAsync(string question, IReadOnlyList<string> contextChunks, CancellationToken cancellationToken = default)
    {
        var context = string.Join("\n\n---\n\n", contextChunks.Select(TruncateChunk));
        var prompt = "You are an AI course assistant. Answer only from the supplied syllabus and lecture-note context. If the answer is not present, say that the course documents do not contain enough information.";
        return SendChatAsync(prompt, $"Context:\n{context}\n\nQuestion: {question}", cancellationToken);
    }

    private async Task<string> SendChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "OpenAI API key is not configured. Add OpenAI:ApiKey to user secrets or set OPENAI_API_KEY.";
        }

        var model = _configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini";
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.2
        }), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI request failed with status {Status}: {Body}", response.StatusCode, body);
            return "AI service could not generate a response right now. Please check the OpenAI configuration and try again.";
        }

        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    private static string Truncate(string value) => value.Length <= 24000 ? value : value[..24000];
    private static string TruncateChunk(string value) => value.Length <= 4000 ? value : value[..4000];
}
