using Microsoft.Extensions.Configuration;
using RacingTicketsAgent.Agent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RacingAgentClaude.Agent;

public class ClaudeClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly int _maxTokens;

    public ClaudeClient(IConfiguration config)
    {
        var apiKey = config["Claude:ApiKey"] ?? throw new Exception("Claude:ApiKey missing in appsettings.json");
        _model = config["Claude:Model"] ?? "claude-haiku-4-5-20251001";
        _maxTokens = int.Parse(config["Claude:MaxTokens"] ?? "4096");
        var timeout = int.Parse(config["Claude:TimeoutSeconds"] ?? "120");

        _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.anthropic.com"),
            Timeout = TimeSpan.FromSeconds(timeout)
        };
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> ChatAsync(string userPrompt, CancellationToken ct = default)
    {
        var request = new
        {
            model = _model,
            max_tokens = _maxTokens,
            messages = new[] { new { role = "user", content = userPrompt } }
        };

        var response = await _http.PostAsJsonAsync("/v1/messages", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: ct);
        return result?.Content?.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var test = await ChatAsync("Reply with only the word: ok");
            return !string.IsNullOrWhiteSpace(test);
        }
        catch { return false; }
    }
}

public class ClaudeResponse
{
    [JsonPropertyName("content")] public List<ClaudeContent>? Content { get; set; }
}

public class ClaudeContent
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("text")] public string Text { get; set; } = "";
}