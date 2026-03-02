using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace RacingTicketsAgent.Agent;

public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaClient(IConfiguration config)
    {
        var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model = config["Ollama:Model"] ?? "deepseek-coder:33b";
        var timeout = int.Parse(config["Ollama:TimeoutSeconds"] ?? "300");

        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(timeout)
        };
    }

    public async Task<string> ChatAsync(string userPrompt, CancellationToken ct = default)
    {
        var request = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = userPrompt } },
            stream = false
        };

        var response = await _http.PostAsJsonAsync("/api/chat", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct);
        return result?.Message?.Content?.Trim() ?? string.Empty;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _http.GetAsync("/api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}

public class OllamaResponse
{
    [JsonPropertyName("message")] public OllamaMessage? Message { get; set; }
}

public class OllamaMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("content")] public string Content { get; set; } = "";
}